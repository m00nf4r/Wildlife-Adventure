using System.Collections.Generic;

namespace WildlifeAdventure
{
    /// <summary>
    /// Central store for the game's educational content. The active lists
    /// (Species, Quiz) are replaced with cloud data by <see cref="Backend"/> when
    /// the player is signed in; otherwise they hold the built-in defaults below,
    /// which also serve as the seed source and the offline fallback.
    /// This is the code equivalent of the report's "Wildlife Facts" table plus
    /// the quiz bank, kept separate from gameplay logic so content can grow.
    /// </summary>
    public static class WildlifeDatabase
    {
        // Active content (may be swapped for cloud data at runtime).
        public static List<WildlifeData> Species = DefaultSpecies();
        public static List<QuizQuestion> Quiz = DefaultQuiz();

        public static WildlifeData GetById(string id)
        {
            foreach (var s in Species)
                if (s.id == id) return s;
            return null;
        }

        public static int TotalSpecies => Species.Count;

        // =====================================================================
        //  Quiz selection by difficulty
        // =====================================================================

        /// <summary>
        /// Picks <paramref name="count"/> questions appropriate for the chosen
        /// level. Questions tagged for that tier are preferred; if the tier is
        /// short, the remainder is topped up from the rest of the pool. The
        /// result is shuffled, so each play-through differs.
        /// </summary>
        public static List<QuizQuestion> SelectForDifficulty(Difficulty d, int count)
        {
            int tier = (int)d;
            var tierMatches = new List<QuizQuestion>();
            var others = new List<QuizQuestion>();
            foreach (var q in Quiz)
            {
                if (q.difficulty == tier) tierMatches.Add(q);
                else others.Add(q);
            }

            Shuffle(tierMatches);
            Shuffle(others);

            var picked = new List<QuizQuestion>();
            foreach (var q in tierMatches) { if (picked.Count >= count) break; picked.Add(q); }
            // Prefer harder leftovers first when topping up Extra Hard, else any.
            others.Sort((a, b) => b.difficulty.CompareTo(a.difficulty));
            foreach (var q in others) { if (picked.Count >= count) break; picked.Add(q); }

            Shuffle(picked);
            return picked;
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        // =====================================================================
        //  CRUD on the quiz bank (used by the Content Manager screen)
        // =====================================================================

        public static void AddQuestion(QuizQuestion q)
        {
            if (q == null) return;
            if (string.IsNullOrEmpty(q.id))
                q.id = System.Guid.NewGuid().ToString("N").Substring(0, 10);
            Quiz.Add(q);
            SaveCustomQuiz();
        }

        public static void UpdateQuestion(QuizQuestion q)
        {
            if (q == null) return;
            for (int i = 0; i < Quiz.Count; i++)
            {
                if (Quiz[i].id == q.id) { Quiz[i] = q; SaveCustomQuiz(); return; }
            }
            // Not found by id -> treat as add.
            AddQuestion(q);
        }

        public static void DeleteQuestion(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            Quiz.RemoveAll(q => q.id == id);
            SaveCustomQuiz();
        }

        public static void ResetQuizToDefault()
        {
            Quiz = DefaultQuiz();
            UnityEngine.PlayerPrefs.DeleteKey(QUIZ_KEY);
            UnityEngine.PlayerPrefs.Save();
        }

        // ---------------- local persistence (PlayerPrefs / WebGL storage) ----
        const string QUIZ_KEY = "wa_custom_quiz_v1";

        /// <summary>Serialises the current quiz bank to local storage.</summary>
        public static void SaveCustomQuiz()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('[');
            for (int i = 0; i < Quiz.Count; i++)
            {
                var q = Quiz[i];
                if (i > 0) sb.Append(',');
                sb.Append('{');
                sb.Append("\"id\":\"").Append(MiniJson.Escape(q.id)).Append("\",");
                sb.Append("\"question\":\"").Append(MiniJson.Escape(q.question)).Append("\",");
                sb.Append("\"correctIndex\":").Append(q.correctIndex).Append(',');
                sb.Append("\"difficulty\":").Append(q.difficulty).Append(',');
                sb.Append("\"explanation\":\"").Append(MiniJson.Escape(q.explanation)).Append("\",");
                sb.Append("\"options\":[");
                for (int k = 0; k < 4; k++)
                {
                    string opt = (q.options != null && k < q.options.Length) ? q.options[k] : "";
                    if (k > 0) sb.Append(',');
                    sb.Append('"').Append(MiniJson.Escape(opt)).Append('"');
                }
                sb.Append("]}");
            }
            sb.Append(']');
            UnityEngine.PlayerPrefs.SetString(QUIZ_KEY, sb.ToString());
            UnityEngine.PlayerPrefs.Save();
        }

