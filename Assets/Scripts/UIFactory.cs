using UnityEngine;
using UnityEngine.UI;

namespace WildlifeAdventure
{
    /// <summary>
    /// Builds Unity UI (uGUI) elements purely from code so the project needs
    /// no hand-wired prefabs. Also holds the nature-inspired colour palette
    /// (earthy greens and deep blues) described in the report.
    /// </summary>
    public static class UIFactory
    {
        // ---- Palette ----
        public static readonly Color Green      = Hex("2E7D32");
        public static readonly Color GreenDark  = Hex("1B5E20");
        public static readonly Color GreenLight = Hex("66BB6A");
        public static readonly Color Blue       = Hex("1565C0");
        public static readonly Color Sky        = Hex("8EC9E8");
        public static readonly Color Cream      = Hex("F4F1E8");
        public static readonly Color Ink        = Hex("1A2421");
        public static readonly Color Amber      = Hex("F9A825");
        public static readonly Color Red        = Hex("C62828");
        public static readonly Color Panel      = Hex("FFFFFF");
        public static readonly Color Shade      = new Color(0f, 0f, 0f, 0.55f);

        static Font _font;
        public static Font GetFont()
        {
            if (_font != null) return _font;
            // Pixel font: put a .ttf named "PixelFont.ttf" into
            // Assets/Resources/Fonts/ and the whole game uses it.
            _font = Resources.Load<Font>("Fonts/PixelFont");
            // Fallbacks if no pixel font has been added yet.
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }

        /// <summary>Creates the root screen-space canvas (1280x720 reference).</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform AddRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        public static Image Panel2(Transform parent, Color color, Vector2 anchorMin,
                                   Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, string name = "Panel")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = AddRect(go);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return img;
        }

        /// <summary>A panel anchored to a fixed pixel size, centred at (x,y) from screen centre.</summary>
        public static Image PanelCentered(Transform parent, Color color, float w, float h,
                                          float x = 0, float y = 0, string name = "Panel")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = AddRect(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
                                 TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = GetFont();
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = AddRect(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 8); rt.offsetMax = new Vector2(-8, -8);
            return t;
        }

        /// <summary>Convenience: a label placed at a fixed size and position.</summary>
        public static Text LabelAt(Transform parent, string text, int size, Color color,
                                   float w, float h, float x, float y,
                                   TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var holder = new GameObject("LabelHolder");
            holder.transform.SetParent(parent, false);
            var rt = AddRect(holder);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return Label(holder.transform, text, size, color, align, style);
        }

        public static Button MakeButton(Transform parent, string text, Color bg, Color textColor,
                                         float w, float h, float x, float y,
                                         System.Action onClick, int fontSize = 26)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var rt = AddRect(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.95f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.85f, 0.8f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            Label(go.transform, text, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return btn;
        }

        public static Image SpriteImage(Transform parent, string spriteName, float w, float h,
                                        float x, float y)
        {
            var go = new GameObject("Sprite_" + spriteName);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("Sprites/" + spriteName);
            img.preserveAspect = true;
            var rt = AddRect(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return img;
        }

        /// <summary>Builds a legacy uGUI InputField (works with keyboard input on WebGL).</summary>
        public static InputField MakeInput(Transform parent, string placeholder, float w, float h,
                                           float x, float y, bool password = false, int fontSize = 22)
        {
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var rt = AddRect(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);

            var input = go.AddComponent<InputField>();
            input.targetGraphic = img;

            var ph = Label(go.transform, placeholder, fontSize, new Color(0.55f, 0.55f, 0.55f));
            ph.alignment = TextAnchor.MiddleLeft;
            ph.fontStyle = FontStyle.Italic;

            var txt = Label(go.transform, "", fontSize, Ink);
            txt.alignment = TextAnchor.MiddleLeft;
            txt.supportRichText = false;

            input.textComponent = txt;
            input.placeholder = ph;
            if (password)
            {
                input.contentType = InputField.ContentType.Password;
                input.inputType = InputField.InputType.Password;
            }
            else
            {
                input.contentType = InputField.ContentType.Standard;
            }
            return input;
        }

        /// <summary>Full-screen dim overlay used behind modal panels.</summary>
        public static Image Dim(Transform parent)
        {
            return Panel2(parent, Shade, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Dim");
        }

        public static Color StatusColor(string status)
        {
            if (status.Contains("Critically")) return Hex("B71C1C");
            if (status.Contains("Endangered")) return Hex("E65100");
            return Hex("F9A825");
        }
    }
}