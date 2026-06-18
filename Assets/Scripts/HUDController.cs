using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// The minimalist heads-up display: Conservation Points, species progress,
    /// habitat name, a Field Journal button, and a contextual interaction
    /// prompt that follows whatever Wira is closest to.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        Canvas canvas;
        Text pointsText;
        Text progressText;
        Text promptText;
        GameObject promptBox;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("HUDCanvas", 10);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            // Top bar
            var bar = UIFactory.Panel2(root, new Color(0.10f, 0.20f, 0.13f, 0.82f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 0), "TopBar");

            UIFactory.LabelAt(bar.transform, HabitatBuilder.HabitatName, 22, Color.white,
                420, 40, -420, 0, TextAnchor.MiddleLeft, FontStyle.Bold);

            pointsText = UIFactory.LabelAt(bar.transform, "", 26, UIFactory.Amber,
                360, 40, 70, 0, TextAnchor.MiddleCenter, FontStyle.Bold);

            progressText = UIFactory.LabelAt(bar.transform, "", 22, Color.white,
                300, 40, 360, 0, TextAnchor.MiddleCenter);

            UIFactory.MakeButton(bar.transform, "Field Journal (J)", UIFactory.GreenLight,
                UIFactory.GreenDark, 230, 44, 540, 0,
                () => GameManager.Instance.OpenJournal(), 18);

            // Bottom interaction prompt
            promptBox = UIFactory.Panel2(root, new Color(0f, 0f, 0f, 0.7f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-260, 24), new Vector2(260, 70), "Prompt").gameObject;
            promptText = UIFactory.Label(promptBox.transform, "", 22, Color.white);

            Hide();
        }

        void Update()
        {
            if (canvas == null || !canvas.gameObject.activeSelf) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.player == null) return;

            var target = gm.player.Current;
            if (gm.State == GameState.Exploring && target != null && !string.IsNullOrEmpty(target.Prompt))
            {
                promptBox.SetActive(true);
                promptText.text = target.Prompt;
            }
            else
            {
                promptBox.SetActive(false);
            }
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            pointsText.text = "★ " + gm.Score + " pts";
            progressText.text = "Wildlife: " + gm.DiscoveredCount + " / " + gm.TotalSpecies;
        }

        public void Show() { canvas.gameObject.SetActive(true); Refresh(); }
        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
