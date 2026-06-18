using System;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>The "Nature Hero" reward screen shown after the quiz.</summary>
    public class RewardScreenUI : MonoBehaviour
    {
        Canvas canvas;
        Text titleText, subText, scoreText, timeText, rankText, bestTag;
        Action onContinue;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("RewardCanvas", 40);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Panel2(root, UIFactory.Green,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

            titleText = UIFactory.LabelAt(root, "AMAZING JOB!", 64, Color.white,
                900, 90, 0, 240, TextAnchor.MiddleCenter, FontStyle.Bold);
            subText = UIFactory.LabelAt(root, "You're a Nature Hero!", 30, UIFactory.Hex("DCEDC8"),
                900, 40, 0, 180);

            // Medal (hornbill in a ring)
            var ring = UIFactory.PanelCentered(root, UIFactory.GreenLight, 200, 200, 0, 30, "Ring");
            UIFactory.SpriteImage(ring.transform, "hornbill", 170, 170, 0, 0);

            // Stat cards
            var s1 = UIFactory.PanelCentered(root, UIFactory.Cream, 200, 110, -240, -150, "ScoreCard");
            UIFactory.LabelAt(s1.transform, "FINAL SCORE", 16, UIFactory.Green, 180, 24, 0, 32);
            scoreText = UIFactory.LabelAt(s1.transform, "0", 40, UIFactory.GreenDark, 180, 60, 0, -10,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            var s2 = UIFactory.PanelCentered(root, UIFactory.Cream, 200, 110, 0, -150, "TimeCard");
            UIFactory.LabelAt(s2.transform, "TIME TAKEN", 16, UIFactory.Green, 180, 24, 0, 32);
            timeText = UIFactory.LabelAt(s2.transform, "0:00", 40, UIFactory.GreenDark, 180, 60, 0, -10,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            var s3 = UIFactory.PanelCentered(root, UIFactory.Cream, 200, 110, 240, -150, "RankCard");
            UIFactory.LabelAt(s3.transform, "RANGER RANK", 16, UIFactory.Green, 180, 24, 0, 32);
            rankText = UIFactory.LabelAt(s3.transform, "", 24, UIFactory.GreenDark, 190, 60, 0, -10,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            bestTag = UIFactory.LabelAt(root, "", 22, UIFactory.Amber, 600, 30, 0, -235,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.MakeButton(root, "Save and Return to Menu", UIFactory.Cream, UIFactory.GreenDark,
                380, 60, 0, -300, () => onContinue?.Invoke(), 22);

            Hide();
        }

        public void Show(int score, float seconds, string rank, bool newBest, Action cont)
        {
            onContinue = cont;
            scoreText.text = score.ToString();
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            timeText.text = string.Format("{0}:{1:00}", m, s);
            rankText.text = rank;
            bestTag.text = newBest ? "New Best Score!" : "";
            canvas.gameObject.SetActive(true);
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
