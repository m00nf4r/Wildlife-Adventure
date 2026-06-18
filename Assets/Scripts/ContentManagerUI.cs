using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// Content management screen for the quiz bank. Lets the player (or an
    /// author) ADD new questions, EDIT existing ones and DELETE them. Changes
    /// are saved to local storage immediately and, when signed in, also synced
    /// to Firebase. Reached from a button on the main menu.
    /// </summary>
    public class ContentManagerUI : MonoBehaviour
    {
        Canvas canvas;
        Action onBack;

        // ----- List view -----
        GameObject listPanel;
        readonly List<GameObject> rowObjects = new List<GameObject>();
        Transform listRoot;
        Text pageLabel;
        int page;
        const int PerPage = 5;

        // ----- Editor view -----
        GameObject editPanel;
        InputField questionInput;
        InputField[] optionInputs = new InputField[4];
        InputField explanationInput;
        Button[] correctButtons = new Button[4];
        Button[] difficultyButtons = new Button[3];
        Text editTitle, editStatus;
        int editCorrect;
        int editDifficulty;
        string editingId;   // null = adding new

        readonly string[] tierNames = { "Easy", "Hard", "Extra Hard" };
        readonly char[] letters = { 'A', 'B', 'C', 'D' };

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("ContentManagerCanvas", 58);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Panel2(root, UIFactory.Hex("E8F3E0"),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

            BuildListPanel(root);
            BuildEditPanel(root);

            Hide();
        }

        // =====================================================================
        //  LIST VIEW
        // =====================================================================
        void BuildListPanel(Transform root)
        {
            listPanel = UIFactory.PanelCentered(root, UIFactory.Cream, 1040, 620, 0, 0, "ListPanel").gameObject;
            var p = listPanel.transform;

            UIFactory.Panel2(p, UIFactory.GreenDark,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), Vector2.zero, "Header");
            UIFactory.LabelAt(p, "Manage Quiz Questions", 30, Color.white, 700, 44, -130, 268,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            UIFactory.MakeButton(p, "+ Add New", UIFactory.Amber, UIFactory.Ink,
                170, 46, 300, 268, () => OpenEditor(null), 20);
            UIFactory.MakeButton(p, "Back", UIFactory.Hex("B0BEC5"), UIFactory.Ink,
                120, 46, 450, 268, () => onBack?.Invoke(), 20);

            listRoot = new GameObject("Rows").transform;
            listRoot.SetParent(p, false);
            var rt = UIFactory.AddRect(listRoot.gameObject);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000, 480);
            rt.anchoredPosition = new Vector2(0, -10);

            // Footer: pagination + reset to defaults
            UIFactory.MakeButton(p, "< Prev", UIFactory.Green, Color.white,
                130, 44, -260, -280, () => { page--; RefreshList(); }, 18);
            pageLabel = UIFactory.LabelAt(p, "", 20, UIFactory.GreenDark, 240, 40, 0, -280,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.MakeButton(p, "Next >", UIFactory.Green, Color.white,
                130, 44, 260, -280, () => { page++; RefreshList(); }, 18);

            UIFactory.MakeButton(p, "Reset to Defaults", UIFactory.Hex("EF9A9A"), UIFactory.Ink,
                200, 40, 400, -280, ResetDefaults, 16);
        }

        void RefreshList()
        {
            foreach (var go in rowObjects) Destroy(go);
            rowObjects.Clear();

            var bank = WildlifeDatabase.Quiz;
            int total = bank.Count;
            int maxPage = Mathf.Max(0, (total - 1) / PerPage);
            page = Mathf.Clamp(page, 0, maxPage);
            pageLabel.text = "Page " + (page + 1) + " / " + (maxPage + 1)
                             + "   (" + total + " questions)";

            int start = page * PerPage;
            float rowH = 86, gap = 8;
            float y = 200;

            for (int i = start; i < Mathf.Min(start + PerPage, total); i++)
            {
                var q = bank[i];
                string capturedId = q.id;

                var row = UIFactory.PanelCentered(listRoot, Color.white, 980, rowH, 0, y, "Row").gameObject;
                rowObjects.Add(row);
                var rp = row.transform;

                // Difficulty tag
                var tag = UIFactory.PanelCentered(rp, TierColor(q.difficulty), 120, 34, -410, 22, "Tag");
                UIFactory.Label(tag.transform, tierNames[Mathf.Clamp(q.difficulty, 0, 2)], 16, Color.white);

                string preview = q.question.Length > 78 ? q.question.Substring(0, 75) + "..." : q.question;
                UIFactory.LabelAt(rp, preview, 18, UIFactory.Ink, 720, 40, 20, 18,
                    TextAnchor.MiddleLeft);
                UIFactory.LabelAt(rp, "Answer: " + letters[Mathf.Clamp(q.correctIndex, 0, 3)]
                    + ".  " + Trim(q.options != null && q.correctIndex < q.options.Length ? q.options[q.correctIndex] : "", 60),
                    14, UIFactory.Green, 760, 26, -10, -22, TextAnchor.MiddleLeft, FontStyle.Italic);

                UIFactory.MakeButton(rp, "Edit", UIFactory.Blue, Color.white,
                    100, 44, 380, 0, () => OpenEditor(capturedId), 18);
                UIFactory.MakeButton(rp, "Delete", UIFactory.Red, Color.white,
                    100, 44, 430, 0, () => DeleteQuestion(capturedId), 18);

                y -= (rowH + gap);
            }

            if (total == 0)
                UIFactory.LabelAt(listRoot, "No questions yet. Tap \"+ Add New\" to create one.",
                    22, UIFactory.Ink, 800, 60, 0, 120);
        }

        void DeleteQuestion(string id)
        {
            WildlifeDatabase.DeleteQuestion(id);
            SyncDeleteToCloud(id);
            RefreshList();
        }

        void ResetDefaults()
        {
            WildlifeDatabase.ResetQuizToDefault();
            page = 0;
            RefreshList();
        }

        // =====================================================================
        //  EDITOR VIEW
        // =====================================================================
        void BuildEditPanel(Transform root)
        {
            editPanel = UIFactory.PanelCentered(root, UIFactory.Cream, 900, 660, 0, 0, "EditPanel").gameObject;
            var p = editPanel.transform;

            UIFactory.Panel2(p, UIFactory.Green,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), Vector2.zero, "Header");
            editTitle = UIFactory.LabelAt(p, "Add Question", 28, Color.white, 700, 40, 0, 290,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.LabelAt(p, "Question", 18, UIFactory.GreenDark, 820, 26, 0, 240,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            questionInput = UIFactory.MakeInput(p, "Type the question...", 820, 60, 0, 200);

            // Four options with a "correct" selector each.
            float[] oy = { 130, 70, 10, -50 };
            for (int i = 0; i < 4; i++)
            {
                int captured = i;
                UIFactory.LabelAt(p, letters[i].ToString(), 22, UIFactory.GreenDark, 30, 50, -415, oy[i],
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                optionInputs[i] = UIFactory.MakeInput(p, "Option " + letters[i], 600, 50, -70, oy[i]);
                correctButtons[i] = UIFactory.MakeButton(p, "Correct", UIFactory.Hex("CFD8DC"), UIFactory.Ink,
                    160, 46, 330, oy[i], () => SetCorrect(captured), 16);
            }

            UIFactory.LabelAt(p, "Difficulty / Level", 18, UIFactory.GreenDark, 400, 26, -210, -100,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            float[] dx = { -260, -120, 30 };
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                difficultyButtons[i] = UIFactory.MakeButton(p, tierNames[i], UIFactory.Hex("CFD8DC"), UIFactory.Ink,
                    i == 2 ? 150 : 120, 44, dx[i] + (i == 2 ? 30 : 0), -140, () => SetDifficulty(captured), 16);
            }

            UIFactory.LabelAt(p, "Explanation (shown after answering)", 18, UIFactory.GreenDark, 600, 26, -110, -190,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            explanationInput = UIFactory.MakeInput(p, "Why is that the answer?", 820, 60, 0, -230);

            editStatus = UIFactory.LabelAt(p, "", 18, UIFactory.Red, 820, 30, 0, -272,
                TextAnchor.MiddleCenter);

            UIFactory.MakeButton(p, "Save", UIFactory.Green, Color.white,
                240, 56, -150, -300, OnSave, 22);
            UIFactory.MakeButton(p, "Cancel", UIFactory.Hex("B0BEC5"), UIFactory.Ink,
                240, 56, 150, -300, ShowList, 22);
        }

        void OpenEditor(string id)
        {
            editingId = id;
            editStatus.text = "";

            if (string.IsNullOrEmpty(id))
            {
                editTitle.text = "Add Question";
                questionInput.text = "";
                for (int i = 0; i < 4; i++) optionInputs[i].text = "";
                explanationInput.text = "";
                editCorrect = 0;
                editDifficulty = 0;
            }
            else
            {
                QuizQuestion q = null;
                foreach (var item in WildlifeDatabase.Quiz)
                    if (item.id == id) { q = item; break; }
                if (q == null) { ShowList(); return; }

                editTitle.text = "Edit Question";
                questionInput.text = q.question;
                for (int i = 0; i < 4; i++)
                    optionInputs[i].text = (q.options != null && i < q.options.Length) ? q.options[i] : "";
                explanationInput.text = q.explanation;
                editCorrect = Mathf.Clamp(q.correctIndex, 0, 3);
                editDifficulty = Mathf.Clamp(q.difficulty, 0, 2);
            }

            RefreshCorrectButtons();
            RefreshDifficultyButtons();

            listPanel.SetActive(false);
            editPanel.SetActive(true);
        }

        void SetCorrect(int i) { editCorrect = i; RefreshCorrectButtons(); }
        void SetDifficulty(int i) { editDifficulty = i; RefreshDifficultyButtons(); }

        void RefreshCorrectButtons()
        {
            for (int i = 0; i < 4; i++)
            {
                var img = correctButtons[i].GetComponent<Image>();
                var lbl = correctButtons[i].GetComponentInChildren<Text>();
                bool on = i == editCorrect;
                img.color = on ? UIFactory.Green : UIFactory.Hex("CFD8DC");
                lbl.color = on ? Color.white : UIFactory.Ink;
                lbl.text = on ? "✓ Correct" : "Set correct";
            }
        }

        void RefreshDifficultyButtons()
        {
            for (int i = 0; i < 3; i++)
            {
                var img = difficultyButtons[i].GetComponent<Image>();
                var lbl = difficultyButtons[i].GetComponentInChildren<Text>();
                bool on = i == editDifficulty;
                img.color = on ? TierColor(i) : UIFactory.Hex("CFD8DC");
                lbl.color = on ? Color.white : UIFactory.Ink;
            }
        }

        void OnSave()
        {
            string question = questionInput.text.Trim();
            var opts = new string[4];
            bool allOpts = true;
            for (int i = 0; i < 4; i++)
            {
                opts[i] = optionInputs[i].text.Trim();
                if (string.IsNullOrEmpty(opts[i])) allOpts = false;
            }

            if (string.IsNullOrEmpty(question)) { Warn("Please enter the question text."); return; }
            if (!allOpts) { Warn("Please fill in all four answer options."); return; }

            string expl = explanationInput.text.Trim();
            if (string.IsNullOrEmpty(expl)) expl = "Good thinking!";

            var q = new QuizQuestion(question, opts, editCorrect, expl, editDifficulty, editingId);

            if (string.IsNullOrEmpty(editingId)) WildlifeDatabase.AddQuestion(q);
            else WildlifeDatabase.UpdateQuestion(q);

            SyncUpsertToCloud(q);
            ShowList();
        }

        void Warn(string msg) { editStatus.color = UIFactory.Red; editStatus.text = msg; }

        // =====================================================================
        //  Cloud sync (best-effort; no-op when offline / not signed in)
        // =====================================================================
        void SyncUpsertToCloud(QuizQuestion q)
        {
            var be = Backend.Instance;
            if (be != null && be.IsConfigured && be.IsSignedIn)
                be.UpsertQuizQuestion(q, null);
        }

        void SyncDeleteToCloud(string id)
        {
            var be = Backend.Instance;
            if (be != null && be.IsConfigured && be.IsSignedIn)
                be.DeleteQuizQuestion(id, null);
        }

        // =====================================================================
        //  helpers
        // =====================================================================
        static Color TierColor(int tier)
        {
            switch (tier)
            {
                case 0: return UIFactory.Hex("43A047"); // easy green
                case 1: return UIFactory.Hex("FB8C00"); // hard orange
                case 2: return UIFactory.Hex("E53935"); // extra-hard red
            }
            return UIFactory.Green;
        }

        static string Trim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > max ? s.Substring(0, max - 1) + "…" : s;
        }

        void ShowList()
        {
            editPanel.SetActive(false);
            listPanel.SetActive(true);
            RefreshList();
        }

        public void Show(Action back)
        {
            onBack = back;
            page = 0;
            canvas.gameObject.SetActive(true);
            ShowList();
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
