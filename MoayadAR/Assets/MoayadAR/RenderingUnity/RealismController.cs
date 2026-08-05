using MoayadAR.OnDeviceAI;
using MoayadAR.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;

namespace MoayadAR.RenderingUnity
{
    /// <summary>
    /// Applies QualityBudget to the live URP asset + AR managers. Thermal state degrades the level
    /// through the honest ladder in RealismPresets. Ultra is only reachable when
    /// RealismPresets.IsUltraAvailable() says so. DEVICE-PENDING.
    /// </summary>
    public sealed class RealismController : MonoBehaviour
    {
        [SerializeField] private UniversalRenderPipelineAsset _urpAsset;
        [SerializeField] private AROcclusionManager _occlusionManager;
        [SerializeField] private ARCameraManager _cameraManager;
        [SerializeField] private bool _deviceSupportsUltraTier; // set by DeviceCapabilityProbe at runtime

        public RealismLevel CurrentLevel { get; private set; } = RealismLevel.Balanced;
        public ThermalState CurrentThermal { get; private set; } = ThermalState.Nominal;

        /// <summary>Returns false (with reason surfaced by UI) when Ultra is requested but unavailable.</summary>
        public bool TrySetLevel(RealismLevel requested)
        {
            if (requested == RealismLevel.Ultra &&
                !RealismPresets.IsUltraAvailable(_deviceSupportsUltraTier, CurrentThermal))
                return false;
            Apply(RealismPresets.DegradeForThermal(requested, CurrentThermal));
            return true;
        }

        public void OnThermalStateChanged(ThermalState state)
        {
            CurrentThermal = state;
            // Degrade if needed; never upgrade silently.
            Apply(RealismPresets.DegradeForThermal(CurrentLevel, state));
        }

        private void Apply(RealismLevel level)
        {
            CurrentLevel = level;
            QualityBudget b = RealismPresets.For(level);
            Application.targetFrameRate = b.TargetFrameRate;

            if (_urpAsset != null)
            {
                _urpAsset.renderScale = b.RenderScale;
                _urpAsset.shadowDistance = level == RealismLevel.Battery ? 15f : 40f;
                _urpAsset.softShadowsSupported = b.SoftShadows;
            }

            if (_occlusionManager != null)
            {
                // Depth occlusion is the backbone; person segmentation only refines it (never replaces it).
                _occlusionManager.requestedEnvironmentDepthMode =
                    level == RealismLevel.Battery ? EnvironmentDepthMode.Disabled : EnvironmentDepthMode.Best;
                _occlusionManager.requestedOcclusionPreferenceMode =
                    b.PersonSegmentationRefinement ? OcclusionPreferenceMode.PreferHumanOcclusion
                                                   : OcclusionPreferenceMode.PreferEnvironmentOcclusion;
            }

            if (_cameraManager != null)
            {
                _cameraManager.requestedLightEstimation = level == RealismLevel.Battery
                    ? LightEstimation.AmbientIntensity | LightEstimation.AmbientColor
                    : LightEstimation.AmbientIntensity | LightEstimation.AmbientColor
                      | LightEstimation.MainLightDirection | LightEstimation.MainLightIntensityLumens
                      | LightEstimation.AmbientSphericalHarmonics | LightEstimation.EnvironmentReflections;
            }
        }
    }
}
