using System;
using MoayadAR.OnDeviceAI;

namespace MoayadAR.Rendering
{
    public enum RealismLevel { Battery, Balanced, High, Ultra }

    public sealed class QualityBudget
    {
        public int MaxTriangles = 1_500_000;
        public int MaxSkinnedBones = 128;
        public int MaxTextureDimension = 2048;
        public int MaxActiveRenderers = 256;
        public int ShadowResolution = 2048;
        public int ReflectionProbeUpdateFrames = 60;   // probe refresh interval
        public int MlInputResolution = 192;
        public float RenderScale = 1.0f;               // dynamic resolution ceiling
        public int TargetFrameRate = 60;
        public bool SoftShadows = true;
        public bool PersonSegmentationRefinement = true;
    }

    /// <summary>
    /// Realism presets (master prompt §8/§16). Ultra exists ONLY when the device probe says the
    /// capability is present and thermals allow it — otherwise the UI disables it with an
    /// accurate explanation (realism.ultraUnavailable). No fake ray-tracing toggle anywhere.
    /// </summary>
    public static class RealismPresets
    {
        public static QualityBudget For(RealismLevel level) => level switch
        {
            RealismLevel.Battery => new QualityBudget
            {
                MaxTriangles = 500_000, MaxSkinnedBones = 64, MaxTextureDimension = 1024,
                ShadowResolution = 1024, ReflectionProbeUpdateFrames = 240, MlInputResolution = 128,
                RenderScale = 0.8f, TargetFrameRate = 30, SoftShadows = false,
                PersonSegmentationRefinement = false
            },
            RealismLevel.Balanced => new QualityBudget(),
            RealismLevel.High => new QualityBudget
            {
                MaxTriangles = 3_000_000, MaxSkinnedBones = 256, MaxTextureDimension = 4096,
                ShadowResolution = 4096, ReflectionProbeUpdateFrames = 30, MlInputResolution = 256,
                RenderScale = 1.0f, TargetFrameRate = 60
            },
            RealismLevel.Ultra => new QualityBudget
            {
                MaxTriangles = 8_000_000, MaxSkinnedBones = 512, MaxTextureDimension = 8192,
                ShadowResolution = 4096, ReflectionProbeUpdateFrames = 15, MlInputResolution = 384,
                RenderScale = 1.0f, TargetFrameRate = 30 // honest: Ultra targets a stable 30
            },
            _ => new QualityBudget()
        };

        /// <summary>Ultra gate: capability AND thermal headroom required.</summary>
        public static bool IsUltraAvailable(bool deviceSupportsUltraTier, ThermalState thermal) =>
            deviceSupportsUltraTier && thermal <= ThermalState.Fair;

        /// <summary>Thermal degradation ladder: which level to fall back to.</summary>
        public static RealismLevel DegradeForThermal(RealismLevel current, ThermalState thermal) => thermal switch
        {
            ThermalState.Nominal => current,
            ThermalState.Fair => current == RealismLevel.Ultra ? RealismLevel.High : current,
            ThermalState.Serious => current > RealismLevel.Balanced ? RealismLevel.Balanced : current,
            ThermalState.Critical => RealismLevel.Battery,
            _ => current
        };
    }
}
