using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

namespace WildlifeAdventure
{
    /// <summary>
    /// Thin wrapper over UnityWebRequest for JSON REST calls. Works identically
    /// in the Editor, standalone, and WebGL (where it uses the browser's fetch).
    /// </summary>
    public static class RestClient
    {
        /// <param name="onDone">(success, httpStatus, responseBody)</param>
        public static IEnumerator Send(string method, string url, string jsonBody,
                                       string bearer, Action<bool, long, string> onDone)
        {
            using (var req = new UnityWebRequest(url, method))
            {
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] raw = Encoding.UTF8.GetBytes(jsonBody);
                    req.uploadHandler = new UploadHandlerRaw(raw);
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                if (!string.IsNullOrEmpty(bearer))
                    req.SetRequestHeader("Authorization", "Bearer " + bearer);

                yield return req.SendWebRequest();

                bool ok = req.result == UnityWebRequest.Result.Success;
                string body = req.downloadHandler != null ? req.downloadHandler.text : "";
                if (onDone != null) onDone(ok, req.responseCode, body);
            }
        }

        public static IEnumerator SendForm(string url, string formBody,
                                           Action<bool, long, string> onDone)
        {
            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] raw = Encoding.UTF8.GetBytes(formBody);
                req.uploadHandler = new UploadHandlerRaw(raw);
                req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                req.downloadHandler = new DownloadHandlerBuffer();

                yield return req.SendWebRequest();

                bool ok = req.result == UnityWebRequest.Result.Success;
                string body = req.downloadHandler != null ? req.downloadHandler.text : "";
                if (onDone != null) onDone(ok, req.responseCode, body);
            }
        }
    }
}
