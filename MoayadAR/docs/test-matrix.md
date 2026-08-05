# Test Matrix — Moayad AR

Legend: ✅ passed · ❌ failed · ⬜ not run. Environments: **NET8** (.NET 8 harness, this repo, ran 2026-08-05) · **Unity** (Unity Editor 6000.3.21f1) · **S25U** (physical Galaxy S25 Ultra).

## Automated (NET8) — 35/35 passed, 2026-08-05

| # | Feature | Env | Expected | Actual | Result |
|---|---|---|---|---|---|
| A1 | GLB magic validation | NET8 | accept real GLB | accepted | ✅ |
| A2 | Renamed OBJ-as-GLB | NET8 | reject, magic_mismatch | rejected | ✅ |
| A3 | Unsupported extension | NET8 | reject | rejected | ✅ |
| A4 | Binary FBX header | NET8 | accept for native path | accepted | ✅ |
| A5 | Oversized file | NET8 | reject per limits | rejected | ✅ |
| A6 | GLB stats parse | NET8 | verts/tris/skin/anims correct | 24v/12t/skin5bones/2clips | ✅ |
| A7 | Truncated GLB | NET8 | reject | rejected (declared-vs-actual length) | ✅ |
| A8 | OBJ stats + bounds | NET8 | counts + bounds correct | correct | ✅ |
| A9 | OBJ quad fan estimate | NET8 | 2 triangles | 2 | ✅ |
| A10 | OBJ vertex limit | NET8 | reject over limit | rejected | ✅ |
| A11 | OBJ rig honesty | NET8 | RigDetected=false | false | ✅ |
| A12 | SHA-256 + cache key | NET8 | deterministic; preset changes key | as expected | ✅ |
| A13 | Import pipeline GLB | NET8 | end-to-end + staged progress | passed | ✅ |
| A14 | FBX flags native importer | NET8 | RequiresNativeImporter=true | true | ✅ |
| A15–A18 | AutoScale paths | NET8 | units→0.9 conf; category→≤0.6; unknown→≤0.3; human range | as expected | ✅ |
| A19–A20 | Undo/Redo semantics | NET8 | apply/restore/re-apply; redo branch cleared | as expected | ✅ |
| A21–A24 | AI backend selection + thermal | NET8 | NPU/mixed/CPU correct; throttle under Critical | as expected | ✅ |
| A25–A27 | Realism presets | NET8 | Ultra gated; degrade ladder; Ultra=30fps | as expected | ✅ |
| A28–A32 | Localization | NET8 | key parity; required AR labels exact; Arabic script; fallback; RTL flag | as expected | ✅ |
| A33–A35 | Persistence | NET8 | Arabic round-trip; traversal rejected; newer schema fails closed | as expected | ✅ |

## Unity Editor tests (⬜ not run — no Unity in authoring env)

| # | Feature | Procedure | Expected | Result |
|---|---|---|---|---|
| U1 | EditMode suite | Unity Test Runner → EditMode | mirrors of A2/A11/A16/A20 pass | ⬜ not run |
| U2 | GLB instantiation | Import rigged GLB via glTFast | hierarchy, clips, skins present | ⬜ not run |
| U3 | Rig badge logic | OBJ then rigged GLB | disabled w/ explanation; enabled w/ badge | ⬜ not run |
| U4 | UI AR/EN | Switch language at runtime | full RTL/LTR, no clipping | ⬜ not run |
| U5 | Pose round-trip | Edit pose → save → reload project | pose restored from JSON | ⬜ not run |

## Device tests — Galaxy S25 Ultra (⬜ not run — no device)

| # | Feature | Procedure | Expected | Result |
|---|---|---|---|---|
| P1 | Place + transform | Floor place, drag/pinch/twist, raise/lower, undo/redo | stable, no drift jumps | ⬜ not run |
| P2 | Relocalization | Leave/return ×10 trials | position/rotation error measured & reported | ⬜ not run |
| P3 | Depth occlusion | Real object in front of model | correct per-pixel occlusion | ⬜ not run |
| P4 | Person occlusion | Person crosses in front/behind | stable mask, limited edge flicker | ⬜ not run |
| P5 | Behind-Walls mode | Model behind real wall | wall occludes; insufficient scan → prompt | ⬜ not run |
| P6 | Lighting change | Dim/bright room | plausible light dir, exposure, shadows | ⬜ not run |
| P7 | Capture | Photo + 1080p30 video via MediaStore | composited, opens in gallery | ⬜ not run |
| P8 | AI backend | Diagnostics overlay | actual NPU/GPU/CPU/mixed shown | ⬜ not run |
| P9 | RT probe | RayTracingProbe on device | result recorded honestly | ⬜ not run |
| P10 | Tracking loss | Cover lens mid-session | content freezes, recovers, no jumps | ⬜ not run |
| P11 | Import cancel/timeout | Cancel mid-import; hostile file | clean cancel, no crash | ⬜ not run |
| P12 | Low storage / memory pressure | Fill storage; background apps | warnings; graceful degradation | ⬜ not run |
| P13 | Sustained 30-min | Balanced profile continuous AR | avg FPS, 1% low, peak mem, thermals, battery — reported | ⬜ not run |
| P14 | FBX via Assimp bridge | Rigged FBX import | works or documented exact failure | ⬜ not run (bridge not built) |
| P15 | Secret scan | gitleaks on repo | no keystore/password/key/media | ✅ clean (this repo) |
