# Performance & Thermal Budgets — Moayad AR

**Status: budgets, not measurements.** The 30-minute sustained S25 Ultra test has NOT been run (no device in the authoring environment). Nothing below is a measured result.

## Targets (design budgets)

| Profile | Target | Notes |
|---|---|---|
| Battery | 30 fps | renderScale 0.8, no soft shadows, depth occlusion off, ML 128px |
| Balanced | 60 fps normal use | the shipping default |
| High | 60 fps | larger budgets, 4K textures |
| Ultra | **stable 30 fps** | gated by capability probe + thermals; never promised at 60 |

No profile promises 60 fps for unlimited polygon counts — budgets are enforced at import (`ImportLimits`) with a labeled Heavy Asset mode.

## Budgets (defaults in `RealismPresets` / `ImportLimits`)

- Triangles: 500K (Battery) / 1.5M (Balanced) / 3M (High) / 8M (Ultra, Heavy mode only)
- Skinning bones: 64–512 by tier · Textures: 1024–8192 max dimension · Active renderers ≤ 256
- Shadow resolution 1024–4096 · Reflection probe refresh every 15–240 frames by tier
- ML input 128–384px square, every 3rd–15th frame by thermal state

## Mechanisms implemented (logic verified in tests-net)

- Thermal ladder: `RealismPresets.DegradeForThermal` — Ultra→High→Balanced→Battery as `PowerManager.getCurrentThermalStatus` rises.
- Inference scheduling: `InferenceScheduler` — frame skipping + resolution step-down; ML never runs on every camera frame.
- Dynamic resolution ceiling per profile (`renderScale`).

## Measurement plan (when a device is available)

1. 30-min continuous AR session with a 1.5M-triangle asset, Balanced profile: avg FPS, 1% low / p99 frame time, peak memory (PSS), thermal transitions, battery drain.
2. Import benchmark suite: 10 MB / 100 MB / 500 MB GLB — stage-by-stage timings (no UFS-4.0 marketing numbers; real parse+decode+upload costs).
3. Video: 1080p30 for 10 min — dropped-frame count; only then consider enabling 4K.
4. Diagnostics export (`diagnostics.export`) attaches all of the above as JSON.
