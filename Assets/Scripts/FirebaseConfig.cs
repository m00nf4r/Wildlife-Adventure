namespace WildlifeAdventure
{
    /// <summary>
    /// Holds the Firebase Web API key and Project ID. These are NOT secrets
    /// (the Web API key is safe to ship in a client; access is controlled by
    /// Firebase Security Rules). Set via GameBootstrap in the Inspector.
    /// </summary>
    public static class FirebaseConfig
    {
        public static string ApiKey = "";
        public static string ProjectId = "";

        public static bool IsConfigured =>
            !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ProjectId);

        // Firestore REST base for the (default) database.
        public static string FirestoreBase =>
            "https://firestore.googleapis.com/v1/projects/" + ProjectId +
            "/databases/(default)/documents";

        public static string IdentityBase => "https://identitytoolkit.googleapis.com/v1/accounts:";
        public static string SecureTokenUrl => "https://securetoken.googleapis.com/v1/token?key=" + ApiKey;
    }
}
