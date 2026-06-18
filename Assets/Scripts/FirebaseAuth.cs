using System;
using System.Collections;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// Email/password authentication via Firebase's Identity Toolkit REST API,
    /// plus silent re-login using a cached refresh token (so returning players
    /// stay signed in). Tokens are refreshed automatically before they expire.
    /// </summary>
    public static class FirebaseAuth
    {
        public static string IdToken { get; private set; }
        public static string RefreshToken { get; private set; }
        public static string Uid { get; private set; }
        public static string Email { get; private set; }
        public static string DisplayName { get; set; }

        static float tokenExpiryRealtime;

        public static bool IsSignedIn => !string.IsNullOrEmpty(IdToken);

        const string PREF_REFRESH = "wa_refresh";
        const string PREF_UID = "wa_uid";
        const string PREF_NAME = "wa_name";
        const string PREF_EMAIL = "wa_email";

        // ---------------- Public coroutines ----------------

        public static IEnumerator SignUp(string email, string password, string displayName,
                                         Action<bool, string> done)
        {
            string url = FirebaseConfig.IdentityBase + "signUp?key=" + FirebaseConfig.ApiKey;
            string body = "{\"email\":\"" + MiniJson.Escape(email) +
                          "\",\"password\":\"" + MiniJson.Escape(password) +
                          "\",\"returnSecureToken\":true}";
            yield return RestClient.Send("POST", url, body, null, (ok, code, resp) =>
            {
                if (ok) { ApplyAuthResponse(resp); DisplayName = displayName; SaveSession(); done(true, null); }
                else    { done(false, FriendlyError(resp)); }
            });
        }

        public static IEnumerator SignIn(string email, string password, Action<bool, string> done)
        {
            string url = FirebaseConfig.IdentityBase + "signInWithPassword?key=" + FirebaseConfig.ApiKey;
            string body = "{\"email\":\"" + MiniJson.Escape(email) +
                          "\",\"password\":\"" + MiniJson.Escape(password) +
                          "\",\"returnSecureToken\":true}";
            yield return RestClient.Send("POST", url, body, null, (ok, code, resp) =>
            {
                if (ok) { ApplyAuthResponse(resp); SaveSession(); done(true, null); }
                else    { done(false, FriendlyError(resp)); }
            });
        }

        /// <summary>Attempts to restore a session from a cached refresh token.</summary>
        public static IEnumerator TrySilentLogin(Action<bool> done)
        {
            string cached = PlayerPrefs.GetString(PREF_REFRESH, "");
            if (string.IsNullOrEmpty(cached)) { done(false); yield break; }
            RefreshToken = cached;
            DisplayName = PlayerPrefs.GetString(PREF_NAME, "");
            Email = PlayerPrefs.GetString(PREF_EMAIL, "");
            yield return Refresh(ok => done(ok));
        }

        public static IEnumerator Refresh(Action<bool> done)
        {
            if (string.IsNullOrEmpty(RefreshToken)) { done(false); yield break; }
            string body = "grant_type=refresh_token&refresh_token=" + RefreshToken;
            yield return RestClient.SendForm(FirebaseConfig.SecureTokenUrl, body, (ok, code, resp) =>
            {
                if (ok)
                {
                    var r = JsonUtility.FromJson<RefreshResponse>(resp);
                    IdToken = r.id_token;
                    RefreshToken = r.refresh_token;
                    Uid = r.user_id;
                    int secs; int.TryParse(r.expires_in, out secs);
                    tokenExpiryRealtime = Time.realtimeSinceStartup + (secs > 0 ? secs : 3600);
                    SaveSession();
                    done(true);
                }
                else done(false);
            });
        }

        /// <summary>Refreshes the token if it is missing or about to expire.</summary>
        public static IEnumerator EnsureFresh()
        {
            if (!IsSignedIn) yield break;
            if (Time.realtimeSinceStartup < tokenExpiryRealtime - 120f) yield break;
            yield return Refresh(_ => { });
        }

        public static void Logout()
        {
            IdToken = RefreshToken = Uid = Email = DisplayName = null;
            PlayerPrefs.DeleteKey(PREF_REFRESH);
            PlayerPrefs.DeleteKey(PREF_UID);
            PlayerPrefs.DeleteKey(PREF_NAME);
            PlayerPrefs.DeleteKey(PREF_EMAIL);
            PlayerPrefs.Save();
        }

        // ---------------- Helpers ----------------

        static void ApplyAuthResponse(string resp)
        {
            var r = JsonUtility.FromJson<AuthResponse>(resp);
            IdToken = r.idToken;
            RefreshToken = r.refreshToken;
            Uid = r.localId;
            Email = r.email;
            if (!string.IsNullOrEmpty(r.displayName)) DisplayName = r.displayName;
            int secs; int.TryParse(r.expiresIn, out secs);
            tokenExpiryRealtime = Time.realtimeSinceStartup + (secs > 0 ? secs : 3600);
        }

        static void SaveSession()
        {
            PlayerPrefs.SetString(PREF_REFRESH, RefreshToken ?? "");
            PlayerPrefs.SetString(PREF_UID, Uid ?? "");
            PlayerPrefs.SetString(PREF_NAME, DisplayName ?? "");
            PlayerPrefs.SetString(PREF_EMAIL, Email ?? "");
            PlayerPrefs.Save();
        }

        static string FriendlyError(string resp)
        {
            string msg = "Something went wrong. Please try again.";
            try
            {
                var e = JsonUtility.FromJson<ErrorResponse>(resp);
                if (e != null && e.error != null && !string.IsNullOrEmpty(e.error.message))
                {
                    switch (e.error.message)
                    {
                        case "EMAIL_EXISTS": return "That email is already registered.";
                        case "EMAIL_NOT_FOUND": return "No account found for that email.";
                        case "INVALID_PASSWORD": return "Incorrect password.";
                        case "INVALID_LOGIN_CREDENTIALS": return "Incorrect email or password.";
                        case "WEAK_PASSWORD : Password should be at least 6 characters":
                            return "Password must be at least 6 characters.";
                        case "MISSING_PASSWORD": return "Please enter a password.";
                        case "INVALID_EMAIL": return "That email address looks invalid.";
                        default: return e.error.message.Replace("_", " ");
                    }
                }
            }
            catch { }
            return msg;
        }
    }
}
