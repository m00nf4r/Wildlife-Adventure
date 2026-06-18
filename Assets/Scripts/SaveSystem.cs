using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// Persists the player's progress to the browser's local storage
    /// (Unity maps PlayerPrefs to IndexedDB/localStorage on WebGL), matching
    /// the "Load Progress" / "Save Game" use cases. This is the "Players" table.
    /// </summary>
    public static class SaveSystem
    {
        const string KEY_DISCOVERED = "wa_discovered"; // CSV of species ids
        const string KEY_BEST_SCORE = "wa_best_score";
        const string KEY_HAS_SAVE   = "wa_has_save";

        public static void Save(HashSet<string> discovered, int bestScore)
        {
            PlayerPrefs.SetString(KEY_DISCOVERED, string.Join(",", discovered));
            PlayerPrefs.SetInt(KEY_BEST_SCORE, bestScore);
            PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
            PlayerPrefs.Save();
        }

        public static bool HasSave()
        {
            return PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;
        }

        public static HashSet<string> LoadDiscovered()
        {
            var set = new HashSet<string>();
            string raw = PlayerPrefs.GetString(KEY_DISCOVERED, "");
            if (!string.IsNullOrEmpty(raw))
                foreach (var id in raw.Split(','))
                    if (!string.IsNullOrEmpty(id)) set.Add(id);
            return set;
        }

        public static int LoadBestScore()
        {
            return PlayerPrefs.GetInt(KEY_BEST_SCORE, 0);
        }

        public static void ClearSave()
        {
            PlayerPrefs.DeleteKey(KEY_DISCOVERED);
            PlayerPrefs.DeleteKey(KEY_BEST_SCORE);
            PlayerPrefs.DeleteKey(KEY_HAS_SAVE);
            PlayerPrefs.Save();
        }

        /// <summary>Turns a total score into a friendly Ranger Rank.</summary>
        public static string RankForScore(int score)
        {
            if (score >= 1500) return "Nature Hero";
            if (score >= 1000) return "Senior Ranger";
            if (score >= 500)  return "Junior Ranger";
            return "Ranger Cadet";
        }
    }
}
