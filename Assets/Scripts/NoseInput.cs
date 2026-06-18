using UnityEngine;
using UnityEngine.InputSystem; // new Input System — used for the keyboard fallback
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace WildlifeAdventure
{
    /// <summary>
    /// Turns face/nose tracking from the device camera into a virtual movement
    /// axis used to fly Wira during exploration.
    ///
    /// On a WebGL build it talks to <c>NoseControl.jslib</c>, which runs
    /// MediaPipe FaceMesh in the browser, finds the nose tip and reports its
    /// position (normalised to roughly [-1..1] around the camera centre) back
    /// here via <see cref="OnNosePosition"/>.
    ///
    /// When the camera is unavailable (Editor, desktop, or blocked inside the
    /// Unity Play iframe) it transparently falls back to keyboard / WASD so the
    /// game stays fully playable. The fallback uses the new Input System
    /// (Keyboard.current) so it works no matter how Active Input Handling is set.
    /// </summary>
    public class NoseInput : MonoBehaviour
    {
        public static NoseInput Instance { get; private set; }

        [Header("Tuning")]
        [Tooltip("How far the nose must move from centre before Wira reacts.")]
        public float deadzone = 0.06f;
        [Tooltip("Multiplier applied to the nose offset before clamping.")]
        public float sensitivity = 2.0f;
        [Tooltip("Higher = snappier, lower = smoother follow.")]
        public float smoothing = 10f;

        // Latest *raw* nose offset reported by the tracker, relative to centre.
        float rawX, rawY;
        // Calibration origin (recentred when the player taps "Recenter").
        float originX, originY;
        // Smoothed output axes in [-1..1].
        float smoothX, smoothY;

        public bool CameraReady { get; private set; }
        public bool Tracking { get; private set; }
        public string Status { get; private set; } = "Camera off";

        // Nose control is only ever active in an actual WebGL build with a
        // working webcam. In the Editor or on desktop there is no camera plugin,
        // so this stays false and movement falls through to the keyboard.
#if UNITY_WEBGL && !UNITY_EDITOR
        public bool UsingNose => Tracking && CameraReady;
#else
        public bool UsingNose => false;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void WAStartNoseTracking(string objectName);
        [DllImport("__Internal")] static extern void WAStopNoseTracking();
#endif

        void Awake()
        {
            Instance = this;
        }

        /// <summary>Ask the browser for camera access and begin tracking.</summary>
        public void StartTracking()
        {
            Tracking = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            Status = "Requesting camera...";
            WAStartNoseTracking(gameObject.name);
#else
            // Editor / desktop: pretend the "camera" is ready and use keyboard.
            CameraReady = true;
            Status = "Keyboard mode (no webcam in Editor)";
#endif
        }

        public void StopTracking()
        {
            Tracking = false;
            CameraReady = false;
            Status = "Camera off";
            smoothX = smoothY = rawX = rawY = 0f;
#if UNITY_WEBGL && !UNITY_EDITOR
            WAStopNoseTracking();
#endif
        }

        /// <summary>Treat the player's current head position as the new centre.</summary>
        public void Recenter()
        {
            originX = rawX;
            originY = rawY;
        }

        // ---- Called from JavaScript (NoseControl.jslib) ----

        /// <summary>payload = "x,y" with each value roughly in [-1..1].</summary>
        public void OnNosePosition(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;
            int comma = payload.IndexOf(',');
            if (comma <= 0) return;
            float x, y;
            if (float.TryParse(payload.Substring(0, comma), out x) &&
                float.TryParse(payload.Substring(comma + 1), out y))
            {
                rawX = x;
                rawY = y;
            }
        }

        /// <summary>payload = "ready" | "denied" | "error:..." | text status.</summary>
        public void OnCameraStatus(string payload)
        {
            if (payload == "ready")
            {
                CameraReady = true;
                Status = "Camera on — move your nose to fly";
                Recenter();
            }
            else if (payload == "denied")
            {
                CameraReady = false;
                Status = "Camera blocked — using keyboard";
            }
            else
            {
                Status = payload;
            }
        }

        void Update()
        {
            float targetX, targetY;

            if (UsingNose)
            {
                // Offset from the calibrated centre, scaled and dead-zoned.
                float dx = (rawX - originX) * sensitivity;
                float dy = (rawY - originY) * sensitivity;
                targetX = ApplyDeadzone(dx);
                targetY = ApplyDeadzone(dy);
            }
            else
            {
                // Keyboard fallback (new Input System). Works in the Editor and
                // on Unity Play, where the camera is blocked by the iframe.
                ReadKeyboard(out targetX, out targetY);
            }

            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            smoothX = Mathf.Lerp(smoothX, Mathf.Clamp(targetX, -1f, 1f), k);
            smoothY = Mathf.Lerp(smoothY, Mathf.Clamp(targetY, -1f, 1f), k);
        }

        /// <summary>WASD + arrow keys via the new Input System.</summary>
        void ReadKeyboard(out float h, out float v)
        {
            h = 0f;
            v = 0f;

            var kb = Keyboard.current;
            if (kb == null) return; // no keyboard attached this frame

            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
        }

        float ApplyDeadzone(float val)
        {
            if (Mathf.Abs(val) < deadzone) return 0f;
            float sign = Mathf.Sign(val);
            return sign * (Mathf.Abs(val) - deadzone) / (1f - deadzone);
        }

        public float Horizontal => smoothX;
        public float Vertical   => smoothY;
    }
}
