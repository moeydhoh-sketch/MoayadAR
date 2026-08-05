# Known Limitations — Moayad AR (as of 2026-08-05)

Ordered by importance. Each lists the fallback actually implemented.

## 1. Runtime FBX import is NOT functional yet
**What exists:** FBX magic-byte validation, the `INativeImporter`/AAR contract, honest UI flagging (`RequiresNativeImporter`).
**What's missing:** Assimp compiled for `arm64-v8a` (NDK/CMake) and its Unity wrapper — requires an Android NDK toolchain not present in the authoring environment.
**Fallback:** GLB/glTF is the primary path and fully supported via glTFast 6.1.0; OBJ statistics/analysis works (geometry path via Assimp pending the same bridge). The UI never claims FBX works. FBX feature coverage (skins, morphs, per-version quirks) must be validated with real files after the bridge lands.

## 2. Nothing Unity-layer has been compiled or device-tested
AR placement, relocalization, Behind-Walls mode, occlusion/realism controller, rig editor, capture, and UI are written against AR Foundation 6.3.5 / URP 17.3 APIs but **have not been compiled by Unity Editor nor run on a Galaxy S25 Ultra**. Expect a normal bring-up pass (API drift fixes, scene wiring, shader for wall-mask `_WallMaskEnabled`) on first Unity open.

## 3. Hardware ray tracing: disabled by default
`RayTracingProbe` returns *disabled* until (a) Vulkan RT extensions and (b) a measured Unity Android render path are both proven on the S25 Ultra. **Fallback:** high-quality PBR + HDR light estimation + reflection probes + tuned shadows. There is no user-facing fake RT toggle.

## 4. Persistent anchors: probed at runtime, not assumed
ARCore persistent-anchor availability depends on device/services state. If unsupported, the app reports it (`anchor.persistentUnsupported`) and offers the **Relocalize Room** guided flow instead of pretending persistence. Cloud Anchors remain opt-in with user-provided Google Cloud configuration — no keys are stored or shipped.

## 5. Person occlusion is a refinement, geometric depth is the backbone
Segmentation (LiteRT, NPU→GPU→CPU) refines edges but never replaces ARCore environment depth. Under thermal pressure the `InferenceScheduler` reduces resolution/frame rate; edge flicker under fast motion/low light is expected and must be measured on device (test-matrix rows P6).

## 6. Hand interaction: mode exists in logic; latency unvalidated
Pinch confidence/dwell/hysteresis parameters are defined, but real-world latency on the S25 Ultra NPU path is unmeasured. The mode ships **disabled by default** and self-throttles under thermals.

## 7. No APK/AAB produced in the authoring environment
No Unity Editor / Android SDK / Gradle was available. Build instructions: `README.md` + `docs/signing.md`. The output must be named `Moayad-AR-v<version>-s25ultra-arm64-release.apk` with a `.sha256` sidecar.

## 8. Performance numbers are budgets, not measurements
All FPS/thermal targets in `docs/performance.md` are engineering budgets pending the 30-minute sustained S25 Ultra test (test-matrix P13). No performance claim should be quoted as measured until then.
