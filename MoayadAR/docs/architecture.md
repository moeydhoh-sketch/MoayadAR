# Architecture — Moayad AR

## Component map

```
┌──────────────────────────── UI (UI Toolkit, AR/EN, RTL) ───────────────────────────┐
│ MainUIController · analysis card · bottom bar · context panel · diagnostics overlay │
└───────────────┬──────────────────────────────────────────────────┬─────────────────┘
                │ events                                           │ events
┌───────────────▼──────────┐                          ┌────────────▼────────────────┐
│ AR (Unity layer)         │                          │ Capture (Unity+JNI)         │
│ ARPlacementController    │                          │ CaptureController           │
│ RelocalizationController │                          │ MediaStoreBridge            │
│ BehindWallsMode          │                          │ AndroidVideoRecorder (AAR)  │
└───────┬──────────────────┘                          └───────────────────────────────┘
        │ anchors, poses
┌───────▼───────────────────────────────────────────────────────────────────────────┐
│ PLATFORM-AGNOSTIC CORE (no UnityEngine refs — compiled & tested under .NET 8)      │
│ Core (Float3/Float4/TransformPose/Result) · Persistence (ProjectStore, models)     │
│ Localization (tables AR/EN) · Import (Validator, GlbHeaderReader, ObjModelReader,  │
│ Pipeline, Cache) · Analysis (ModelReport, AutoScaleRecommender) ·                  │
│ Interaction (UndoRedoStack, SetPoseCommand) · OnDeviceAI (BackendSelector,         │
│ InferenceScheduler) · Rendering (RealismPresets, QualityBudget)                    │
└───────┬───────────────────────────────┬───────────────────────────────┬───────────┘
        │                               │                               │
┌───────▼───────────┐        ┌──────────▼──────────┐        ┌───────────▼──────────┐
│ RenderingUnity     │        │ Rigging              │        │ PlatformAndroid       │
│ RealismController  │        │ RigEditController    │        │ DocumentPickerBridge  │
│ RayTracingProbe    │        │ (FK + CCD IK, poses) │        │ DeviceCapabilityProbe │
└────────────────────┘        └─────────────────────┘        └──────────────────────┘
```

## Data flow — import → place → persist

1. `DocumentPickerBridge` (SAF, persistable URI) → content stream.
2. `ImportPipeline`: validate (extension+magic+limits) → SHA-256 → cache key (sha + importer + pipeline + preset) → statistics parse (GLB header/OBJ streaming) → `ModelReport` + `ImportOutcome`.
3. Geometry instantiation: **glTFast 6.1.0** (GLB/glTF) or **Assimp AAR** (FBX; OBJ via Assimp or Unity mesh path). Progress phases are real (`ImportPhase`).
4. `AutoScaleRecommender` proposes size with confidence + reason → ghost preview → user choice.
5. `ARPlacementController` attaches content to an **ARAnchor**; `PlacedModelRecord` stores the anchor-relative `TransformPose` (never camera-relative).
6. `ProjectStore` saves Project + Room (anchors, walls, coverage) as versioned JSON, atomic write.

## Thread boundaries

- **Main thread:** Unity rendering, AR managers, UI.
- **Background:** file I/O, hashing, parsing, texture decode (glTFast async), ML inference (LiteRT worker), capture encode (MediaCodec thread).
- Cancellation tokens flow through import; `InferenceScheduler` yields under thermal pressure.

## Native resource ownership

| Resource | Owner | Released by |
|---|---|---|
| MediaCodec encoder/surface | `AndroidVideoRecorder` (AAR) | `Dispose()` (finally on stop/pause/error) |
| Microphone | recorder | same — and only after mic permission at point of use |
| SAF URI permission | activity layer | persists; revocable by user in system settings |
| LiteRT interpreter/delegate | AAR AI module | session dispose; models listed with model cards |
| Assimp scene | native importer | RAII wrapper, import timeout + cancellation |

## Honesty mechanisms (by design)

- `Result<T>` everywhere: error code + localization key, no swallowed exceptions.
- OBJ → `RigDetected=false` structurally; UI disables Rig Edit with explanation.
- Ultra tier & RT: gated by runtime probes (`DeviceCapabilityProbe`, `RayTracingProbe`), not device name.
- Diagnostics exposes the *actual* AI backend (NPU/GPU/CPU/mixed) and tracking state.
