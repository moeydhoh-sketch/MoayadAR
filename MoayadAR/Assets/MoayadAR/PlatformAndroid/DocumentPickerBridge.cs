using System;
using UnityEngine;

namespace MoayadAR.PlatformAndroid
{
    /// <summary>
    /// Storage Access Framework bridge: ACTION_OPEN_DOCUMENT with persistable URI permission.
    /// No MANAGE_EXTERNAL_STORAGE, no broad file access (master prompt §5.1). DEVICE-PENDING.
    /// The native counterpart (MoayadDocumentPicker.java in the AAR) marshals the result back.
    /// </summary>
    public sealed class DocumentPickerBridge
    {
        public sealed class PickedDocument
        {
            public string Uri;          // content:// URI with persisted read permission
            public string DisplayName;
            public long SizeBytes;
            public string MimeType;
        }

        public event Action<PickedDocument> Picked;
        public event Action Cancelled;

        private const string RequestCode = 0x4D41; // "MA"

        public void OpenModelPicker()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.OPEN_DOCUMENT");
            intent.Call<AndroidJavaObject>("addCategory", "android.intent.category.OPENABLE");
            intent.Call<AndroidJavaObject>("setType", "*/*");
            string[] mimeTypes = {
                "model/gltf-binary", "model/gltf+json", "application/octet-stream", "text/plain"
            };
            intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.MIME_TYPES", mimeTypes);
            // FLAG_GRANT_READ_URI_PERMISSION | FLAG_GRANT_PERSISTABLE_URI_PERMISSION
            intent.Call<AndroidJavaObject>("addFlags", 0x00000001 | 0x00000040);
            activity.Call("startActivityForResult", intent, RequestCode);
#else
            // Editor path: simulated pick, always labeled as simulation in the UI/diagnostics.
            Picked?.Invoke(new PickedDocument
            {
                Uri = "editor-simulated://sample.glb",
                DisplayName = "sample.glb",
                SizeBytes = 0,
                MimeType = "model/gltf-binary"
            });
#endif
        }

        /// <summary>Called from the native activity result handler via UnitySendMessage.</summary>
        public void OnPickResult(string payload)
        {
            if (string.IsNullOrEmpty(payload)) { Cancelled?.Invoke(); return; }
            var parts = payload.Split('|');
            if (parts.Length < 4) { Cancelled?.Invoke(); return; }
            Picked?.Invoke(new PickedDocument
            {
                Uri = parts[0],
                DisplayName = parts[1],
                SizeBytes = long.TryParse(parts[2], out var s) ? s : 0,
                MimeType = parts[3]
            });
        }
    }
}
