using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// The digital Field Journal: a centralized repository where players review
    /// every species. Discovered ones show art and name; undiscovered ones show
    /// a locked silhouette ("?").
    /// </summary>
    public class FieldJournalUI : MonoBehaviour
    {
        Canvas canvas;
        Transform grid;
        readonly List<(Image art, Text caption, Image bg)> slots = new List<(Image, Text, Image)>();
        Action closeAction;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("JournalCanvas", 22);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Dim(root);

            var panel = UIFactory.PanelCentered(root, UIFactory.Hex("EFE9D8"), 900, 560, 0, 0, "Journal");
            var p = panel.transform;

            UIFactory.Panel2(p, UIFactory.GreenDark,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(0, 0), "Header");
            UIFactory.LabelAt(p, "Field Journal", 32, Color.white, 700, 44, 0, 215,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.MakeButton(p, "Close (J)", UIFactory.Amber, UIFactory.Ink,
                150, 44, 340, 215, () => closeAction?.Invoke(), 18);

            // Build a fixed slot per species in a single row/grid.
            int n = WildlifeDatabase.TotalSpecies;
            float cellW = 190, cellH = 250, gap = 24;
            float totalW = n * cellW + (n - 1) * gap;
            float startX = -totalW / 2f + cellW / 2f;

            for (int i = 0; i < n; i++)
            {
                float x = startX + i * (cellW + gap);
                var cell = UIFactory.PanelCentered(p, Color.white, cellW, cellH, x, -30, "Cell");
                var art = UIFactory.SpriteImage(cell.transform, "tree", 150, 150, 0, 45);
                var cap = UIFactory.LabelAt(cell.transform, "", 18, UIFactory.GreenDark,
                    cellW - 12, 60, 0, -90, TextAnchor.UpperCenter, FontStyle.Bold);
                slots.Add((art, cap, cell));
            }

            Hide();
        }

        public void Show(Action onClose)
        {
            closeAction = onClose;
            var gm = GameManager.Instance;
            var species = WildlifeDatabase.Species;

            for (int i = 0; i < slots.Count && i < species.Count; i++)
            {
                var data = species[i];
                bool found = gm.Discovered.Contains(data.id);
                var slot = slots[i];

                if (found)
                {
                    slot.art.sprite = Resources.Load<Sprite>("Sprites/" + data.spriteName);
                    slot.art.color = Color.white;
                    slot.caption.text = data.commonName + "\n<" + data.conservationStatus + ">";
                    slot.bg.color = Color.white;
                }
                else
                {
                    slot.art.sprite = Resources.Load<Sprite>("Sprites/" + data.spriteName);
                    slot.art.color = new Color(0f, 0f, 0f, 0.55f); // silhouette
                    slot.caption.text = "???";
                    slot.bg.color = UIFactory.Hex("D7D2C2");
                }
            }

            canvas.gameObject.SetActive(true);
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
