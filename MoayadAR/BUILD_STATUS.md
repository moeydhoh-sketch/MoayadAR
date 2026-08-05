# BUILD_STATUS.md — Moayad AR

**Last updated:** 2026-08-05 · **Authoring environment:** Linux x86_64 container (no Unity, no Android SDK, no device)

## Environment inspection (2026-08-05)

| Tool | Result |
|---|---|
| Unity Editor / Unity Hub | **not installed** → Unity compile & APK build impossible here |
| Android SDK / NDK / Gradle / adb | **not installed** → no APK, no device deploy |
| JDK | OpenJDK 17.0.19 |
| .NET | 8.0.423 ✅ used for core verification |
| Git | 2.39.5 |
| Physical Galaxy S25 Ultra | **absent** → zero device claims |

## Logo

- URL: `https://i.postimg.cc/jjPsJXbs/O.png` → HTTP 200, `image/png`, 116,997 bytes, 800×800 RGBA (validated with `file` + PIL decode).
- SHA-256: `9922c255a243605b60c8733a6295a3a38439e433fe9972b3169f43c79b3df9fb`
- Palette extracted (k-means): dominant `#4B342C` (41.7%), dark `#2F1F18` (26.5%) → tokens in `branding/generated/design-tokens.json`.
- Generated: adaptive icon foreground/background/monochrome (432px), 5 launcher densities, splash 1152px, brand mark 512px → `branding/generated/` and `Assets/MoayadAR/Branding/`.

## Commands executed and results

| Command | Exit | Result |
|---|---|---|
| `dotnet build tests-net/MoayadAR.TestsNet.csproj` | 0 | compiles all platform-agnostic Unity sources (Core, Persistence, Localization, Import, Analysis, Interaction, OnDeviceAI, Rendering) with 0 errors, 0 warnings |
| `dotnet bin/Debug/net8.0/MoayadAR.TestsNet.dll` | 0 | **35/35 tests passed** (initial run 33/35 → 2 real defects found & fixed: GLB truncation check, traversal-test contract) |
| `curl -sSL <logo>` | 0 | validated PNG (details above) |
| `dotnet run` (first attempt) | 1 | environment mount forbids exec bit → documented; tests run via `dotnet <dll>` instead |

## Defects found by testing (and fixed)

1. **GLB reader accepted truncated containers** — declared-length vs actual-stream-length check added (`GlbHeaderReader`), test now passes.
2. **Path-traversal test contract** — Load() wraps ArgumentException into a failed Result; test corrected to assert "never succeeds" (the actual security contract).

## Artifacts

- Project root: `/mnt/agents/output/MoayadAR/` (98 files)
- Test harness: `tests-net/` (reproducible: `dotnet build && dotnet bin/Debug/net8.0/MoayadAR.TestsNet.dll`)
- Test list & device matrix: `docs/test-matrix.md`

## NOT produced (honest)

- ❌ APK / AAB / checksums — no Unity Editor, Android SDK, or Gradle in this environment. Build recipe: `README.md` + `docs/signing.md`.
- ❌ Unity compilation of AR/Rendering/Rigging/Capture/UI layers — Editor-pending.
- ❌ Assimp arm64 FBX bridge — NDK absent; contract + honest UI in place.
- ❌ Any device measurement (FPS, thermals, occlusion, relocalization error).

## Blockers → next actions (on a Unity-equipped machine)

1. Open in Unity 6000.3.21f1 + Android Build Support → fix any API-drift compile errors in Unity-layer asmdefs.
2. Wire scene: ARSession, XROrigin + managers, UIDocument with `MainScreen.uxml`/`Theme.uss`, controllers.
3. Build Assimp AAR (arm64-v8a) if FBX is required; otherwise ship GLB-first honestly.
4. Run Unity EditMode suite, then the S25 Ultra device matrix (`docs/test-matrix.md` P1–P14).
5. Signing per `docs/signing.md` → `Moayad-AR-v<version>-s25ultra-arm64-release.apk` + `.sha256`.
