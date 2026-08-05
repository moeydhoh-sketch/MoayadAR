# TASKS.md — Moayad AR

Rule: exactly one task is `▶ current`.

- [x] Inspect build environment; record capabilities honestly
- [x] Download + validate logo; extract palette; generate icons/splash/tokens
- [x] Verify official versions (Unity 6000.3.21f1 LTS, AR Foundation 6.3.5, glTFast 6.1.0, LiteRT QNN)
- [x] Write IMPLEMENTATION_PLAN / TASKS / DECISIONS / BUILD_STATUS
- [x] Unity scaffold: manifest.json (pinned), ProjectVersion.txt, asmdefs, folder layout
- [x] Core: math structs, Result types, cancellation helpers, asmdef
- [x] Persistence: versioned JSON store, Project/Room/Anchor/PlacedModel/PoseSnapshot models
- [x] Localization: en/ar tables (all required labels), lookup service, RTL data
- [x] Import: file validator (ext/MIME/magic), GLB header parser, OBJ parser, SHA-256 cache key, staged progress, limits
- [x] Analysis: ModelReport, rig detection, AutoScaleRecommender (confidence + reasons)
- [x] Interaction logic: multi-step Undo/Redo for transforms/placement/pose
- [x] OnDeviceAI: backend capability model, NPU→GPU→CPU fallback chain, thermal scheduler
- [x] Rendering logic: Realism presets (Battery/Balanced/High/Ultra gating), budgets
- [x] Unity-layer source: AR placement/anchors/Behind-Walls, occlusion/realism controller, rig editor (FK/IK), capture, UI Toolkit screens (Editor/Device-pending)
- [x] tests-net harness: compile all platform-agnostic sources with .NET 8 and run unit tests
- [x] Docs: architecture, performance, privacy, signing, known-limitations, test-matrix, README (AR+EN), LICENSES, THIRD_PARTY_NOTICES
- [x] Signing scripts, .gitignore/.gitattributes, CI workflow, git init (local only)
- [x] Final delivery report with exact artifact paths and honest limitation list

▶ current: none — execution complete (Unity/APK phases blocked by environment, see BUILD_STATUS.md)
