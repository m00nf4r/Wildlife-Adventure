using System;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// The Fact Card overlay: high-contrast species illustration plus simplified,
    /// legible facts, mirroring the report's information layer. "Add to Journal"
    /// records the discovery and awards Conservation Points.
    /// </summary>
    public class FactCardUI : MonoBehaviour
    {
        Canvas canvas;
        Image artwork;
        Image statusBadge;
        Text nameText, sciText, statusText, factText, pointsHint;
        Action addAction, closeAction;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("FactCardCanvas", 20);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Dim(root);

            var card = UIFactory.PanelCentered(root, UIFactory.Cream, 760, 520, 0, 0, "Card");
            var c = card.transform;

            // Header strip
            UIFactory.Panel2(c, UIFactory.Green,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), new Vector2(0, 0), "Header");
            UIFactory.LabelAt(c, "New Discovery!", 26, Color.white, 700, 40, 0, 195,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            // Artwork
            var artBg = UIFactory.PanelCentered(c, UIFactory.Hex("DCE7D5"), 260, 260, -230, 0, "ArtBg");
            artwork = UIFactory.SpriteImage(artBg.transform, "tree", 230, 230, 0, 0);

            // Texts (right column)
            nameText = UIFactory.LabelAt(c, "", 34, UIFactory.GreenDark,
                420, 44, 120, 150, TextAnchor.MiddleLeft, FontStyle.Bold);
            sciText = UIFactory.LabelAt(c, "", 22, new Color(0.35f, 0.4f, 0.35f),
                420, 30, 120, 112, TextAnchor.MiddleLeft, FontStyle.Italic);

            statusBadge = UIFactory.PanelCentered(c, UIFactory.Red, 260, 40, 65, 70, "StatusBadge");
            statusText = UIFactory.Label(statusBadge.transform, "", 18, Color.white);

            factText = UIFactory.LabelAt(c, "", 20, UIFactory.Ink,
                440, 200, 130, -55, TextAnchor.UpperLeft);

            pointsHint = UIFactory.LabelAt(c, "", 18, UIFactory.Green,
                440, 26, 130, -170, TextAnchor.MiddleLeft, FontStyle.Bold);

            // Buttons
            UIFactory.MakeButton(c, "Add to Journal  +", UIFactory.Green, Color.white,
                280, 56, 110, -215, () => addAction?.Invoke(), 22);
            UIFactory.MakeButton(c, "Close", UIFactory.Hex("B0BEC5"), UIFactory.Ink,
                150, 56, -200, -215, () => closeAction?.Invoke(), 20);

            Hide();
        }

        public void Show(WildlifeData data, Action onAdd, Action onClose)
        {
            addAction = onAdd;
            closeAction = onClose;

            artwork.sprite = Resources.Load<Sprite>("Sprites/" + data.spriteName);
            nameText.text = data.commonName;
            sciText.text = data.scientificName;
            statusText.text = data.conservationStatus;
            statusBadge.color = UIFactory.StatusColor(data.conservationStatus);
            factText.text = data.fact;
            pointsHint.text = "+" + data.pointsAwarded + " Conservation Points";

            canvas.gameObject.SetActive(true);
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
