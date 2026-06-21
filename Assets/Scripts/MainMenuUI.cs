using System;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// Main menu: a clean, light layout where the player picks a difficulty
    /// (Novice / Intermediate / Advanced), manages questions, resets progress,
    /// opens the leaderboard, and signs in via the top-right account control.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        // Palette tuned to match the approved mockup.
        static readonly Color Ink        = UIFactory.Hex("16201C");
        static readonly Color Novice     = UIFactory.Hex("27AE74");
        static readonly Color Intermed   = UIFactory.Hex("F2B713");
        static readonly Color Advanced   = UIFactory.Hex("F0392B");
        static readonly Color ManageCol  = UIFactory.Hex("A5C95A");
        static readonly Color ResetCol   = UIFactory.Hex("6D6E70");
        static readonly Color BoardCol   = UIFactory.Hex("97674A");
        static readonly Color BoxBg      = UIFactory.Hex("CDEAD6");
        static readonly Color BoxText    = UIFactory.Hex("2E5E3A");

        Canvas canvas;
        Text bestText;
        Text loginText;
        Button loginButton;
        Image avatar;
        Button leaderboardPill;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("MenuCanvas", 50);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            // White backdrop.
            UIFactory.Panel2(root, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

            // ---- Header: mascot + title ----
            UIFactory.SpriteImage(root, "hornbill", 150, 150, -555, 252);
            UIFactory.LabelAt(root, "WILDLIFE ADVENTURE", 76, Ink,
                1010, 110, 80, 250, TextAnchor.MiddleLeft, FontStyle.Bold);

            // ---- Account control (top-right): text + avatar, both clickable ----
            loginButton = UIFactory.MakeButton(root, "", new Color(1, 1, 1, 0), Ink,
                330, 64, 470, 300, OnAccountClicked, 1);
            ((Image)loginButton.targetGraphic).raycastTarget = true;
            loginText = AddLabel(root, "LOGIN / SIGNUP", 19, Ink, 360, 36, 374, 300,
                TextAnchor.MiddleRight, FontStyle.Bold);
            avatar = RawSprite(root, UIShapes.Avatar(), Ink, 54, 54, 602, 300);

            // ---- Difficulty squares ----
            DifficultySquare(root, "NOVICE", DifficultyInfo.Questions(Difficulty.Easy) + " Questions",
                Novice, -340, Difficulty.Easy);
            DifficultySquare(root, "INTERMEDIATE", DifficultyInfo.Questions(Difficulty.Hard) + " Questions  \u2022  Timed",
                Intermed, 0, Difficulty.Hard);
            DifficultySquare(root, "ADVANCED", DifficultyInfo.Questions(Difficulty.ExtraHard) + " Questions  \u2022  Fast Timer",
                Advanced, 340, Difficulty.ExtraHard);

            // ---- Tool pills ----
            Pill(root, "MANAGE\nQUESTIONS", ManageCol, -300, () => GameManager.Instance.OpenContentManager());
            Pill(root, "RESET PROGRESS", ResetCol, 0, () => { SaveSystem.ClearSave(); RefreshBest(); });
            leaderboardPill = Pill(root, "LEADERBOARD", BoardCol, 300, () => GameManager.Instance.OpenLeaderboard());

            // ---- Info box (best score + controls) ----
            RawSprite(root, UIShapes.RoundedRect(16), BoxBg, 900, 104, 0, -262, false, Image.Type.Sliced);
            bestText = AddLabel(root, "", 22, BoxText, 880, 28, 0, -232, TextAnchor.MiddleCenter, FontStyle.Bold);
            AddLabel(root, "Move: Arrow Keys / WASD  \u2022  Journal: J",
                17, BoxText, 880, 24, 0, -260, TextAnchor.MiddleCenter);
            AddLabel(root, "Avoid the trees! Hitting a trunk fails the level. Animals & litter are hidden \u2014 explore to find them.",
                16, BoxText, 880, 24, 0, -286, TextAnchor.MiddleCenter);
            AddLabel(root, "Illustrated by Deeja & Myn",
                15, BoxText, 880, 22, 0, -308, TextAnchor.MiddleCenter, FontStyle.Italic);

            RefreshBest();
            Hide();
        }

        // ----- builders -----

        void DifficultySquare(Transform root, string title, string caption, Color color, float x, Difficulty d)
        {
            var b = UIFactory.MakeButton(root, "", color, Color.black, 220, 220, x, 32,
                () => GameManager.Instance.StartGame(d), 1);
            var img = (Image)b.targetGraphic;
            img.sprite = UIShapes.RoundedRect(26);
            img.type = Image.Type.Sliced;
            // Title can overflow the square edges (matches the mockup look).
            AddLabel(b.transform, title, 30, Color.black, 320, 40, 0, 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            AddLabel(b.transform, caption, 16, new Color(0, 0, 0, 0.72f), 196, 36, 0, -46, TextAnchor.MiddleCenter);
        }

        Button Pill(Transform root, string label, Color color, float x, Action action)
        {
            var b = UIFactory.MakeButton(root, "", color, Color.black, 250, 66, x, -150, action, 1);
            var img = (Image)b.targetGraphic;
            img.sprite = UIShapes.RoundedRect(33);
            img.type = Image.Type.Sliced;
            AddLabel(b.transform, label, 20, Color.black, 230, 60, 0, 0, TextAnchor.MiddleCenter, FontStyle.Bold);
            return b;
        }

        // A label placed at a pixel offset relative to its parent's centre.
        Text AddLabel(Transform parent, string text, int size, Color color, float w, float h,
                      float x, float y, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var holder = new GameObject("Label");
            holder.transform.SetParent(parent, false);
            var rt = holder.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            var t = UIFactory.Label(holder.transform, text, size, color, align, style);
            return t;
        }

        Image RawSprite(Transform parent, Sprite sp, Color color, float w, float h, float x, float y,
                        bool raycast = false, Image.Type type = Image.Type.Simple)
        {
            var go = new GameObject("Shape");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sp;
            img.color = color;
            img.type = type;
            img.raycastTarget = raycast;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return img;
        }

        // ----- behaviour -----

        void OnAccountClicked()
        {
            var be = Backend.Instance;
            if (be != null && be.IsSignedIn) GameManager.Instance.Logout();
            else GameManager.Instance.ShowAuth();
        }

        void RefreshBest()
        {
            int best = SaveSystem.LoadBestScore();
            bestText.text = SaveSystem.HasSave()
                ? "Best Score: " + best + "     |     Rank: " + SaveSystem.RankForScore(best)
                : "No runs yet \u2014 pick a level to begin!";
        }

        public void Show()
        {
            RefreshBest();
            var be = Backend.Instance;
            bool configured = be != null && be.IsConfigured;
            bool signedIn = be != null && be.IsSignedIn;

            if (signedIn && !string.IsNullOrEmpty(be.DisplayName))
                loginText.text = be.DisplayName.ToUpper();
            else if (signedIn)
                loginText.text = "LOG OUT";
            else if (configured)
                loginText.text = "LOGIN / SIGNUP";
            else
                loginText.text = "OFFLINE MODE";

            if (loginButton != null) loginButton.interactable = configured || signedIn;
            if (avatar != null) avatar.color = (configured || signedIn) ? Ink : new Color(0.6f, 0.6f, 0.6f, 1f);
            if (leaderboardPill != null) leaderboardPill.gameObject.SetActive(configured);

            canvas.gameObject.SetActive(true);
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}