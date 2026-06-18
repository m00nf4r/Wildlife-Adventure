using System;

namespace WildlifeAdventure
{
    /// <summary>A player record stored in the Firestore "users" collection.</summary>
    [Serializable]
    public class UserProfile
    {
        public string uid;
        public string displayName;
        public string email;
        public int bestScore;
        public string rank;
        public int discoveredCount;
        public int totalPlays;
        public int timeBestSeconds;
        public string discoveredCsv; // comma-separated species ids
    }

    /// <summary>A single run result stored in the Firestore "scores" collection.</summary>
    [Serializable]
    public class ScoreEntry
    {
        public string uid;
        public string displayName;
        public string rank;
        public int score;
        public int timeSeconds;
        public string createdAt;
    }

    // ---- JSON shapes for Firebase Auth (parsed with JsonUtility) ----

    [Serializable]
    public class AuthResponse
    {
        public string idToken;
        public string refreshToken;
        public string expiresIn;
        public string localId;
        public string email;
        public string displayName;
    }

    [Serializable]
    public class RefreshResponse
    {
        public string id_token;
        public string refresh_token;
        public string expires_in;
        public string user_id;
    }

    [Serializable]
    public class ErrorResponse
    {
        public ErrorBody error;
    }

    [Serializable]
    public class ErrorBody
    {
        public int code;
        public string message;
    }
}
