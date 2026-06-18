// =============================================================================
//  NoseControl.jslib  —  WebGL nose-tracking plugin for Wildlife Adventure
//
//  Runs MediaPipe FaceMesh in the browser, finds the nose tip from the device
//  camera and reports its position (normalised to ~[-1..1] around centre) to
//  the Unity "NoseInput" GameObject.
//
//  Unity -> JS:   WAStartNoseTracking(objectName) / WAStopNoseTracking()
//  JS -> Unity:   SendMessage(objectName, "OnNosePosition", "x,y")
//                 SendMessage(objectName, "OnCameraStatus", "ready|denied|...")
//
//  CHANGES vs. the original:
//   * The camera preview <video> now starts HIDDEN and is only shown once the
//     camera actually streams. If the camera is blocked/denied (e.g. inside the
//     Unity Play iframe) or anything fails, the preview is removed instead of
//     being left on-screen as a black box.
//
//  NOTE: in your WebGL template's index.html, after createUnityInstance()
//  resolves, add:   window.unityInstance = unityInstance;
//  (The bundled docs/index.html has already been patched to do this.)
// =============================================================================
mergeInto(LibraryManager.library, {

  WAStartNoseTracking: function (objNamePtr) {
    var objName = UTF8ToString(objNamePtr);
    var WA = window.__WA_Nose = window.__WA_Nose || {};
    WA.obj = objName;
    WA.running = true;

    // Safe bridge back into Unity, trying every common instance handle.
    WA.send = function (method, value) {
      try {
        if (window.unityInstance && window.unityInstance.SendMessage) {
          window.unityInstance.SendMessage(WA.obj, method, value); return;
        }
        if (typeof unityInstance !== "undefined" && unityInstance.SendMessage) {
          unityInstance.SendMessage(WA.obj, method, value); return;
        }
        if (typeof gameInstance !== "undefined" && gameInstance.SendMessage) {
          gameInstance.SendMessage(WA.obj, method, value); return;
        }
        if (typeof SendMessage === "function") { SendMessage(WA.obj, method, value); return; }
      } catch (e) { console.warn("[NoseControl] send failed:", e); }
    };

    function loadScript(src) {
      return new Promise(function (resolve, reject) {
        if (document.querySelector('script[src="' + src + '"]')) { resolve(); return; }
        var s = document.createElement("script");
        s.src = src; s.crossOrigin = "anonymous";
        s.onload = resolve; s.onerror = function () { reject(new Error("load " + src)); };
        document.head.appendChild(s);
      });
    }

    function makePreview() {
      // Small mirrored camera preview so the player can see themselves.
      // Starts hidden (display:none) so a blocked/denied camera never shows
      // up as a black rectangle — we only reveal it once the stream is live.
      var video = document.createElement("video");
      video.setAttribute("playsinline", "");
      video.muted = true;
      video.style.cssText =
        "position:fixed;right:12px;bottom:12px;width:160px;height:120px;" +
        "object-fit:cover;transform:scaleX(-1);border:3px solid #2E7D32;" +
        "border-radius:10px;z-index:9999;background:#000;box-shadow:0 4px 16px rgba(0,0,0,.4);" +
        "display:none;";
      video.id = "wa-nose-preview";
      document.body.appendChild(video);
      WA.video = video;
      return video;
    }

    function showPreview() {
      if (WA.video) WA.video.style.display = "block";
    }

    // Stops the camera tracks and removes the preview element entirely.
    // Stored on WA so WAStopNoseTracking() can reuse it.
    function removePreview() {
      try {
        if (WA.video) {
          if (WA.video.srcObject) {
            WA.video.srcObject.getTracks().forEach(function (t) { t.stop(); });
          }
          if (WA.video.parentNode) WA.video.parentNode.removeChild(WA.video);
          WA.video = null;
        }
      } catch (e) {}
    }
    WA.removePreview = removePreview;

    function start() {
      var video = makePreview();

      var FaceMeshCtor = window.FaceMesh;
      var CameraCtor = window.Camera;
      if (!FaceMeshCtor || !CameraCtor) {
        removePreview();
        WA.send("OnCameraStatus", "error:libs");
        return;
      }

      var faceMesh = new FaceMeshCtor({
        locateFile: function (f) {
          return "https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh/" + f;
        }
      });
      faceMesh.setOptions({
        maxNumFaces: 1, refineLandmarks: false,
        minDetectionConfidence: 0.5, minTrackingConfidence: 0.5
      });

      faceMesh.onResults(function (results) {
        if (!WA.running) return;
        if (results.multiFaceLandmarks && results.multiFaceLandmarks.length > 0) {
          var lm = results.multiFaceLandmarks[0];
          var nose = lm[1] || lm[4];   // FaceMesh nose-tip landmark
          if (nose) {
            // Image space is 0..1, origin top-left. Mirror X (selfie view),
            // invert Y so raising the head moves Wira up.
            var nx = (0.5 - nose.x) * 2.0;
            var ny = (0.5 - nose.y) * 2.0;
            WA.send("OnNosePosition", nx.toFixed(4) + "," + ny.toFixed(4));
          }
        }
      });
      WA.faceMesh = faceMesh;

      var camera = new CameraCtor(video, {
        onFrame: function () {
          if (WA.running && WA.faceMesh) return WA.faceMesh.send({ image: video });
          return Promise.resolve();
        },
        width: 320, height: 240
      });
      WA.camera = camera;

      camera.start()
        .then(function () {
          showPreview();                       // reveal only now that it's live
          WA.send("OnCameraStatus", "ready");
        })
        .catch(function (err) {
          console.warn("[NoseControl] camera error:", err);
          removePreview();                     // no black box if denied/blocked
          WA.send("OnCameraStatus",
            (err && err.name === "NotAllowedError") ? "denied" : "error:camera");
        });
    }

    WA.send("OnCameraStatus", "Loading face tracking...");
    loadScript("https://cdn.jsdelivr.net/npm/@mediapipe/camera_utils/camera_utils.js")
      .then(function () {
        return loadScript("https://cdn.jsdelivr.net/npm/@mediapipe/face_mesh/face_mesh.js");
      })
      .then(start)
      .catch(function (e) {
        console.warn("[NoseControl] init failed:", e);
        removePreview();
        WA.send("OnCameraStatus", "error:load");
      });
  },

  WAStopNoseTracking: function () {
    var WA = window.__WA_Nose;
    if (!WA) return;
    WA.running = false;
    try { if (WA.camera && WA.camera.stop) WA.camera.stop(); } catch (e) {}
    try { if (WA.removePreview) WA.removePreview(); } catch (e) {}
  }

});
