using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// The centralized controller from the report's architecture. Owns the
    /// game state machine, the Conservation Points score, the set of discovered
    /// species, and the run timer, and routes the player between menu,
    /// exploration, quiz and reward stages.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ---- Runtime state ----
        public GameState State { get; private set; } = GameState.MainMenu;
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public readonly HashSet<string> Discovered = new HashSet<string>();
        public int PollutionCleaned { get; private set; }
        public float RunTime { get; private set; }
        bool timerRunning;

        // Difficulty chosen on the main menu; drives question count, timer and
        // how hard hidden animals/trash are to find. Set BEFORE habitat.Build().
        public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Easy;

        // ---- Injected references (set by GameBootstrap) ----
        public MainMenuUI menu;
        public DialogueUI dialogue;
        public HUDController hud;
        public FactCardUI factCard;
        public FieldJournalUI journal;
        public QuizManager quiz;
        public RewardScreenUI reward;
        public HabitatBuilder habitat;
        public PlayerController player;
        public CameraFollow cam;
        public AuthUI auth;
        public LeaderboardUI leaderboard;
        public LevelFailedUI levelFailed;
        public ContentManagerUI contentManager;

        UserProfile loadedProfile;   // from cloud, if signed in
        int totalPlays;

        public int DiscoveredCount => Discovered.Count;
        public int TotalSpecies => WildlifeDatabase.TotalSpecies;
        public bool AllDiscovered => DiscoveredCount >= TotalSpecies;
        public string CurrentRank => SaveSystem.RankForScore(Mathf.Max(Score, BestScore));

        void Awake()
        {
            Instance = this;
            BestScore = SaveSystem.LoadBestScore();
        }

        void Start()
        {
            Begin();
        }

        /// <summary>
        /// Entry point. The game now ALWAYS opens to the main menu (offline by
        /// default) so the player can pick a level immediately. Signing in only
        /// happens when the player taps the Sign In button on the menu. If a
        /// previous cloud session can be restored silently we still load it, but
        /// we never block the player on a login screen at startup.
        /// </summary>
        public void Begin()
        {
            HideAll();
            WildlifeDatabase.LoadCustomQuizIfAny();   // restore any edited questions

            var be = Backend.Instance;
            if (be != null && be.IsConfigured)
            {
                be.TrySilentLogin(ok =>
                {
                    if (ok) OnSignedIn();   // restores profile then -> main menu
                    else OnOffline();       // straight to main menu, no auth wall
                });
            }
            else
            {
                OnOffline();
            }
        }

        /// <summary>Open the sign in / sign up screen (from the menu button or logout).</summary>
        public void ShowAuth()
        {
            SetState(GameState.Auth);
            if (menu != null) menu.Hide();
            auth.Show();
        }

        /// <summary>Called by AuthUI after a successful login/registration.</summary>
        public void OnSignedIn()
        {
            auth.SetBusy("Loading your forest...");
            var be = Backend.Instance;
            be.LoadContent(() =>
            {
                be.LoadUser(profile =>
                {
                    loadedProfile = profile;
                    if (profile != null)
                    {
                        BestScore = profile.bestScore;
                        totalPlays = profile.totalPlays;
                    }
                    GoToMainMenu();
                });
            });
        }

        /// <summary>Offline path: no cloud, built-in content + local save.</summary>
        public void OnOffline()
        {
            loadedProfile = null;
            BestScore = SaveSystem.LoadBestScore();
            GoToMainMenu();
        }

        public void Logout()
        {
            if (Backend.Instance != null) Backend.Instance.Logout();
            loadedProfile = null;
            BestScore = SaveSystem.LoadBestScore();
            totalPlays = 0;
            GoToMainMenu();
        }

        void Update()
        {
            if (timerRunning) RunTime += Time.deltaTime;

            // Closing the Field Journal (opening it is handled by PlayerController,
            // which gives priority to "add nearby animal to journal").
            if (State == GameState.FieldJournal &&
                (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Escape)))
                CloseJournal();
        }

        void SetState(GameState s)
        {
            State = s;
            // Player only accepts movement input while freely exploring.
            if (player != null) player.AcceptInput = (s == GameState.Exploring);
        }

        // ---------------- Flow ----------------

        void HideAll()
        {
            if (habitat != null) habitat.SetWorldVisible(false);
            if (hud != null) hud.Hide();
            if (factCard != null) factCard.Hide();
            if (journal != null) journal.Hide();
            if (quiz != null) quiz.Hide();
            if (reward != null) reward.Hide();
            if (dialogue != null) dialogue.Hide();
            if (menu != null) menu.Hide();
            if (auth != null) auth.Hide();
            if (leaderboard != null) leaderboard.Hide();
            if (levelFailed != null) levelFailed.Hide();
            if (contentManager != null) contentManager.Hide();
        }

        public void GoToMainMenu()
        {
            SetState(GameState.MainMenu);
            timerRunning = false;
            HideAll();
            menu.Show();
        }

        /// <summary>Open the question Content Manager (Add / Edit / Delete quiz questions).</summary>
        public void OpenContentManager()
        {
            SetState(GameState.ContentManager);
            timerRunning = false;
            if (menu != null) menu.Hide();
            contentManager.Show(GoToMainMenu);
        }

        public void OpenLeaderboard()
        {
            SetState(GameState.Leaderboard);
            menu.Hide();
            leaderboard.ShowLoading();
            Backend.Instance.LoadLeaderboard(list => leaderboard.Show(list, GoToMainMenu));
        }

        /// <summary>Start a run at the chosen difficulty level (Easy/Hard/Extra Hard).</summary>
        public void StartGame(Difficulty difficulty)
        {
            CurrentDifficulty = difficulty;   // MUST be set before habitat.Build()
            menu.Hide();
            if (contentManager != null) contentManager.Hide();
            if (levelFailed != null) levelFailed.Hide();

            Discovered.Clear();
            PollutionCleaned = 0;
            Score = 0;
            RunTime = 0f;

            habitat.Build();
            habitat.SetWorldVisible(true);
            habitat.RefreshDiscoveredState();
            player.ResetToStart();
            cam.SnapToPlayer();

            // Wira greets the player, then exploration begins.
            SetState(GameState.Intro);
            dialogue.ShowIntro(() =>
            {
                SetState(GameState.Exploring);
                hud.Show();
                hud.Refresh();
                timerRunning = true;
            });
        }

        /// <summary>Replay the current level after failing (same difficulty).</summary>
        public void RetryLevel()
        {
            StartGame(CurrentDifficulty);
        }

        /// <summary>
        /// Called by PlayerController when Wira crashes into a tree trunk. The
        /// level is failed and must be restarted from the beginning.
        /// </summary>
        public void FailLevel()
        {
            if (State == GameState.LevelFailed) return;
            SetState(GameState.LevelFailed);
            timerRunning = false;
            if (hud != null) hud.Hide();
            if (dialogue != null) dialogue.Hide();
            if (factCard != null) factCard.Hide();
            if (levelFailed != null)
                levelFailed.Show(Score, DiscoveredCount, TotalSpecies, RetryLevel, GoToMainMenu);
        }

        // ---------------- Scoring / discovery ----------------

        public void DiscoverSpecies(WildlifeData data)
        {
            if (Discovered.Contains(data.id)) return;
            Discovered.Add(data.id);
            AddScore(data.pointsAwarded);
            hud.Refresh();
        }

        public void CleanPollution(int points)
        {
            PollutionCleaned++;
            AddScore(points);
            hud.Refresh();
        }

        public void AddScore(int amount)
        {
            Score += amount;
            if (hud != null) hud.Refresh();
        }

        // ---------------- Fact Card ----------------

        public void ShowFactCard(WildlifeData data)
        {
            SetState(GameState.FactCard);
            timerRunning = false;
            factCard.Show(data, () =>
            {
                DiscoverSpecies(data);   // "Add to Journal"
                CloseFactCard();
            }, CloseFactCard);
        }

        void CloseFactCard()
        {
            factCard.Hide();
            SetState(GameState.Exploring);
            timerRunning = true;
            habitat.RefreshDiscoveredState();
            hud.Refresh();
        }

        // ---------------- Field Journal ----------------

        public void OpenJournal()
        {
            SetState(GameState.FieldJournal);
            timerRunning = false;
            journal.Show(CloseJournal);
        }

        public void CloseJournal()
        {
            journal.Hide();
            SetState(GameState.Exploring);
            timerRunning = true;
        }

        // ---------------- Quiz ----------------

        public void StartQuiz()
        {
            SetState(GameState.Quiz);
            timerRunning = false;
            quiz.Begin((correctCount) =>
            {
                AddScore(correctCount * 100);
                FinishRun();
            });
        }

        // ---------------- Reward / end ----------------

        public void FinishRun()
        {
            SetState(GameState.Reward);
            timerRunning = false;
            quiz.Hide();
            hud.Hide();

            bool newBest = Score > BestScore;
            if (newBest) BestScore = Score;
            SaveSystem.Save(Discovered, BestScore);   // local cache always

            // Cloud sync (no-op if offline / not signed in)
            var be = Backend.Instance;
            if (be != null && be.IsSignedIn)
            {
                totalPlays += 1;
                int timeBest = loadedProfile != null && loadedProfile.timeBestSeconds > 0
                    ? Mathf.Min(loadedProfile.timeBestSeconds, Mathf.RoundToInt(RunTime))
                    : Mathf.RoundToInt(RunTime);

                var profile = new UserProfile
                {
                    uid = be.Uid,
                    displayName = be.DisplayName,
                    email = FirebaseAuth.Email,
                    bestScore = BestScore,
                    rank = CurrentRank,
                    discoveredCount = DiscoveredCount,
                    totalPlays = totalPlays,
                    timeBestSeconds = timeBest,
                    discoveredCsv = string.Join(",", Discovered)
                };
                loadedProfile = profile;
                be.SaveUser(profile, null);

                be.SubmitScore(new ScoreEntry
                {
                    uid = be.Uid,
                    displayName = be.DisplayName,
                    rank = CurrentRank,
                    score = Score,
                    timeSeconds = Mathf.RoundToInt(RunTime)
                }, null);
            }

            reward.Show(Score, RunTime, CurrentRank, newBest, GoToMainMenu);
        }
    }
}