        /// <summary>Loads a previously saved custom quiz bank, if one exists.</summary>
        public static void LoadCustomQuizIfAny()
        {
            string raw = UnityEngine.PlayerPrefs.GetString(QUIZ_KEY, "");
            if (string.IsNullOrEmpty(raw)) return;
            var parsed = MiniJson.Parse(raw) as List<object>;
            if (parsed == null || parsed.Count == 0) return;

            var list = new List<QuizQuestion>();
            foreach (var item in parsed)
            {
                var o = item as Dictionary<string, object>;
                if (o == null) continue;
                string id = MiniJson.Str(o, "id");
                string question = MiniJson.Str(o, "question");
                string expl = MiniJson.Str(o, "explanation");
                int correct = ParseInt(MiniJson.Get(o, "correctIndex"));
                int diff = ParseInt(MiniJson.Get(o, "difficulty"));
                var optsArr = MiniJson.Get(o, "options") as List<object>;
                var opts = new string[4];
                for (int k = 0; k < 4; k++)
                    opts[k] = (optsArr != null && k < optsArr.Count && optsArr[k] != null)
                        ? optsArr[k].ToString() : "";
                list.Add(new QuizQuestion(question, opts, correct, expl, diff, id));
            }
            if (list.Count > 0) Quiz = list;
        }

        static int ParseInt(object o)
        {
            if (o == null) return 0;
            if (o is long l) return (int)l;
            if (o is double d) return (int)d;
            int.TryParse(o.ToString(), out int r);
            return r;
        }

        // ---------- Built-in defaults (seed + offline fallback) ----------
        // Real Malaysian wildlife of the Belum-Temenggor rainforest.
        public static List<WildlifeData> DefaultSpecies()
        {
            return new List<WildlifeData>
            {
                new WildlifeData(
                    "malayan_tapir", "Malayan Tapir", "Tapirus indicus", "Endangered",
                    "The Malayan Tapir is the largest of all tapirs. Its black-and-white " +
                    "coat looks bold to us, but at night it works like camouflage, " +
                    "breaking up its shape so predators can't spot it. Baby tapirs are " +
                    "born brown with stripes and spots, like a walking watermelon!",
                    "Tapir"),

                new WildlifeData(
                    "malayan_tiger", "Malayan Tiger", "Panthera tigris jacksoni", "Critically Endangered",
                    "The Malayan Tiger is a national symbol of Malaysia, appearing on the " +
                    "country's coat of arms. Fewer than 150 are believed to remain in the " +
                    "wild. Each tiger's stripes are unique, just like a human fingerprint.",
                    "Tiger"),

                new WildlifeData(
                    "sumatran_rhino", "Sumatran Rhinoceros", "Dicerorhinus sumatrensis", "Critically Endangered",
                    "The Sumatran Rhino is the smallest rhino on Earth and the only Asian " +
                    "rhino with two horns. It is also the hairiest, with a reddish-brown " +
                    "coat. Sadly, it is one of the rarest large mammals in the world.",
                    "rhinoceros"),

                new WildlifeData(
                    "asian_elephant", "Asian Elephant", "Elephas maximus", "Endangered",
                    "Asian Elephants are gardeners of the forest. They spread seeds across " +
                    "huge distances in their dung, helping new trees grow. They are smaller " +
                    "than African elephants and have one finger-like tip on their trunk.",
                    "elephant"),
            };
        }

