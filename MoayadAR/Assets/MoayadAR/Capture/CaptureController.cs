using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MoayadAR.Capture
{
    /// <summary>
    /// Photo/video capture of the composited AR view (master prompt §13).
    /// Photos: on-demand render of the AR camera stack to a RenderTexture (occlusion/lighting included),
    /// saved via MediaStore. Video: 1080p30 default; 4K only after a device benchmark enables it.
    /// All native resources are released on stop/pause/error — see StopRecording's finally block.
    /// DEVICE-PENDING.
    /// </summary>
    public sealed class CaptureController : MonoBehaviour
    {
        public enum CaptureMode { Photo, Video, LiveAR }
        public enum RecordState { Idle, Recording, Paused, Stopping }

        [SerializeField] private Camera _arCamera;
        [SerializeField] private bool _includeUi;

        public CaptureMode Mode { get; private set; } = CaptureMode.LiveAR;
        public RecordState State { get; private set; } = RecordState.Idle;
        public float RecordingSeconds { get; private set; }
        public bool AudioEnabled { get; set; }
        public event Action<string> CaptureSaved;   // MediaStore content URI
        public event Action<string> CaptureFailed;  // localization key

        private MediaStoreBridge _mediaStore;
        private AndroidVideoRecorder _recorder;
        private float _recordStarted;

        private void Awake() => _mediaStore = new MediaStoreBridge();

        public void SetMode(CaptureMode mode) => Mode = mode;

        public void TakePhoto()
        {
            if (State != RecordState.Idle) return;
            StartCoroutine(TakePhotoRoutine());
        }

        private IEnumerator TakePhotoRoutine()
        {
            yield return new WaitForEndOfFrame();
            int w = Screen.width, h = Screen.height;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            try
            {
                _arCamera.targetTexture = rt;
                _arCamera.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                byte[] jpg = tex.EncodeToJPG(95);
                Destroy(tex);
                string uri = _mediaStore.SaveImage(jpg, $"MoayadAR_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg");
                if (uri != null) CaptureSaved?.Invoke(uri); else CaptureFailed?.Invoke("capture.failed");
            }
            finally
            {
                _arCamera.targetTexture = null;
                RenderTexture.active = prev;
                rt.Release();
                Destroy(rt);
            }
        }

        public bool StartRecording(bool withAudio)
        {
            if (State != RecordState.Idle) return false;
            AudioEnabled = withAudio; // microphone permission requested at this point of use, never earlier
            _recorder = new AndroidVideoRecorder();
            if (!_recorder.Start(1920, 1080, 30, withAudio))
            {
                _recorder.Dispose();
                _recorder = null;
                CaptureFailed?.Invoke("capture.failed");
                return false;
            }
            RecordingSeconds = 0f;
            _recordStarted = Time.time;
            State = RecordState.Recording;
            return true;
        }

        public void StopRecording()
        {
            if (State == RecordState.Idle) return;
            State = RecordState.Stopping;
            try
            {
                string uri = _recorder?.StopAndSave(_mediaStore);
                if (uri != null) CaptureSaved?.Invoke(uri); else CaptureFailed?.Invoke("capture.failed");
            }
            finally
            {
                _recorder?.Dispose(); // encoder, microphone, surface, file handle — always released
                _recorder = null;
                State = RecordState.Idle;
            }
        }

        private void Update()
        {
            if (State == RecordState.Recording) RecordingSeconds = Time.time - _recordStarted;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && State == RecordState.Recording) StopRecording(); // no half-written files
        }

        private void OnDestroy()
        {
            if (State != RecordState.Idle) StopRecording();
        }
    }
}
