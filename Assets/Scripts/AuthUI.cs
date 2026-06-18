using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// Login / registration screen backed by Firebase Authentication. Players
    /// sign in with email + password so their progress and scores sync to the
    /// cloud. Also offers an offline option and a one-time content-seeding
    /// button for first-time setup.
    /// </summary>
    public class AuthUI : MonoBehaviour
    {
        Canvas canvas;
        InputField nameField, emailField, passwordField;
        Text titleText, statusText, toggleHintText;
        Button primaryButton, toggleButton, offlineButton, seedButton;
        Text primaryLabel, toggleLabel;
        GameObject nameRow;
        bool registerMode = false;
        bool busy = false;

        public void Build()
        {
            canvas = UIFactory.CreateCanvas("AuthCanvas", 60);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Panel2(root, UIFactory.Hex("E8F3E0"),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Bg");

            var card = UIFactory.PanelCentered(root, Color.white, 520, 560, 0, 0, "Card");
            var c = card.transform;

            UIFactory.SpriteImage(c, "hornbill", 110, 110, 0, 210);
            titleText = UIFactory.LabelAt(c, "Sign In", 34, UIFactory.GreenDark,
                460, 44, 0, 138, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Name (register only)
            nameField = UIFactory.MakeInput(c, "Display name", 420, 52, 0, 90);
            nameRow = nameField.gameObject;

            emailField = UIFactory.MakeInput(c, "Email", 420, 52, 0, 28);
            passwordField = UIFactory.MakeInput(c, "Password (min 6 characters)", 420, 52, 0, -34, true);

            primaryButton = UIFactory.MakeButton(c, "Sign In", UIFactory.Green, Color.white,
                420, 56, 0, -106, OnPrimary, 24);
            primaryLabel = primaryButton.GetComponentInChildren<Text>();

            toggleButton = UIFactory.MakeButton(c, "New here? Create an account", UIFactory.Cream,
                UIFactory.GreenDark, 420, 44, 0, -168, ToggleMode, 18);
            toggleLabel = toggleButton.GetComponentInChildren<Text>();

            statusText = UIFactory.LabelAt(c, "", 18, UIFactory.Red,
                460, 50, 0, -212, TextAnchor.MiddleCenter);

            // Footer: offline + seed
            offlineButton = UIFactory.MakeButton(root, "Play offline (no saving)", UIFactory.Hex("B0BEC5"),
                UIFactory.Ink, 300, 44, -110, -312, OnOffline, 16);
            seedButton = UIFactory.MakeButton(root, "Upload content to Firebase", UIFactory.Hex("90CAF9"),
                UIFactory.Ink, 300, 44, 210, -312, OnSeed, 16);

            ApplyMode();
            Hide();
        }

        void ApplyMode()
        {
            titleText.text = registerMode ? "Create Account" : "Sign In";
            primaryLabel.text = registerMode ? "Create Account" : "Sign In";
            toggleLabel.text = registerMode
                ? "Already have an account? Sign in"
                : "New here? Create an account";
            nameRow.SetActive(registerMode);
            statusText.text = "";
        }

        void ToggleMode()
        {
            if (busy) return;
            registerMode = !registerMode;
            ApplyMode();
        }

        void OnPrimary()
        {
            if (busy) return;
            string email = emailField.text.Trim();
            string pass = passwordField.text;
            string name = nameField.text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                statusText.text = "Please enter your email and password.";
                return;
            }
            if (registerMode && string.IsNullOrEmpty(name))
            {
                statusText.text = "Please choose a display name.";
                return;
            }

            SetBusy(registerMode ? "Creating account..." : "Signing in...");
            var be = Backend.Instance;
            if (registerMode)
                be.Register(email, pass, name, OnAuthResult);
            else
                be.SignIn(email, pass, OnAuthResult);
        }

        void OnAuthResult(bool ok, string error)
        {
            if (ok)
            {
                GameManager.Instance.OnSignedIn();
            }
            else
            {
                busy = false;
                statusText.color = UIFactory.Red;
                statusText.text = error;
            }
        }

        void OnOffline()
        {
            if (busy) return;
            GameManager.Instance.OnOffline();
        }

        void OnSeed()
        {
            if (busy) return;
            string email = emailField.text.Trim();
            string pass = passwordField.text;
            if (!Backend.Instance.IsSignedIn && (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)))
            {
                statusText.color = UIFactory.Ink;
                statusText.text = "Sign in first, then tap upload to seed content.";
                return;
            }
            SetBusy("Uploading content...");
            // Ensure signed in, then seed.
            if (Backend.Instance.IsSignedIn) DoSeed();
            else Backend.Instance.SignIn(email, pass, (ok, err) =>
            {
                if (ok) DoSeed();
                else { busy = false; statusText.color = UIFactory.Red; statusText.text = err; }
            });
        }

        void DoSeed()
        {
            Backend.Instance.SeedContent(ok =>
            {
                busy = false;
                statusText.color = ok ? UIFactory.Green : UIFactory.Red;
                statusText.text = ok
                    ? "Content uploaded to Firebase. You can sign in now."
                    : "Upload failed. Check your rules and config.";
            });
        }

        public void SetBusy(string message)
        {
            busy = true;
            statusText.color = UIFactory.Ink;
            statusText.text = message;
        }

        public void Show()
        {
            busy = false;
            ApplyMode();
            canvas.gameObject.SetActive(true);
        }

        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
