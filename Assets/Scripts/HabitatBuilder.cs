using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// The habitat-exploration module. Builds the Belum-Temenggor rainforest
    /// level entirely from code: sky, ground, layered trees for depth, the
    /// wildlife to discover, pollution to clean, and the quiz totem at the end.
    /// </summary>
    public class HabitatBuilder : MonoBehaviour
    {
        public const string HabitatName = "Belum-Temenggor Forest";

        public float worldMinX = -3f;
        public float worldMaxX = 46f;
        public float groundY = -2.6f;   // top surface of ground
        public float topAnchorY = 3.9f; // ceiling that top-hanging trees grow down from

        GameObject root;
        readonly List<WildlifeEntity> wildlife = new List<WildlifeEntity>();
        readonly List<float> trunkXs = new List<float>();   // x of each deadly trunk
        bool built;

        PlayerController player;
        CameraFollow cam;
        Camera mainCam;

        public void Configure(PlayerController p, CameraFollow c, Camera cameraObj)
        {
            player = p; cam = c; mainCam = cameraObj;
        }

        public void SetWorldVisible(bool v)
        {
            if (root != null) root.SetActive(v);
            if (player != null) player.gameObject.SetActive(v);
        }

        public void Build()
        {
            if (built && root != null) Destroy(root);
            InteractableRegistry.Clear();
            ObstacleRegistry.Clear();
            wildlife.Clear();
            trunkXs.Clear();

            root = new GameObject("Habitat");
            built = true;

            if (mainCam != null) mainCam.backgroundColor = UIFactory.Sky;

            BuildGround();
            BuildBackgroundTrees();
            BuildForegroundTrees();   // solid/deadly trunks first, so collectibles can avoid them
            BuildWildlife();
            BuildPollution();
            BuildQuizTotem();

            // Bounds
            if (player != null) player.SetBounds(worldMinX + 1f, worldMaxX - 1f, groundY + 0.2f, 3.6f);
            if (cam != null) cam.SetBounds(worldMinX + 7f, worldMaxX - 7f);
        }

        // ---------- pieces ----------

        void BuildGround()
        {
            // Main soil band
            var soil = SolidQuad("Ground", UIFactory.Hex("5D7C3F"), 8);
            float width = worldMaxX - worldMinX + 8f;
            soil.transform.localScale = new Vector3(width, 4f, 1f);
            soil.transform.position = new Vector3((worldMinX + worldMaxX) / 2f, groundY - 2f, 0f);

            // Grass line on top
            var grass = SolidQuad("Grass", UIFactory.Hex("7CB342"), 9);
            grass.transform.localScale = new Vector3(width, 0.35f, 1f);
            grass.transform.position = new Vector3((worldMinX + worldMaxX) / 2f, groundY, 0f);

            // Distant hill band for depth
            var hill = SolidQuad("Hills", UIFactory.Hex("A5D6A7"), 1);
            hill.transform.localScale = new Vector3(width, 3f, 1f);
            hill.transform.position = new Vector3((worldMinX + worldMaxX) / 2f, groundY + 1.2f, 0f);
            var hsr = hill.GetComponent<SpriteRenderer>();
            hsr.color = new Color(hsr.color.r, hsr.color.g, hsr.color.b, 0.55f);
        }

        void BuildBackgroundTrees()
        {
            float[] xs = { -1f, 3f, 6.5f, 11f, 15f, 19f, 24f, 28f, 33f, 38f, 42f };
            int i = 0;
            foreach (float x in xs)
            {
                float scale = 1.0f + ((i * 37) % 5) * 0.12f;
                var t = TreeAt(x + ((i % 2) * 0.6f), scale, 3);
                var sr = t.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.82f, 0.88f, 0.82f, 1f); // hazy, further back
                i++;
            }
        }

        void BuildForegroundTrees()
        {
            // These foreground trees are SOLID and DEADLY: Wira cannot fly
            // through a trunk. They are spaced to leave clear gaps to weave
            // through, and the first one is kept well clear of the spawn (x=0)
            // so the player never dies instantly. The deadly trunk is a narrow
            // column (much slimmer than the leafy canopy) so the level stays fair.
            // The foreground trees are SOLID and DEADLY. They now ALTERNATE
            // between growing UP from the ground and hanging DOWN from the top,
            // so Wira has to weave up and down through the gaps instead of just
            // skimming along the bottom. The first one starts as a top-hanging
            // tree with a low gap, leaving the spawn lane clear for a gentle opening.
            float[] xs = { 5f, 9f, 13f, 17f, 21f, 25f, 29f, 33f, 37f };
            float trunkWidth = 0.7f;

            // World Y of the very top of the camera view, so top-hanging trees can
            // actually reach the top edge instead of floating in mid-air.
            float screenTopY = (cam != null && mainCam != null)
                ? cam.fixedY + mainCam.orthographicSize
                : 5.5f;
            float topEdge = screenTopY + 1f;     // block right up past the ceiling

            int i = 0;
            foreach (float x in xs)
            {
                // Safety: never put a deadly trunk near the player's spawn x.
                if (Mathf.Abs(x - 0f) < 3f) { i++; continue; }

                bool fromTop = (i % 2 == 0);     // top, bottom, top, bottom, ...
                float scale = 1.4f + (i % 3) * 0.1f;

                var t = TreeAt(x, scale, 15, fromTop);
                var sr = t.GetComponent<SpriteRenderer>();
                // Full opacity + a subtle warm danger tint so solid trees read
                // differently from the hazy, harmless background trees.
                sr.color = new Color(1f, 0.93f, 0.86f, 1f);

                var obs = t.AddComponent<TreeObstacle>();
                float yMin, yMax;
                float vary = ((i % 3) - 1) * 0.15f;   // small per-tree variation

                if (fromTop)
                {
                    // The hanging tree must reach the TOP EDGE of the screen (so it
                    // doesn't look like it's floating) while its canopy still hangs
                    // low enough to leave a clear gap to fly UNDER. We scale it to
                    // span from the gap target up to the screen top, then position
                    // it so the trunk sits flush against the top edge.
                    float bottomTarget = 0.7f + vary;
                    float desiredHeight = screenTopY - bottomTarget;
                    float h0 = sr.bounds.size.y;
                    if (h0 > 0.01f)
                    {
                        float k = desiredHeight / h0;
                        var ls = t.transform.localScale;
                        t.transform.localScale = new Vector3(ls.x * k, ls.y * k, 1f);
                    }
                    // Push the trunk flush to (just past) the top edge.
                    t.transform.position += new Vector3(0f, (screenTopY + 0.3f) - sr.bounds.max.y, 0f);
                    yMin = sr.bounds.min.y;   // deadly down to the visible canopy
                    yMax = topEdge;
                }
                else
                {
                    // Grow up from the ground: scale so the base sits on the ground
                    // and the deadly top edge reaches the gap target, leaving a
                    // clear gap ABOVE to fly over.
                    float topTarget = 1.1f + vary;
                    float desiredHeight = topTarget - groundY;
                    float h0 = sr.bounds.size.y;
                    if (h0 > 0.01f)
                    {
                        float k = desiredHeight / h0;
                        var ls = t.transform.localScale;
                        t.transform.localScale = new Vector3(ls.x * k, ls.y * k, 1f);
                    }
                    // Drop the base flush onto the ground.
                    t.transform.position += new Vector3(0f, groundY - sr.bounds.min.y, 0f);
                    yMin = groundY;
                    yMax = sr.bounds.max.y;   // deadly up to the visible top
                }
                obs.Configure(trunkWidth, yMin, yMax);
                trunkXs.Add(x);
                i++;
            }
        }

        GameObject TreeAt(float x, float scale, int order, bool fromTop = false)
        {
            var go = new GameObject("Tree");
            go.transform.SetParent(root.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/tree");
            sr.sortingOrder = order;
            if (fromTop)
            {
                // Flip vertically so the tree hangs down from the top of the area.
                go.transform.localScale = new Vector3(scale, -scale, 1f);
                go.transform.position = new Vector3(x, topAnchorY - scale * 0.75f, 0f);
            }
            else
            {
                go.transform.localScale = Vector3.one * scale;
                // place so trunk base sits roughly on the ground
                go.transform.position = new Vector3(x, groundY + scale * 0.75f, 0f);
            }
            return go;
        }

        void BuildWildlife()
        {
            // Per-species display scale (bigger animals stay bigger).
            var scaleById = new Dictionary<string, float>
            {
                { "malayan_tapir",  0.9f },
                { "asian_elephant", 1.7f },
                { "malayan_tiger",  0.9f },
                { "sumatran_rhino", 0.6f },
            };

            // Take the active species list and shuffle the order each play.
            var species = new List<WildlifeData>(WildlifeDatabase.Species);
            for (int i = species.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = species[i]; species[i] = species[j]; species[j] = tmp;
            }

            int n = species.Count;
            if (n == 0) return;
            float leftX = worldMinX + 6f;
            float rightX = worldMaxX - 5f;
            float slot = (rightX - leftX) / n;

            // Hidden animals shrink their reveal radius on harder difficulties.
            float revealRadius = DifficultyInfo.RevealRadius(GameManager.Instance.CurrentDifficulty);

            for (int i = 0; i < n; i++)
            {
                var data = species[i];
                float baseX = leftX + slot * (i + 0.5f);
                float jitter = Random.Range(-slot * 0.30f, slot * 0.30f);
                float x = SafeX(baseX + jitter, worldMinX + 3f, worldMaxX - 4f);

                float scale;
                if (!scaleById.TryGetValue(data.id, out scale)) scale = 0.9f;

                // Spread across the FULL vertical range and away from the straight
                // y=0 lane, so the player cannot just fly in a line and spot them.
                float y = OffLaneY(0.6f, 3.1f);

                var go = new GameObject("Wildlife_" + data.id);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(x, y, 0f);
                var e = go.AddComponent<WildlifeEntity>();
                e.Init(data, scale);
                e.Conceal(revealRadius);   // hidden in foliage until Wira is close
                wildlife.Add(e);
            }
        }

        void BuildPollution()
        {
            // Pollution type table (sprite + friendly label + scale).
            var types = new (string sprite, string label, float scale)[]
            {
                ("plastic",        "plastic bag",    0.55f),
                ("plastic_bottle", "plastic bottle", 0.5f),
                ("glass_bottle",   "glass bottle",   0.5f),
            };

            float revealRadius = DifficultyInfo.RevealRadius(GameManager.Instance.CurrentDifficulty);

            // Random count and random positions/types each play, hidden in foliage
            // across the whole vertical space and off the straight lane.
            int count = Random.Range(5, 8);   // 5, 6 or 7 pieces of litter
            for (int i = 0; i < count; i++)
            {
                var t = types[Random.Range(0, types.Length)];
                float x = SafeX(Random.Range(worldMinX + 3f, worldMaxX - 4f), worldMinX + 3f, worldMaxX - 4f);
                float y = OffLaneY(0.4f, 2.9f);
                var go = new GameObject("Pollution");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(x, y, 0f);
                var p = go.AddComponent<PollutionItem>();
                p.Init(t.sprite, t.label, t.scale);
                p.Conceal(revealRadius);
            }
        }

        // ---------- placement helpers ----------

        /// <summary>True if x is within margin of any deadly trunk column.</summary>
        bool NearTrunk(float x, float margin)
        {
            for (int i = 0; i < trunkXs.Count; i++)
                if (Mathf.Abs(x - trunkXs[i]) < margin) return true;
            return false;
        }

        /// <summary>Nudge a desired x off any deadly trunk so collectibles aren't unreachable.</summary>
        float SafeX(float desired, float min, float max)
        {
            float x = Mathf.Clamp(desired, min, max);
            for (int attempt = 0; attempt < 16 && NearTrunk(x, 1.1f); attempt++)
                x = Mathf.Clamp(Random.Range(min, max), min, max);
            // Final fallback: shift sideways out of the trunk band.
            if (NearTrunk(x, 1.1f)) x = Mathf.Clamp(x + 1.4f, min, max);
            return x;
        }

        /// <summary>
        /// Pick a Y across the vertical range but biased AWAY from the y=0 lane
        /// the player naturally flies along, so collectibles aren't found by
        /// simply travelling in a straight line.
        /// </summary>
        float OffLaneY(float minOffset, float maxY)
        {
            float lo = groundY + minOffset;          // just above ground
            float hi = Mathf.Min(maxY, 3.4f);        // within reachable bounds
            float y = Random.Range(lo, hi);
            // If it landed right on the straight lane (~y 0), push it up or down.
            if (Mathf.Abs(y) < 0.7f)
                y += (Random.value < 0.5f ? -1.1f : 1.1f);
            return Mathf.Clamp(y, lo, hi);
        }

        void BuildQuizTotem()
        {
            var go = new GameObject("QuizTotem");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(worldMaxX - 2f, groundY, 0f);
            var t = go.AddComponent<QuizTotem>();
            t.Init(1.2f);
        }

        public void RefreshDiscoveredState()
        {
            var gm = GameManager.Instance;
            foreach (var w in wildlife)
                if (w != null && w.data != null)
                    w.SetDiscovered(gm.Discovered.Contains(w.data.id));
        }

        // ---------- helpers ----------

        static Sprite _solid;
        static Sprite SolidSprite()
        {
            if (_solid != null) return _solid;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            _solid = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            return _solid;
        }

        GameObject SolidQuad(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SolidSprite();
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }
    }
}