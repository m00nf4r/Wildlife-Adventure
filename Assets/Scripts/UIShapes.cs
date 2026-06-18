using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// Generates simple white UI sprites at runtime (rounded rectangles, circles
    /// and a generic person/avatar icon) so the code-built UI can have rounded
    /// buttons and pills without importing any art. Sprites are white and meant
    /// to be tinted via <c>Image.color</c>. Everything is cached.
    /// </summary>
    public static class UIShapes
    {
        static readonly Dictionary<int, Sprite> roundedCache = new Dictionary<int, Sprite>();
        static Sprite circleCache;
        static Sprite avatarCache;

        /// <summary>
        /// A white rounded-rectangle sprite with a 9-slice border equal to the
        /// corner radius, so it can be stretched to any button size with crisp
        /// corners. Assign to an Image and set <c>type = Sliced</c>.
        /// </summary>
        public static Sprite RoundedRect(int radius)
        {
            if (radius < 1) radius = 1;
            if (roundedCache.TryGetValue(radius, out var cached) && cached != null)
                return cached;

            int r = radius;
            int s = r * 2 + 4;                 // a few middle pixels to stretch
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var clear = new Color(1f, 1f, 1f, 0f);
            var solid = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Corner circle centres.
                    float cx = Mathf.Clamp(x, r, s - 1 - r);
                    float cy = Mathf.Clamp(y, r, s - 1 - r);
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    // Soft 1px edge for anti-aliasing.
                    float a = Mathf.Clamp01(r - dist + 0.5f);
                    tex.SetPixel(x, y, a >= 1f ? solid : new Color(1f, 1f, 1f, a));
                    if (a <= 0f) tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            roundedCache[radius] = sprite;
            return sprite;
        }

        /// <summary>A plain white filled circle (tint via Image.color).</summary>
        public static Sprite Circle()
        {
            if (circleCache != null) return circleCache;
            int d = 128;
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float c = (d - 1) / 2f, rad = d / 2f - 1f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(rad - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            circleCache = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 100f);
            return circleCache;
        }

        /// <summary>
        /// A generic "person inside a ring" account icon, drawn opaque (tint via
        /// Image.color). Used for the Login / Sign-up control on the menu.
        /// </summary>
        public static Sprite Avatar()
        {
            if (avatarCache != null) return avatarCache;
            int d = 128;
            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float c = (d - 1) / 2f;
            float outer = 60f, inner = 52f;     // ring radius / thickness
            float headCx = c, headCy = 86f, headR = 17f;

            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float distC = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    bool inCircle = distC <= inner - 1f;

                    // Ring (outline).
                    float ring = Mathf.Min(Mathf.Clamp01(outer - distC + 0.5f),
                                           Mathf.Clamp01(distC - inner + 0.5f));

                    // Head.
                    float headDist = Mathf.Sqrt((x - headCx) * (x - headCx) + (y - headCy) * (y - headCy));
                    float head = Mathf.Clamp01(headR - headDist + 0.5f);

                    // Shoulders: a dome that is clipped to inside the ring.
                    float sx = (x - c) / 34f;
                    float sy = (y - 30f) / 30f;
                    bool domeShape = (sx * sx + sy * sy) <= 1f && y <= 56f;
                    float shoulders = (domeShape && inCircle) ? 1f : 0f;

                    float a = Mathf.Max(ring, Mathf.Max(head, shoulders));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            avatarCache = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 100f);
            return avatarCache;
        }
    }
}
