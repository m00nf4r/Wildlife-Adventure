using System;
using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// Shows Wira the Hornbill's opening narration (the "Junior Ranger"
    /// story hook) as a bottom dialogue box, then hands control to the player.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        Canvas canvas;
        Text body;
        Action onDone;

        readonly string[] lines =
        {
            "Welcome, traveler! I am Wira the Hornbill, guardian of this forest.",
            "The Belum-Temenggor rainforest is in danger. Pollution is harming our home, and many animals are now rare.",
            "Your mission, Junior Ranger: explore the forest, scan every animal into your Field Journal, and clean up the litter you find.",
            "Press J near an animal to add it to your journal, and E near litter to clean it up. Mind the trees! Let's go!"
        };
        int index;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("DialogueCanvas", 15);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            var box = UIFactory.Panel2(root, UIFactory.Hex("1B5E20"),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-560, 30), new Vector2(560, 200), "Box");
            box.color = new Color(box.color.r, box.color.g, box.color.b, 0.96f);

            UIFactory.SpriteImage(box.transform, "hornbill", 150, 150, -470, 10);
            UIFactory.LabelAt(box.transform, "Wira the Hornbill", 24, UIFactory.Amber,
                700, 30, 70, 55, TextAnchor.MiddleLeft, FontStyle.Bold);

            body = UIFactory.LabelAt(box.transform, "", 24, Color.white,
                740, 110, 90, -15, TextAnchor.UpperLeft);

            UIFactory.MakeButton(box.transform, "Next  >", UIFactory.Amber, UIFactory.Ink,
                150, 44, 460, -55, Advance, 20);

            Hide();
        }

        public void ShowIntro(Action done)
        {
            onDone = done;
            index = 0;
            canvas.gameObject.SetActive(true);
            body.text = lines[index];
        }

        void Advance()
        {
            index++;
            if (index >= lines.Length)
            {
                Hide();
                onDone?.Invoke();
            }
            else body.text = lines[index];
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
