# 🌿 Wildlife Adventure

A 2D educational game that raises **biodiversity awareness** in children, set in Malaysia's **Belum-Temenggor rainforest**. Players take on the role of **Wira the Hornbill**, a Junior Ranger who explores the forest, discovers endangered wildlife, cleans up pollution, and tests their knowledge in a quiz — all while learning about real Malaysian species and conservation.

Developed as a **Final Year Project (FYP)**.

> **Built with:** Unity 6 · C# · Cloud Firestore + Firebase Authentication (REST API) · WebGL

---

## 📖 About the game

Wildlife Adventure teaches conservation through play. As Wira the Hornbill, the player:

- **Explores** a side-scrolling rainforest habitat.
- **Discovers wildlife** by scanning animals, unlocking illustrated **Fact Cards** saved to a **Field Journal**.
- **Cleans up pollution** (litter, plastic, bottles) to earn Conservation Points.
- **Takes a Level Quiz** once all animals are found, reinforcing what they learned.
- **Earns a Ranger Rank** (Ranger Cadet → Junior Ranger → Senior Ranger → Nature Hero) based on their score.

The featured species are all real and native to the region: the **Malayan Tapir**, **Malayan Tiger**, **Sumatran Rhinoceros**, and **Asian Elephant**.

---

## ✨ Features

- Free-flight exploration of a hand-illustrated rainforest level
- **Three difficulty levels** chosen on the main menu: **Easy (5 questions)**, **Hard (7, timed)**, **Extra Hard (10, fast timer)**
- **Hidden collectibles** — animals and litter are concealed in foliage and revealed only when Wira flies close (reveal radius shrinks on harder levels)
- **Solid, deadly trees** — Wira cannot pass through a trunk; hitting one fails the level and it must be restarted
- **Question Content Manager (CRUD)** — add, edit and delete quiz questions in-game; saved locally and synced to Firebase when signed in
- **Opt-in sign in** — the game opens straight to the menu offline; you only sign in / sign up when you tap the button
- Discoverable wildlife with educational Fact Cards and a Field Journal
- Pollution clean-up mechanic tied to conservation points
- Multiple-choice Level Quiz with instant feedback, explanations and a per-question timer on harder levels
- Ranger Rank progression and an end-of-run reward screen
- **Cloud-powered:** player accounts, content, and a global leaderboard stored in Firebase
- **Offline-tolerant:** plays fully without a connection, using built-in content and local saves

---

## 🎮 Controls

| Action | Keys |
|---|---|
| Move Wira (fallback) | Arrow Keys or WASD (Editor / desktop, or if no camera) |
| Scan wildlife / Clean pollution | Hover close for a moment (auto), or press E |
| Open / close Field Journal | J |

**Nose control:** when the WebGL build loads, allow camera access. A small mirrored preview appears in the
bottom-right; move your nose to fly Wira. Keyboard always works as a fallback.

**Watch out:** the foreground trees are **solid** — crash into a trunk and the level fails and must be restarted.
Animals and litter are **hidden** in the foliage; explore the whole space (not just a straight line) to reveal them.

Discover all the animals to unlock the **Ranger Outpost** quiz at the far right of the level.

---

## 🛠️ Tech stack

| Area | Technology |
|---|---|
| Engine | Unity 6 (2D, Built-in Render Pipeline) |
| Language | C# |
| UI | Unity uGUI (built from code at runtime) |
| Backend | Google Firebase — **Cloud Firestore** + **Firebase Authentication** (Email/Password) |
| Connection | Firebase **REST API** (no SDK — works identically in Editor and WebGL) |
| Target platform | WebGL (also runs in the Editor and as standalone) |

The entire game is generated from code at runtime by a single `GameBootstrap` script — there is no hand-authored scene, which keeps the project lightweight and version-control friendly.

---

## ☁️ Firebase setup (for the online version)

To enable accounts, cloud-stored content, and the leaderboard:

1. Create a project at [console.firebase.google.com](https://console.firebase.google.com).
2. Register a **Web app** and copy the **Web API Key** and **Project ID**.
3. Enable **Authentication → Email/Password**.
4. Create a **Cloud Firestore** database (production mode).
5. Publish these security rules:

   ```
   rules_version = '2';
   service cloud.firestore {
     match /databases/{database}/documents {
       match /wildlifeFacts/{doc}  { allow read: if true; allow write: if request.auth != null; }
       match /quizQuestions/{doc}  { allow read: if true; allow write: if request.auth != null; }
       match /users/{uid}          { allow read, write: if request.auth != null && request.auth.uid == uid; }
       match /scores/{doc}         { allow read: if true; allow create: if request.auth != null; }
     }
   }
   ```

6. In Unity, paste your **Web API Key** and **Project ID** into the `GameBootstrap` component.
7. Press Play → **Create Account** → click **"Upload content to Firebase"** once to seed the wildlife facts and quiz.

The Web API Key is **not a secret** — it is safe to include in a client build; access is controlled by the security rules above.

### Firestore collections

| Collection | Purpose |
|---|---|
| `users/{uid}` | Player profile and progress (best score, rank, discovered species, plays) |
| `wildlifeFacts/{id}` | The Fact Card content |
| `quizQuestions/{id}` | The quiz bank |
| `scores/{autoId}` | Leaderboard entries |

---

## 📁 Project structure

```
WildlifeAdventure/
├── Assets/
│   ├── Scripts/        # All gameplay, UI, and Firebase C# scripts
│   ├── Resources/
│   │   └── Sprites/    # Character and object artwork (loaded by name at runtime)
│   └── Plugins/
│       └── WebGL/      # NoseControl.jslib — webcam nose-tracking bridge (WebGL only)
├── Packages/           # Unity package manifest
└── ProjectSettings/    # Unity project configuration
```

---

## 📝 Notes

- The game's content (fact cards and quiz) lives in Firestore once seeded, but a built-in copy in `WildlifeDatabase.cs` serves as both the seed source and the offline fallback.
- Sprite artwork is loaded by name from `Assets/Resources/Sprites/` — no manual Inspector wiring needed.

---

## 👤 Credits

Developed by **[@m00nf4r](https://github.com/m00nf4r)** as a Final Year Project.

Wildlife facts and conservation themes are based on real Malaysian species of the Belum-Temenggor rainforest.
