using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// Cloud scoreboard: shows the top scores stored in the Firestore "scores"
    /// collection across all players.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        Canvas canvas;
        Transform listRoot;
        Text statusText;
        Action onClose;
        readonly List<GameObject> rows = new List<GameObject>();

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("LeaderboardCanvas", 45);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Panel2(root, UIFactory.Hex("E8F3E0"),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

            var panel = UIFactory.PanelCentered(root, Color.white, 720, 560, 0, 0, "Panel");
            var p = panel.transform;

            UIFactory.Panel2(p, UIFactory.GreenDark,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), Vector2.zero, "Header");
            UIFactory.LabelAt(p, "Top Rangers", 32, Color.white, 600, 44, 0, 215,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            // Column headers
            UIFactory.LabelAt(p, "#", 18, UIFactory.GreenDark, 50, 26, -300, 150, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.LabelAt(p, "Ranger", 18, UIFactory.GreenDark, 260, 26, -150, 150, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.LabelAt(p, "Rank", 18, UIFactory.GreenDark, 180, 26, 120, 150, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.LabelAt(p, "Score", 18, UIFactory.GreenDark, 100, 26, 290, 150, TextAnchor.MiddleRight, FontStyle.Bold);

            listRoot = p;
            statusText = UIFactory.LabelAt(p, "", 20, UIFactory.Ink, 600, 40, 0, 0);

            UIFactory.MakeButton(p, "Back to Menu", UIFactory.Green, Color.white,
                240, 52, 0, -230, () => { if (onClose != null) onClose(); }, 20);

            Hide();
        }

        public void ShowLoading()
        {
            ClearRows();
            statusText.text = "Loading scores...";
            canvas.gameObject.SetActive(true);
        }

        public void Show(List<ScoreEntry> entries, Action close)
        {
            onClose = close;
            ClearRows();

            if (entries == null || entries.Count == 0)
            {
                statusText.text = "No scores yet. Be the first!";
            }
            else
            {
                statusText.text = "";
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    float y = 110 - i * 34;
                    var row = new GameObject("Row");
                    row.transform.SetParent(listRoot, false);
                    var rt = UIFactory.AddRect(row);
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(640, 32);
                    rt.anchoredPosition = new Vector2(0, y);
                    rows.Add(row);

                    Color tint = i == 0 ? UIFactory.Hex("F9A825") : UIFactory.Ink;
                    UIFactory.LabelAt(row.transform, (i + 1).ToString(), 18, tint, 50, 26, -300, 0, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIFactory.LabelAt(row.transform, string.IsNullOrEmpty(e.displayName) ? "Anonymous" : e.displayName,
                        18, UIFactory.Ink, 260, 26, -150, 0, TextAnchor.MiddleLeft);
                    UIFactory.LabelAt(row.transform, e.rank, 16, UIFactory.Green, 180, 26, 120, 0, TextAnchor.MiddleLeft);
                    UIFactory.LabelAt(row.transform, e.score.ToString(), 18, UIFactory.GreenDark, 100, 26, 290, 0, TextAnchor.MiddleRight, FontStyle.Bold);
                }
            }
            canvas.gameObject.SetActive(true);
        }

        void ClearRows()
        {
            foreach (var r in rows) if (r != null) Destroy(r);
            rows.Clear();
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
