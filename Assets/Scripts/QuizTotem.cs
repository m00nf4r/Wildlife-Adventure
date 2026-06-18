using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// A "Ranger Outpost" sign at the end of the trail. It only becomes active
    /// once every species has been added to the Field Journal, then acts as the
    /// gatekeeper to the Level Quiz (the report's "Take Level Quiz" use case).
    /// </summary>
    public class QuizTotem : MonoBehaviour, IInteractable
    {
        SpriteRenderer sr;
        SpriteRenderer glow;
        float pulse;

        public Vector3 WorldPosition => transform.position;
        public string Prompt => Available
            ? "Press E to take the Level Quiz!"
            : "Find all wildlife to unlock the Quiz";
        public bool Available => GameManager.Instance != null && GameManager.Instance.AllDiscovered;

        public void Init(float scale)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = MakeSignSprite();
            sr.sortingOrder = 11;
            transform.localScale = Vector3.one * scale;

            var g = new GameObject("Glow");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0, 0.35f, 0);
            glow = g.AddComponent<SpriteRenderer>();
            glow.sprite = MakeGlow();
            glow.sortingOrder = 10;
            glow.color = new Color(1f, 0.9f, 0.4f, 0f);
            g.transform.localScale = Vector3.one * 2.6f;
        }

        void OnEnable()  { InteractableRegistry.Register(this); }
        void OnDisable() { InteractableRegistry.Unregister(this); }

        void Update()
        {
            pulse += Time.deltaTime * 3f;
            if (glow != null)
            {
                float a = Available ? (0.35f + Mathf.Sin(pulse) * 0.25f) : 0f;
                glow.color = new Color(1f, 0.9f, 0.4f, a);
            }
        }

        public void Interact()
        {
            if (!Available) return;
            GameManager.Instance.StartQuiz();
        }

        // ---- procedural signpost texture ----
        static Sprite MakeSignSprite()
        {
            int w = 96, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0, 0, 0, 0);
            Color post  = UIFactory.Hex("6D4C41");
            Color board = UIFactory.Hex("2E7D32");
            Color edge  = UIFactory.Hex("1B5E20");
            Color star  = UIFactory.Hex("FFE082");

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            // post
            for (int y = 0; y < 70; y++)
                for (int x = w / 2 - 6; x < w / 2 + 6; x++)
                    tex.SetPixel(x, y, post);

            // board
            for (int y = 64; y < h - 6; y++)
                for (int x = 8; x < w - 8; x++)
                {
                    bool border = (y < 70 || y > h - 12 || x < 14 || x > w - 14);
                    tex.SetPixel(x, y, border ? edge : board);
                }

            // simple star in the middle of the board
            Vector2 c = new Vector2(w / 2f, 96);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float ang = Mathf.Atan2(y - c.y, x - c.x);
                    float r = 16 + Mathf.Cos(ang * 5f) * 7f;
                    if (d < r) tex.SetPixel(x, y, star);
                }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100);
        }

        static Sprite MakeGlow()
        {
            int s = 96;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Vector2 mid = new Vector2(s / 2f, s / 2f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), mid) / (s / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
