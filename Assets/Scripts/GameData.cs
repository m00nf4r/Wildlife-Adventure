using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>High-level state machine for the whole game flow.</summary>
    public enum GameState
    {
        Auth,
        MainMenu,
        Intro,
        Exploring,
        FactCard,
        FieldJournal,
        Quiz,
        Reward,
        Leaderboard,
        LevelFailed,    // Wira hit a tree -> run failed, must restart
        ContentManager  // add / edit / delete quiz questions
    }

    /// <summary>
    /// The three playable levels chosen on the main page. Each tier asks a
    /// different number of quiz questions and ramps up the challenge: harder
    /// tiers add a shrinking per-question timer and prefer tougher questions.
    /// </summary>
    public enum Difficulty
    {
        Easy = 0,       // 5 questions, relaxed
        Hard = 1,       // 7 questions, timed
        ExtraHard = 2   // 10 questions, fast timer
    }

    public static class DifficultyInfo
    {
        /// <summary>How many quiz questions each level asks.</summary>
        public static int Questions(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy:      return 5;
                case Difficulty.Hard:      return 7;
                case Difficulty.ExtraHard: return 10;
            }
            return 5;
        }

        /// <summary>Seconds allowed per question. 0 means no timer (Easy).</summary>
        public static float SecondsPerQuestion(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy:      return 0f;    // no pressure
                case Difficulty.Hard:      return 20f;
                case Difficulty.ExtraHard: return 12f;
            }
            return 0f;
        }

        /// <summary>How tightly wildlife/litter are hidden (reveal radius shrinks on harder tiers).</summary>
        public static float RevealRadius(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy:      return 2.6f;
                case Difficulty.Hard:      return 2.1f;
                case Difficulty.ExtraHard: return 1.7f;
            }
            return 2.4f;
        }

        public static string Display(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy:      return "Easy";
                case Difficulty.Hard:      return "Hard";
                case Difficulty.ExtraHard: return "Extra Hard";
            }
            return "Easy";
        }
    }

    /// <summary>
    /// One record in the "Wildlife Facts" table described in the report.
    /// Plain serializable class so it needs no asset wiring.
    /// </summary>
    [System.Serializable]
    public class WildlifeData
    {
        public string id;               // unique key, e.g. "malayan_tapir"
        public string commonName;       // "Malayan Tapir"
        public string scientificName;   // "Tapirus indicus"
        public string conservationStatus; // "Endangered"
        public string fact;             // child-friendly fact text
        public string spriteName;       // file in Resources/Sprites (no extension)
        public int pointsAwarded = 250; // Conservation Points for discovery

        public WildlifeData(string id, string commonName, string scientificName,
                            string status, string fact, string spriteName)
        {
            this.id = id;
            this.commonName = commonName;
            this.scientificName = scientificName;
            this.conservationStatus = status;
            this.fact = fact;
            this.spriteName = spriteName;
        }
    }

    /// <summary>One multiple-choice quiz question (the Quiz Manager bank).</summary>
    [System.Serializable]
    public class QuizQuestion
    {
        public string id;          // stable key for edit/delete (auto if empty)
        public string question;
        public string[] options;   // exactly 4
        public int correctIndex;   // 0..3
        public string explanation;
        public int difficulty;     // 0 Easy, 1 Hard, 2 Extra Hard (tier this Q belongs to)

        public QuizQuestion(string question, string[] options, int correctIndex, string explanation)
        {
            this.question = question;
            this.options = options;
            this.correctIndex = correctIndex;
            this.explanation = explanation;
            this.difficulty = 0;
            this.id = System.Guid.NewGuid().ToString("N").Substring(0, 10);
        }

        public QuizQuestion(string question, string[] options, int correctIndex,
                            string explanation, int difficulty, string id = null)
        {
            this.question = question;
            this.options = options;
            this.correctIndex = correctIndex;
            this.explanation = explanation;
            this.difficulty = difficulty;
            this.id = string.IsNullOrEmpty(id)
                ? System.Guid.NewGuid().ToString("N").Substring(0, 10)
                : id;
        }
    }

    /// <summary>
    /// Anything in the world the player (Wira) can interact with:
    /// wildlife to discover, pollution to clean, or the quiz totem.
    /// </summary>
    public interface IInteractable
    {
        Vector3 WorldPosition { get; }
        string Prompt { get; }       // e.g. "Press E to scan"
        bool Available { get; }      // can it be interacted with right now?
        void Interact();
    }
}
