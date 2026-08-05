# DECISIONS.md — Moayad AR

| # | Decision | Evidence |
|---|---|---|
| D1 | **Unity 6000.3.21f1 (6.3 LTS)** over 6.5 tech stream. LTS is the production branch; supported to Dec 2027. | endoflife.date/unity; Unity 6 Releases & Support page (checked 2026-08-05) |
| D2 | **AR Foundation 6.3.5 + ARCore XR Plugin 6.3.x**. 6.3.5 fixes raycast-after-XROrigin-move (UUM-138221) — directly relevant to placement correctness. | docs.unity3d.com AR Foundation changelog |
| D3 | **Unity glTFast 6.1.0** (`com.unity.cloud.gltfast`) as the GLB/GLTF runtime importer. Maintained by Unity, Apache-2.0, Android arm64 supported. | Unity glTFast 6.1.0 installation docs |
| D4 | **Assimp (arm64-v8a)** for FBX/OBJ fallback, behind `INativeImporter` so it can be replaced. Not bundled prebuilt — build script provided; license BSD-3-Clause, attribution in THIRD_PARTY_NOTICES. | Master prompt §5; licensing constraint |
| D5 | **LiteRT + Qualcomm AI Engine Direct (QNN)** as primary NPU path; NNAPI avoided (deprecated in Android 15+). Fallback GPU → CPU per model. | Google LiteRT Qualcomm NPU docs; Android NNAPI deprecation notice |
| D6 | **No hardware-RT toggle shipped.** Vulkan RT extensions must be probed on device and a Unity Android RT path validated before any UI is enabled; default path is PBR + light estimation + reflection probes. | Master prompt §3/§8; Unity Android RT exposure unverified |
| D7 | **Platform-agnostic core in pure C#** (`MoayadAR.Core` etc. have no UnityEngine references) so logic compiles/tests under .NET 8 here and under Unity unchanged. Unity adapters live in separate asmdefs. | This environment has .NET 8 but no Unity |
| D8 | **Newtonsoft Json.NET** for persistence: official `com.unity.nuget.newtonsoft-json` in Unity, NuGet in the .NET test harness — one serializer, two hosts. | Unity package registry |
| D9 | **JSON (versioned) over SQLite** for the project DB: human-inspectable, diff-friendly, per-project files; schema version field with migrator hook. | Master prompt §4 allows either |
| D10 | **UI Toolkit (UXML/USS)** for runtime UI: first-class RTL support path in Unity 6, resolution-independent for S25 Ultra. | Unity 6 UI Toolkit docs |
| D11 | **Package ID `com.moayad.ar`** — no local conflict found. | Master prompt §2 |
| D12 | OBJ = **no rig, ever**. UI shows "No rig detected in this file" and disables Rig Edit. OBJ has no skeleton/animation payload. | Master prompt §3 |
