using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// THE ONLY SCRIPT YOU NEED TO ATTACH. Put this on a single empty
    /// GameObject in an otherwise empty scene and press Play. It creates the
    /// camera setup, the EventSystem, Wira the player, every UI module and the
    /// GameManager, then wires them together.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Orthographic half-height of the camera view, in world units.")]
        public float cameraSize = 5f;

        [Tooltip("On-screen scale of Wira the Hornbill.")]
        public float playerScale = 0.65f;

        [Header("Firebase Configuration")]
        [Tooltip("Firebase Web API Key (Project Settings > General > Web API Key).")]
        public string firebaseApiKey = "AIzaSyDk59zhzuz-4JHjKjuYVQ7MC_aImrZimlA";

        [Tooltip("Firebase Project ID (Project Settings > General > Project ID).")]
        public string firebaseProjectId = "wildlife-awareness-game";

        void Awake()
        {
            // ----- Backend (must exist before GameManager starts) -----
            var backendGo = new GameObject("Backend");
            backendGo.AddComponent<Backend>();
            Backend.Configure(firebaseApiKey, firebaseProjectId);
            SetupCamera(out Camera cam, out CameraFollow camFollow);
            SetupEventSystem();

            // ----- Player: Wira the Hornbill -----
            var playerGo = new GameObject("Wira");
            var sr = playerGo.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/hornbill");
            sr.sortingOrder = 10;
            playerGo.transform.localScale = Vector3.one * playerScale;
            var player = playerGo.AddComponent<PlayerController>();
            camFollow.target = playerGo.transform;

            // ----- UI + system modules (build before GameManager starts) -----
            var menu     = MakeModule<MainMenuUI>("MainMenu");
            var dialogue = MakeModule<DialogueUI>("Dialogue");
            var hud      = MakeModule<HUDController>("HUD");
            var factCard = MakeModule<FactCardUI>("FactCard");
            var journal  = MakeModule<FieldJournalUI>("Journal");
            var quiz     = MakeModule<QuizManager>("Quiz");
            var reward   = MakeModule<RewardScreenUI>("Reward");
            var auth     = MakeModule<AuthUI>("Auth");
            var leaderboard = MakeModule<LeaderboardUI>("Leaderboard");
            var levelFailed = MakeModule<LevelFailedUI>("LevelFailed");
            var contentManager = MakeModule<ContentManagerUI>("ContentManager");

            menu.Build();
            dialogue.Build();
            hud.Build();
            factCard.Build();
            journal.Build();
            quiz.Build();
            reward.Build();
            auth.Build();
            leaderboard.Build();
            levelFailed.Build();
            contentManager.Build();

            // ----- Nose-tracking input (camera-driven movement for exploration).
            // The GameObject MUST be named "NoseInput" because the WebGL plugin
            // calls UnityInstance.SendMessage("NoseInput", ...) by that name.
            var noseGo = new GameObject("NoseInput");
            var noseInput = noseGo.AddComponent<NoseInput>();

            // ----- Habitat -----
            var habitatGo = new GameObject("HabitatBuilder");
            var habitat = habitatGo.AddComponent<HabitatBuilder>();
            habitat.Configure(player, camFollow, cam);

            // ----- GameManager (its Start() drives the flow) -----
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.menu = menu;
            gm.dialogue = dialogue;
            gm.hud = hud;
            gm.factCard = factCard;
            gm.journal = journal;
            gm.quiz = quiz;
            gm.reward = reward;
            gm.habitat = habitat;
            gm.player = player;
            gm.cam = camFollow;
            gm.auth = auth;
            gm.leaderboard = leaderboard;
            gm.levelFailed = levelFailed;
            gm.contentManager = contentManager;
            gm.noseInput = noseInput;
        }

        T MakeModule<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            return go.GetComponent<T>();
        }

        void SetupCamera(out Camera cam, out CameraFollow follow)
        {
            cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            cam.transform.position = new Vector3(0, 0.5f, -10f);
            cam.backgroundColor = UIFactory.Sky;
            cam.clearFlags = CameraClearFlags.SolidColor;

            follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        }

        void SetupEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            // StandaloneInputModule works with the legacy Input Manager (project default).
            es.AddComponent<StandaloneInputModule>();
        }
    }
}