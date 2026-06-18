using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// An environmental threat (plastic bag, plastic bottle, glass bottle) that
    /// the player cleans up for Conservation Points, reinforcing the
    /// conservation message of the game.
    /// </summary>
    public class PollutionItem : MonoBehaviour, IInteractable
    {
        public int points = 50;
        public string label = "litter";
        SpriteRenderer sr;
        SpriteRenderer cover;
        bool cleaned;
        float bob, baseY, baseRot;

        // Concealment: litter is tucked out of sight until Wira gets near.
        bool concealed;
        bool revealed = true;
        float revealRadius = 2.4f;
        float reveal01;
        float rustle;

        public Vector3 WorldPosition => transform.position;
        public string Prompt => revealed ? "Press E to clean up " + label : "";
        public bool Available => !cleaned && revealed;

        public void Init(string spriteName, string label, float scale)
        {
            this.label = label;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/" + spriteName);
            sr.sortingOrder = 9;
            transform.localScale = Vector3.one * scale;
            baseY = transform.position.y;
            baseRot = Random.Range(-12f, 12f);
            transform.rotation = Quaternion.Euler(0, 0, baseRot);
        }

        /// <summary>Hide this litter behind foliage until Wira gets close.</summary>
        public void Conceal(float radius)
        {
            concealed = true;
            revealed = false;
            revealRadius = radius;
            reveal01 = 0f;

            var cg = new GameObject("Cover");
            cg.transform.SetParent(transform, false);
            cg.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            cg.transform.localScale = Vector3.one * 1.6f;
            cover = cg.AddComponent<SpriteRenderer>();
            cover.sprite = Foliage.BushSprite();
            cover.sortingOrder = 13;

            sr.color = new Color(1f, 1f, 1f, 0f);
        }

        void OnEnable()  { InteractableRegistry.Register(this); }
        void OnDisable() { InteractableRegistry.Unregister(this); }

        void Update()
        {
            bob += Time.deltaTime * 1.4f;
            var p = transform.position;
            p.y = baseY + Mathf.Sin(bob + baseRot) * 0.04f;
            transform.position = p;

            if (concealed && !cleaned) UpdateConcealment();
        }

        void UpdateConcealment()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.player == null) return;

            float dist = Vector2.Distance(transform.position, gm.player.transform.position);
            float hintRadius = revealRadius * 1.7f;

            float target;
            if (dist <= revealRadius) { target = 1f; revealed = true; }
            else if (dist <= hintRadius) target = 0.18f;
            else target = 0f;

            reveal01 = Mathf.Lerp(reveal01, target, 6f * Time.deltaTime);

            rustle += Time.deltaTime * 9f;
            float wobble = (target > 0f && target < 1f) ? Mathf.Sin(rustle) * 0.07f : 0f;
            if (cover != null)
                cover.transform.localScale = Vector3.one * (1.6f + wobble);

            if (sr != null) sr.color = new Color(1f, 1f, 1f, reveal01);
            if (cover != null) cover.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.12f, reveal01));
        }

        public void Interact()
        {
            if (cleaned) return;
            cleaned = true;
            InteractableRegistry.Unregister(this);
            GameManager.Instance.CleanPollution(points);
            StartCoroutine(PopAndDestroy());
        }

        System.Collections.IEnumerator PopAndDestroy()
        {
            float t = 0f;
            Vector3 s0 = transform.localScale;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float k = 1f - (t / 0.18f);
                transform.localScale = s0 * k;
                if (sr != null) sr.color = new Color(1f, 1f, 1f, k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
