using System;
using System.Collections.Generic;
using MoayadAR.OnDeviceAI;
using UnityEngine;

namespace MoayadAR.PlatformAndroid
{
    /// <summary>
    /// Runtime capability probe. Nothing is inferred from the SoC marketing name: NPU delegate
    /// availability, GPU delegate, thermal headroom, and Ultra-tier eligibility are all queried.
    /// Results feed BackendSelector and the Realism Ultra gate. DEVICE-PENDING.
    /// </summary>
    public sealed class DeviceCapabilityProbe : MonoBehaviour
    {
        public DeviceAICapabilities AICapabilities { get; private set; }
        public bool UltraTierEligible { get; private set; }
        public ThermalState Thermal { get; private set; } = ThermalState.Nominal;

        public event Action<ThermalState> ThermalChanged;

        private void Start()
        {
            AICapabilities = ProbeAI();
            UltraTierEligible = ProbeUltraTier();
            StartCoroutine(PollThermal());
        }

        private static DeviceAICapabilities ProbeAI()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // LiteRT QNN accelerator availability is checked by attempting delegate creation
            // in the native AAR (MoayadAICapabilities.java) — a failed probe means CPU/GPU fallback.
            try
            {
                using var probe = new AndroidJavaObject("com.moayad.ar.ai.MoayadAICapabilities");
                bool qnn = probe.Call<bool>("isQnnAcceleratorAvailable");
                bool gpu = probe.Call<bool>("isGpuDelegateAvailable");
                string opsCsv = probe.Call<string>("qnnSupportedOpsCsv") ?? "";
                var ops = new HashSet<string>(opsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                return new DeviceAICapabilities(qnn, gpu, ops);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MoayadAR] AI capability probe failed, CPU fallback: {e.Message}");
                return new DeviceAICapabilities(false, false, new HashSet<string>());
            }
#else
            return new DeviceAICapabilities(false, true, new HashSet<string>()); // editor: assume GPU only
#endif
        }

        private static bool ProbeUltraTier() =>
            SystemInfo.graphicsMemorySize >= 1024 && SystemInfo.systemMemorySize >= 8192;

        private System.Collections.IEnumerator PollThermal()
        {
            var wait = new WaitForSeconds(2f);
            while (true)
            {
                var state = QueryThermal();
                if (state != Thermal)
                {
                    Thermal = state;
                    ThermalChanged?.Invoke(state);
                }
                yield return wait;
            }
        }

        private static ThermalState QueryThermal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var pm = activity.Call<AndroidJavaObject>("getSystemService", "power");
                int status = pm.Call<int>("getCurrentThermalStatus");
                return status switch
                {
                    0 => ThermalState.Nominal, 1 => ThermalState.Fair, 2 => ThermalState.Fair,
                    3 => ThermalState.Fair, 4 => ThermalState.Serious, 5 => ThermalState.Critical,
                    _ => ThermalState.Critical
                };
            }
            catch { return ThermalState.Nominal; }
#else
            return ThermalState.Nominal;
#endif
        }
    }
}
