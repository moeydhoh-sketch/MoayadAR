using System;
using System.Collections.Generic;

namespace MoayadAR.OnDeviceAI
{
    public enum AIBackend { None, NpuQnn, Gpu, Cpu, Mixed }
    public enum AITask { PersonSegmentation, HandLandmarks, SceneClassification, DepthRefinement }
    public enum ThermalState { Nominal, Fair, Serious, Critical }

    /// <summary>What the device actually reported — probed at runtime, never assumed from the SoC name.</summary>
    public readonly struct DeviceAICapabilities
    {
        public readonly bool QnnAvailable;          // LiteRT Qualcomm AI Engine Direct accelerator present
        public readonly bool GpuDelegateAvailable;
        public readonly ISet<string> QnnSupportedOps; // op names fully delegable to NPU
        public DeviceAICapabilities(bool qnn, bool gpu, ISet<string> qnnOps)
        { QnnAvailable = qnn; GpuDelegateAvailable = gpu; QnnSupportedOps = qnnOps ?? new HashSet<string>(); }
    }

    public readonly struct BackendChoice
    {
        public readonly AIBackend Backend;
        public readonly bool PartialDelegation; // some ops fell back → report "mixed", honestly
        public BackendChoice(AIBackend b, bool partial) { Backend = b; PartialDelegation = partial; }
        public string DiagnosticsKey =>
            Backend == AIBackend.NpuQnn ? "diagnostics.backend.npu" :
            Backend == AIBackend.Gpu ? "diagnostics.backend.gpu" :
            Backend == AIBackend.Cpu ? "diagnostics.backend.cpu" : "diagnostics.backend.mixed";
    }

    /// <summary>
    /// NPU → GPU → CPU fallback per model (master prompt §9). A model is only NPU-eligible when
    /// every op it needs is in the QNN-supported set; partial graphs are reported as mixed.
    /// The active backend is always exposed to Diagnostics — never claimed, always shown.
    /// </summary>
    public static class BackendSelector
    {
        public static BackendChoice Select(DeviceAICapabilities caps, IEnumerable<string> modelOps)
        {
            var ops = new List<string>(modelOps ?? Array.Empty<string>());
            if (caps.QnnAvailable && ops.Count > 0)
            {
                int unsupported = 0;
                foreach (var op in ops) if (!caps.QnnSupportedOps.Contains(op)) unsupported++;
                if (unsupported == 0) return new BackendChoice(AIBackend.NpuQnn, false);
                if (unsupported < ops.Count) return new BackendChoice(AIBackend.Mixed, true);
            }
            if (caps.GpuDelegateAvailable) return new BackendChoice(AIBackend.Gpu, false);
            return new BackendChoice(AIBackend.Cpu, false);
        }
    }

    /// <summary>
    /// Thermal- and budget-aware inference scheduling: frame skipping + resolution step-down.
    /// ML never runs on every camera frame; under thermal pressure it degrades before rendering does.
    /// </summary>
    public sealed class InferenceScheduler
    {
        public int InputResolution { get; private set; } = 256; // segmentation input, square
        public int FrameInterval { get; private set; } = 3;     // infer every Nth frame
        private int _frameCounter;

        public void ApplyThermal(ThermalState state)
        {
            switch (state)
            {
                case ThermalState.Nominal: InputResolution = 256; FrameInterval = 3; break;
                case ThermalState.Fair: InputResolution = 192; FrameInterval = 5; break;
                case ThermalState.Serious: InputResolution = 128; FrameInterval = 8; break;
                case ThermalState.Critical: InputResolution = 96; FrameInterval = 15; break;
            }
        }

        /// <summary>True when this camera frame should trigger an inference pass.</summary>
        public bool ShouldRunThisFrame()
        {
            _frameCounter++;
            return _frameCounter % Math.Max(1, FrameInterval) == 0;
        }
    }
}
