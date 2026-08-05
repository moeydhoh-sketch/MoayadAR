using System;
using UnityEngine;

namespace MoayadAR.RenderingUnity
{
    /// <summary>
    /// Honest ray-tracing gate (master prompt §8): probe Vulkan device extensions and Unity's
    /// render-path support at runtime. Until both are proven on the S25 Ultra, the RT setting
    /// stays disabled with the accurate explanation "realism.raytracingUnavailable" and the
    /// PBR + light-estimation + reflection-probe path is used. There is no fake RT toggle.
    /// DEVICE-PENDING.
    /// </summary>
    public static class RayTracingProbe
    {
        public readonly struct ProbeResult
        {
            public readonly bool Supported;
            public readonly string Reason; // diagnostic, English-only, goes to logs/diagnostics report
            public ProbeResult(bool supported, string reason) { Supported = supported; Reason = reason; }
        }

        public static ProbeResult Probe()
        {
            if (SystemInfo.graphicsDeviceType != Rendering.GraphicsDeviceType.Vulkan)
                return new ProbeResult(false, "graphics API is not Vulkan: " + SystemInfo.graphicsDeviceType);

            // Ray tracing on Unity Android requires a render path that actually exposes RT on mobile.
            // As of Unity 6.3, URP Android does not expose real-time RT; we verify rather than assume:
            bool claimedBySystem = SystemInfo.supportsRayTracing;
            if (!claimedBySystem)
                return new ProbeResult(false, "SystemInfo.supportsRayTracing == false on this driver/URP stack");

            // Even when the API claims support, the feature must be validated with a measured on-device
            // render test before the setting is enabled for users. Default: stay on the PBR path.
            return new ProbeResult(false,
                "API claims RT support; pending measured on-device validation (see docs/known-limitations.md)");
        }
    }
}
