using System;
using UnityEngine;

namespace MoayadAR.Capture
{
    /// <summary>MediaStore insert (scoped-storage safe; no broad file permissions). DEVICE-PENDING.</summary>
    public sealed class MediaStoreBridge
    {
        public string SaveImage(byte[] jpg, string displayName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                using var values = new AndroidJavaObject("android.content.ContentValues");
                values.Call("put", "_display_name", displayName);
                values.Call("put", "mime_type", "image/jpeg");
                values.Call("put", "relative_path", "Pictures/MoayadAR");
                using var imagesUri = new AndroidJavaClass("android.provider.MediaStore$Images$Media")
                    .CallStatic<AndroidJavaObject>("getContentUri", "external_primary");
                using var item = resolver.Call<AndroidJavaObject>("insert", imagesUri, values);
                if (item == null) return null;
                using var stream = resolver.Call<AndroidJavaObject>("openOutputStream", item);
                stream.Call("write", jpg);
                stream.Call("close");
                return item.Call<string>("toString");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MoayadAR] MediaStore save failed: {e.Message}");
                return null;
            }
#else
            return "editor-simulated://photo/" + displayName; // Editor path is labeled simulation everywhere
#endif
        }
    }

    /// <summary>
    /// MediaCodec/MediaRecorder-backed recorder. Thin managed shell around the native recorder
    /// implemented in the PlatformAndroid AAR (MoayadVideoRecorder.java) so encoder/mic/surface
    /// lifecycle is owned and released in one place. DEVICE-PENDING.
    /// </summary>
    public sealed class AndroidVideoRecorder : IDisposable
    {
        private AndroidJavaObject _native;

        public bool Start(int width, int height, int fps, bool withAudio)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _native = new AndroidJavaObject("com.moayad.ar.capture.MoayadVideoRecorder");
                return _native.Call<bool>("start", width, height, fps, withAudio);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MoayadAR] recorder start failed: {e.Message}");
                return false;
            }
#else
            return true; // editor simulation, flagged in diagnostics
#endif
        }

        public string StopAndSave(MediaStoreBridge mediaStore)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { return _native?.Call<string>("stopAndSave"); }
            catch (Exception e) { Debug.LogWarning($"[MoayadAR] recorder stop failed: {e.Message}"); return null; }
#else
            return "editor-simulated://video";
#endif
        }

        public void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { _native?.Call("release"); } catch { /* release must never throw */ }
            _native?.Dispose();
#endif
            _native = null;
        }
    }
}
