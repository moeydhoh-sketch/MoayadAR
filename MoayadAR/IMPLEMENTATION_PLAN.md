# Moayad AR — Implementation Plan

**Status date:** 2026-08-05 · **Prepared in:** Linux container (no Unity Editor, no Android SDK, no device) · **Target:** Samsung Galaxy S25 Ultra (Snapdragon 8 Elite for Galaxy, arm64-v8a)

## Environment finding (decisive)

| Requirement | Present? | Consequence |
|---|---|---|
| Unity Editor 6000.3.x | ❌ not installed | No Unity compile, no APK build possible here |
| Android SDK / NDK / Gradle | ❌ | No APK build possible here |
| Physical Galaxy S25 Ultra / adb | ❌ | Zero device claims allowed |
| JDK 17, .NET 8, Git | ✅ | Platform-agnostic C# logic is compiled and unit-tested for real |

**Scope decision:** this execution produces the **complete Unity project source** (scripts, asmdefs, manifest, localization, docs, signing/CI scaffolding, generated brand assets), with every platform-agnostic module **actually compiled and tested via .NET 8** in `tests-net/`. Unity-dependent modules are written against AR Foundation 6.3 APIs and are marked **Editor-pending / Device-pending** everywhere. No APK is claimed. Build instructions for a Unity-equipped machine are in `docs/signing.md` and `README.md`.

## Pinned versions (verified 2026-08-05)

- Unity **6000.3.21f1** (6.3 LTS, supported to Dec 2027) — `ProjectSettings/ProjectVersion.txt`
- AR Foundation **6.3.5**, ARCore XR Plugin **6.3.x**, AR Core Extensions compatible release
- URP 17.3.x (ships with Unity 6.3), Linear color space, Vulkan primary / GLES3 fallback
- Unity glTFast (`com.unity.cloud.gltfast`) **6.1.0** — GLB/GLTF runtime path
- Assimp (arm64-v8a, NDK/CMake) for FBX/OBJ fallback importer — **native build not possible here**; wrapper + ABI contract provided, documented as the top known limitation
- LiteRT with Qualcomm AI Engine Direct (QNN) accelerator → NPU; fallback GPU → CPU

## Vertical slices

| # | Slice | State in this execution |
|---|---|---|
| 1 | Project foundation (manifest, asmdefs, tokens, icons) | ✅ done |
| 2 | Persistence (versioned JSON DB, projects/rooms/anchors) | ✅ done + tested |
| 3 | Localization AR/EN + RTL | ✅ done + tested |
| 4 | Import pipeline: validation, GLB header parse, OBJ parse, hashing, cache keys, staged progress | ✅ done + tested |
| 5 | Model analysis + Auto Scale recommender | ✅ done + tested |
| 6 | Undo/Redo transform history | ✅ done + tested |
| 7 | On-device AI backend selection + thermal scheduling (logic) | ✅ done + tested |
| 8 | Quality/Realism presets + budgets (logic) | ✅ done + tested |
| 9 | AR session, anchors, relocalization, Behind-Walls mode | 🟡 source written — Unity/device-pending |
| 10 | Occlusion, light estimation, realism pipeline | 🟡 source written — device-pending |
| 11 | Rig detection/edit, FK/IK, poses | 🟡 source written — Editor-pending |
| 12 | Capture (photo/video, MediaStore) | 🟡 source written — device-pending |
| 13 | UI (UI Toolkit, AR/EN, bottom bar, panels) | 🟡 source written — Editor-pending |
| 14 | Signing scripts, CI, docs, repo hygiene | ✅ done |

## Acceptance criteria (per master prompt §18)

Testable here: import analysis correctness (GLB/OBJ), rig-absence honesty for OBJ, localization completeness + RTL data, scale recommender bounds, undo/redo semantics, cache-key stability, backend fallback chain. **Not testable here:** every criterion requiring Unity Editor, emulator, or the S25 Ultra (tracking, occlusion, capture, performance) — listed in `docs/test-matrix.md` as *Not Run — environment unavailable*.

## Top risks

1. Runtime FBX coverage/licensing (Assimp arm64 build) — fallback: honest UI + GLB-preferred flow.
2. Persistent anchor availability — runtime probe, offline relocalization fallback.
3. Unity Android ray tracing — probe Vulkan extensions; default polished PBR path, no fake toggle.
4. Hand-interaction latency — adaptive inference, disabled by default under thermal pressure.