        public static List<QuizQuestion> DefaultQuiz()
        {
            return new List<QuizQuestion>
            {
                // ---------------- EASY (difficulty 0) ----------------
                new QuizQuestion(
                    "Which of these is a flagship species for Malaysian rainforest conservation?",
                    new[] { "Koala", "Giant Panda", "Malayan Tiger", "African Lion" }, 2,
                    "The Malayan Tiger is a flagship species and appears on Malaysia's coat of arms.", 0),

                new QuizQuestion(
                    "What does the Malayan Tapir's black-and-white coat help it do at night?",
                    new[] { "Glow in the dark", "Stay camouflaged", "Swim faster", "Attract a mate" }, 1,
                    "At night the bold pattern breaks up its body shape, hiding it from predators.", 0),

                new QuizQuestion(
                    "How is the Sumatran Rhino different from other rhinos?",
                    new[] { "It has no horn", "It is the largest rhino", "It has two horns and is hairy", "It can fly" }, 2,
                    "The Sumatran Rhino is the smallest, hairiest rhino and the only Asian rhino with two horns.", 0),

                new QuizQuestion(
                    "Why are Asian Elephants called gardeners of the forest?",
                    new[] { "They plant trees with their trunks", "They spread seeds in their dung",
                            "They water the plants", "They cut down dead trees" }, 1,
                    "Elephants spread seeds over long distances in their dung, helping new trees grow.", 0),

                new QuizQuestion(
                    "What is the biggest threat to wildlife shown in this game?",
                    new[] { "Too much rain", "Pollution and habitat loss", "Bright sunlight", "Cold weather" }, 1,
                    "Pollution and the loss of forest habitat are major threats to Malaysian wildlife.", 0),

                new QuizQuestion(
                    "Where is the rainforest in this game located?",
                    new[] { "Sahara Desert", "Belum-Temenggor, Malaysia", "Amazon, Brazil", "Arctic Circle" }, 1,
                    "The game is set in the Belum-Temenggor rainforest in northern Peninsular Malaysia.", 0),

                // ---------------- HARD (difficulty 1) ----------------
                new QuizQuestion(
                    "A baby Malayan Tapir looks very different from its parents. How?",
                    new[] { "It is pure white", "It is brown with stripes and spots",
                            "It has no legs yet", "It is bright green" }, 1,
                    "Tapir calves are brown with stripes and spots — like a 'walking watermelon' — for camouflage.", 1),

                new QuizQuestion(
                    "Roughly how many Malayan Tigers are believed to remain in the wild?",
                    new[] { "Fewer than 150", "About 5,000", "Around 50,000", "More than a million" }, 0,
                    "Estimates put wild Malayan Tigers at fewer than 150, making them critically endangered.", 1),

                new QuizQuestion(
                    "If the top predator (the tiger) disappeared, what is the MOST likely result?",
                    new[] { "Nothing would change", "Prey animals could overpopulate and damage the forest",
                            "All trees would instantly die", "The rivers would dry up" }, 1,
                    "Removing a top predator unbalances the food web; prey can overpopulate and over-graze the forest.", 1),

                new QuizQuestion(
                    "Two animals share the same forest but eat different food and feed at different times. This is an example of:",
                    new[] { "Competition for the exact same niche", "Sharing resources to reduce conflict",
                            "Migration", "Hibernation" }, 1,
                    "Splitting food and feeding times lets species share a habitat without directly competing.", 1),

                new QuizQuestion(
                    "Why does a single piece of plastic litter harm MORE than one animal?",
                    new[] { "It only affects the animal that eats it", "It can move through the food chain and pollute water for many",
                            "Plastic disappears in a day", "Animals enjoy eating plastic" }, 1,
                    "Plastic breaks into smaller pieces, pollutes water and soil, and moves up the food chain affecting many animals.", 1),

                new QuizQuestion(
                    "Each tiger's stripe pattern is unique. Why is this useful to researchers?",
                    new[] { "It tells you the tiger's age", "It lets them identify individual tigers from photos",
                            "It shows what the tiger ate", "It predicts the weather" }, 1,
                    "Like fingerprints, unique stripes let researchers identify individuals using camera-trap photos to count them.", 1),

                new QuizQuestion(
                    "A 'keystone species' is one that:",
                    new[] { "Is the biggest animal in the forest", "Has a very large effect on its ecosystem relative to its numbers",
                            "Only lives in caves", "Eats only stones" }, 1,
                    "Keystone species (like elephants) shape their whole ecosystem far more than their population size suggests.", 1),

                // ---------------- EXTRA HARD (difficulty 2) ----------------
                new QuizQuestion(
                    "A road is built that splits one large forest into two smaller patches. The biggest danger to large animals is:",
                    new[] { "More sunlight on the road", "Habitat fragmentation isolating populations",
                            "The road colour", "Louder bird songs" }, 1,
                    "Fragmentation isolates populations, shrinks gene pools and blocks animals from finding food and mates.", 2),

                new QuizQuestion(
                    "The Sumatran Rhino population is tiny and scattered. Beyond hunting, why is recovery so hard?",
                    new[] { "They refuse to eat", "Isolated individuals rarely meet to breed",
                            "They live underwater", "They only come out in winter" }, 1,
                    "With so few rhinos spread across fragmented forest, they rarely meet, so natural breeding almost stops.", 2),

                new QuizQuestion(
                    "If elephants (seed dispersers) vanished, what long-term forest change is MOST likely?",
                    new[] { "Faster tree growth everywhere", "Fewer large-seeded trees over time",
                            "Instant desert", "More rainfall" }, 1,
                    "Some big-seeded trees rely on elephants to spread their seeds; without them those trees slowly decline.", 2),

                new QuizQuestion(
                    "A conservation team has limited money. Protecting the tiger's habitat ALSO protects tapirs, deer and birds. This idea is called:",
                    new[] { "Wasting resources", "Umbrella species protection", "Selective logging", "Crop rotation" }, 1,
                    "Protecting a wide-ranging 'umbrella species' like the tiger shelters many other species in the same habitat.", 2),

                new QuizQuestion(
                    "Why can a healthy rainforest help slow climate change?",
                    new[] { "It reflects sunlight like ice", "Its trees store large amounts of carbon",
                            "It produces no oxygen", "It cools the planet with wind" }, 1,
                    "Rainforests lock huge amounts of carbon in their trees and soil; clearing them releases it as CO2.", 2),

                new QuizQuestion(
                    "Camera traps photograph 12 tigers, but 4 photos show the SAME stripe pattern twice. The real count is closer to:",
                    new[] { "12", "10", "8", "4" }, 2,
                    "Duplicates must be removed: if 4 of 12 photos are repeats of others, the true number of individuals is lower (~8).", 2),

                new QuizQuestion(
                    "Litter near a river is especially harmful because:",
                    new[] { "Rivers carry pollution far downstream to many habitats", "Water makes plastic safe",
                            "Animals never drink river water", "Rivers destroy all plastic quickly" }, 0,
                    "Flowing water carries litter and toxins far beyond where they were dropped, harming many downstream habitats.", 2),

                new QuizQuestion(
                    "Which action by a Junior Ranger helps wildlife the MOST in the long run?",
                    new[] { "Feeding wild animals snacks", "Protecting and restoring their natural habitat",
                            "Keeping a wild animal as a pet", "Taking eggs from nests" }, 1,
                    "Protecting and restoring habitat supports whole communities of species — far more than feeding individuals.", 2),

                new QuizQuestion(
                    "An animal is 'Critically Endangered'. Compared with 'Endangered', this means it is:",
                    new[] { "Safer", "At even higher risk of extinction", "Already extinct", "Not protected" }, 1,
                    "Critically Endangered is the highest-risk category before Extinct in the Wild — the tiger and rhino are here.", 2),
            };
        }
    }
}
