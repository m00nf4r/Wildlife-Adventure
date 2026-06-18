# Wildlife Adventure — Update Changelog

This document maps every change to the requirements provided by the supervisor.
The game is still generated entirely from code at runtime by `GameBootstrap`
(no hand-authored scene), so all of this works by attaching the single
`GameBootstrap` component and pressing Play / building to WebGL.

---

## Requirement → What changed

### 1. Increase complexity / "make the user think"
- Quiz question bank rewritten with harder, more thought-provoking conservation
  questions, tagged by difficulty (`WildlifeDatabase.cs`).
- Harder difficulties add a **per-question countdown timer**, so the player must
  recall under pressure (`QuizManager.cs`).
- Collectibles are now **hidden** and spread across the whole play area, so the
  level requires real exploration rather than a straight fly-through.

### 2. Difficulty levels (Easy 5 / Hard 7 / Extra Hard 10 questions)
- New `Difficulty` enum and `DifficultyInfo` helper (`GameData.cs`):
  - Easy = 5 questions, untimed.
  - Hard = 7 questions, 20s per question.
  - Extra Hard = 10 questions, 12s per question.
- `WildlifeDatabase.SelectForDifficulty()` picks the right number of questions,
  preferring questions tagged at that tier and topping up with harder ones.
- The player chooses the level on the main menu (`MainMenuUI.cs`), which calls
  `GameManager.StartGame(Difficulty)`.

### 3. Add / Delete / Edit (CRUD)
- New **Content Manager** screen (`ContentManagerUI.cs`) reachable from the main
  menu: list questions (paginated), **Add**, **Edit**, **Delete**, and
  **Reset to defaults**.
- Persistence: `WildlifeDatabase.AddQuestion/UpdateQuestion/DeleteQuestion/
  ResetQuizToDefault` save to local storage (`PlayerPrefs`) and are restored on
  startup via `LoadCustomQuizIfAny()`.
- Cloud sync: when signed in, changes also PATCH/DELETE `quizQuestions/{id}` in
  Firestore (`Backend.UpsertQuizQuestion/DeleteQuizQuestion`,
  `FirestoreClient.DeleteDocument`).

### 4. Make trash (and animals) hard to find
- Animals and litter are now **concealed in foliage** and only revealed when Wira
  flies close (`WildlifeEntity.cs`, `PollutionItem.cs`, new `Foliage.cs` bush art).
- They are spread across the **full vertical range** and pushed **off the straight
  y=0 lane** (`HabitatBuilder.cs`), so you can't spot everything by flying in a line.
- The reveal radius shrinks on harder difficulties (`DifficultyInfo.RevealRadius`).

### 5. More engaging / interesting
- Camera-based nose control, hidden collectibles, an obstacle course of solid
  trees, difficulty selection, the timer, and the in-game question editor all add
  variety and replay value.

### 6. Trees are solid and deadly (hit a tree = fail = restart)
- New `ObstacleRegistry` + `TreeObstacle` register a narrow "trunk" rectangle for
  each foreground tree.
- `PlayerController` checks the registry every frame; touching a trunk calls
  `GameManager.FailLevel()`.
- New `LevelFailedUI` shows a **LEVEL FAILED** screen with **Restart Level**
  (same difficulty) and **Main Menu**.
- Foreground trees are spaced with clear gaps to weave through and kept away from
  the spawn point so the player never dies instantly.

### 7. Nose-detection movement in exploration (device camera)
- New `NoseInput.cs` + `Assets/Plugins/WebGL/NoseControl.jslib`: in WebGL the
  webcam feeds Google MediaPipe FaceMesh; the nose tip drives Wira's movement,
  with a small mirrored camera preview bottom-right.
- Keyboard (Arrow keys / WASD) remains as an automatic fallback in the Editor,
  desktop, or if the camera is unavailable/denied.
- `docs/index.html` exposes `window.unityInstance` so the plugin can send nose
  coordinates back into the game.

### 8. Main page chooses level; sign in only on button press
- The game now **opens straight to the main menu in offline mode** — no forced
  login screen at startup (`GameManager.Begin`).
- The main menu has level buttons plus a **Sign In / Sign Up** button; signing in
  only happens when the player taps it. A previous cloud session is still restored
  silently in the background if available.

---

## New files
- `Assets/Scripts/ObstacleRegistry.cs` — solid-trunk collision registry.
- `Assets/Scripts/TreeObstacle.cs` — registers a tree's deadly trunk rectangle.
- `Assets/Scripts/NoseInput.cs` — nose/keyboard movement input source.
- `Assets/Scripts/LevelFailedUI.cs` — "Level Failed" / restart screen.
- `Assets/Scripts/ContentManagerUI.cs` — quiz question CRUD UI.
- `Assets/Scripts/Foliage.cs` — procedural bush sprite for concealment.
- `Assets/Plugins/WebGL/NoseControl.jslib` — WebGL webcam → FaceMesh → Unity bridge.
- `.meta` files for all of the above.

## Modified files
- `GameData.cs`, `WildlifeDatabase.cs`, `QuizManager.cs`, `GameManager.cs`,
  `GameBootstrap.cs`, `HabitatBuilder.cs`, `MainMenuUI.cs`, `PlayerController.cs`,
  `WildlifeEntity.cs`, `PollutionItem.cs`, `FirestoreClient.cs`, `Backend.cs`,
  `docs/index.html`, `README.md`.

---

## Setup reminders
- **WebGL must be served over HTTPS or http://localhost** for the camera to work
  (browsers block webcam on `file://` and plain `http://`).
- The player must grant camera permission; otherwise the game falls back to the
  keyboard automatically.
- If you use a custom WebGL template, keep the line
  `window.unityInstance = unityInstance;` inside the
  `createUnityInstance(...).then(...)` callback.
- In the Unity Editor, movement uses the keyboard (the camera plugin only runs in
  actual WebGL builds).
