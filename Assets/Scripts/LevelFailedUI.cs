using System;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// Game-over overlay shown when Wira flies into a tree. The run has failed,
    /// so the only ways forward are to restart the same level or return to the
    /// main menu.
    /// </summary>
    public class LevelFailedUI : MonoBehaviour
    {
        Canvas canvas;
        Text subText, statsText;
        Action onRetry, onMenu;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("LevelFailedCanvas", 45);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Panel2(root, new Color(0.20f, 0.05f, 0.05f, 0.92f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

            UIFactory.LabelAt(root, "LEVEL FAILED", 64, UIFactory.Hex("FF8A80"),
                900, 90, 0, 200, TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.LabelAt(root, "Wira crashed into a tree!", 30, Color.white,
                900, 40, 0, 130);

            subText = UIFactory.LabelAt(root,
                "Trees are solid — steer around the trunks. Keep your movements gentle.",
                22, UIFactory.Hex("FFCDD2"), 820, 70, 0, 60);

            statsText = UIFactory.LabelAt(root, "", 22, UIFactory.Amber,
                820, 40, 0, -10, TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.MakeButton(root, "Restart Level", UIFactory.Green, Color.white,
                300, 60, -170, -120, () => onRetry?.Invoke(), 24);
            UIFactory.MakeButton(root, "Main Menu", UIFactory.Hex("B0BEC5"), UIFactory.Ink,
                300, 60, 170, -120, () => onMenu?.Invoke(), 24);

            Hide();
        }

        public void Show(int score, int discovered, int total, Action retry, Action menu)
        {
            onRetry = retry;
            onMenu = menu;
            statsText.text = "You had ★ " + score + " pts  •  Wildlife found: "
                             + discovered + " / " + total;
            canvas.gameObject.SetActive(true);
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
