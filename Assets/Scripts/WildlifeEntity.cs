using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// A discoverable animal placed in the habitat. Scanning it (pressing E
    /// nearby) opens its Fact Card. Once added to the journal it stays visible
    /// but shows a small "discovered" tick and can't be scanned again.
    /// </summary>
    public class WildlifeEntity : MonoBehaviour, IInteractable
    {
        public WildlifeData data;
        SpriteRenderer sr;
        SpriteRenderer tick;
        SpriteRenderer cover;       // bush hiding the animal until approached
        float bob;
        float baseY;
        bool discovered;

        // Concealment: the animal is hidden until Wira flies close enough.
        bool concealed;
        bool revealed;
        float revealRadius = 2.4f;
        float reveal01;             // 0 hidden .. 1 fully shown (smoothed)
        float rustle;

        public Vector3 WorldPosition => transform.position;
        public string Prompt =>
            discovered ? "" :
            revealed ? "Press J to add " + data.commonName + " to Journal" :
            "";
        // Only scannable once it has been uncovered.
        public bool Available => !discovered && revealed;

        public void Init(WildlifeData d, float scale)
        {
            data = d;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/" + d.spriteName);
            sr.sortingOrder = 10;
            transform.localScale = Vector3.one * scale;
            baseY = transform.position.y;
            revealed = true;   // visible unless Conceal() hides it

            // Small green tick shown after discovery (built from a tiny texture).
            var tg = new GameObject("Tick");
            tg.transform.SetParent(transform, false);
            tg.transform.localPosition = new Vector3(0.0f, 1.1f, 0f);
            tick = tg.AddComponent<SpriteRenderer>();
            tick.sprite = MakeDot(UIFactory.GreenLight);
            tick.sortingOrder = 12;
            tg.transform.localScale = Vector3.one * 0.35f;
            tick.enabled = false;
        }

        /// <summary>Hide this animal behind foliage until Wira gets close.</summary>
        public void Conceal(float radius)
        {
            concealed = true;
            revealed = false;
            revealRadius = radius;
            reveal01 = 0f;

            // Draw a bush in front of the animal (higher sorting order).
            var cg = new GameObject("Cover");
            cg.transform.SetParent(transform, false);
            cg.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            cg.transform.localScale = Vector3.one * 1.35f;
            cover = cg.AddComponent<SpriteRenderer>();
            cover.sprite = Foliage.BushSprite();
            cover.sortingOrder = 13;   // in front of the animal (10) and tick (12)

            ApplyReveal(true);
        }

        void OnEnable()  { InteractableRegistry.Register(this); }
        void OnDisable() { InteractableRegistry.Unregister(this); }

        void Update()
        {
            // Gentle idle motion.
            bob += Time.deltaTime * 1.8f;
            var p = transform.position;
            p.y = baseY + Mathf.Sin(bob) * 0.06f;
            transform.position = p;

            if (concealed && !discovered) UpdateConcealment();
        }

        void UpdateConcealment()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.player == null) return;

            float dist = Vector2.Distance(transform.position, gm.player.transform.position);
            float hintRadius = revealRadius * 1.7f;

            float target;
            if (dist <= revealRadius) { target = 1f; revealed = true; }
            else if (dist <= hintRadius) target = 0.18f;   // a faint hint / rustle
            else target = 0f;

            reveal01 = Mathf.Lerp(reveal01, target, 6f * Time.deltaTime);

            // Bush rustles a little when Wira is in the hint zone.
            rustle += Time.deltaTime * 9f;
            float wobble = (target > 0f && target < 1f) ? Mathf.Sin(rustle) * 0.06f : 0f;
            if (cover != null)
                cover.transform.localScale = Vector3.one * (1.35f + wobble);

            ApplyReveal(false);
        }

        void ApplyReveal(bool instant)
        {
            float a = instant ? reveal01 : reveal01;
            if (sr != null)
                sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, a));
            if (cover != null)
                cover.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.12f, a));
        }

        public void Interact()
        {
            if (discovered) return;
            GameManager.Instance.ShowFactCard(data);
        }

        /// <summary>Called by HabitatBuilder to sync visuals with saved progress.</summary>
        public void SetDiscovered(bool value)
        {
            discovered = value;
            if (value) revealed = true;
            if (tick != null) tick.enabled = value;
            if (cover != null) cover.enabled = !value;     // drop the bush once found
            if (sr != null && value)
                sr.color = new Color(1f, 1f, 1f, 0.92f);
        }

        static Sprite MakeDot(Color c)
        {
            int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 mid = new Vector2(s / 2f, s / 2f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), mid);
                    tex.SetPixel(x, y, d < s / 2f - 1 ? c : new Color(0, 0, 0, 0));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16);
        }
    }
}
