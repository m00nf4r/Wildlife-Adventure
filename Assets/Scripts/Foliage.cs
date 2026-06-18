using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// Procedural leafy "bush" sprite used to conceal wildlife and litter so
    /// they are not visible just by flying past in a straight line — the player
    /// has to explore up, down and around to uncover them.
    /// </summary>
    public static class Foliage
    {
        static Sprite _bush;

        public static Sprite BushSprite()
        {
            if (_bush != null) return _bush;

            int s = 96;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    tex.SetPixel(x, y, clear);

            // A clump of overlapping leafy blobs in two greens.
            Color leafA = UIFactory.Hex("388E3C");
            Color leafB = UIFactory.Hex("2E7D32");
            Vector2[] centres =
            {
                new Vector2(34, 36), new Vector2(62, 38), new Vector2(48, 52),
                new Vector2(24, 50), new Vector2(72, 52), new Vector2(48, 30)
            };
            float[] radii = { 20, 20, 22, 16, 16, 18 };

            for (int i = 0; i < centres.Length; i++)
            {
                Color col = (i % 2 == 0) ? leafA : leafB;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), centres[i]);
                        if (d < radii[i])
                        {
                            float edge = Mathf.Clamp01((radii[i] - d) / 4f);
                            var existing = tex.GetPixel(x, y);
                            var blended = Color.Lerp(existing, col, edge);
                            blended.a = Mathf.Max(existing.a, edge);
                            tex.SetPixel(x, y, blended);
                        }
                    }
            }

            tex.Apply();
            _bush = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.25f), 100);
            return _bush;
        }
    }
}
