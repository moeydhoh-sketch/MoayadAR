# Licenses — Moayad AR

## Application code

Moayad AR application source in this repository: © Moayad, all rights reserved (private project).
No third-party code is vendored in this repository; all dependencies are consumed as packages.

## Runtime dependencies (resolved by Unity Package Manager / Gradle)

| Dependency | Version | License |
|---|---|---|
| Unity glTFast (`com.unity.cloud.gltfast`) | 6.1.0 | Apache-2.0 |
| AR Foundation (`com.unity.xr.arfoundation`) | 6.3.5 | Unity Companion License |
| ARCore XR Plugin (`com.unity.xr.arcore`) | 6.3.5 | Unity Companion License / Google ToS |
| Universal Render Pipeline | 17.3.x | Unity Companion License |
| Newtonsoft Json.NET (`com.unity.nuget.newtonsoft-json`) | 3.2.1 | MIT |
| Unity Test Framework | 1.5.1 | Unity Companion License |
| LiteRT (on-device ML runtime) | current stable | Apache-2.0 |
| Qualcomm AI Engine Direct (QNN) SDK | per Qualcomm AI Hub | Qualcomm license (evaluation terms apply) |

## Planned native dependency (not yet built — see docs/known-limitations.md #1)

| Dependency | License | Note |
|---|---|---|
| Assimp (FBX/OBJ runtime import, arm64-v8a) | BSD-3-Clause | Compatible; attribution required — see THIRD_PARTY_NOTICES.md |

**Rejected:** any GPL/LGPL importer, paid proprietary runtime importers, or license-ambiguous code (master prompt §5.3). Assimp (BSD-3) is the chosen path precisely because it is permissive.

## Fonts

| Font | License |
|---|---|
| Noto Sans Arabic | SIL Open Font License 1.1 |
| Inter (Latin fallback) | SIL Open Font License 1.1 |

Fonts are downloaded at project setup; OFL 1.1 permits bundling with attribution.

## ML models (to be added with model cards)

Every model shipped must carry: name, source, license, SHA-256 checksum, input normalization, expected limitations. No model is currently bundled.

## Logo & brand assets

Logo source: `https://i.postimg.cc/jjPsJXbs/O.png` (provided by the project owner). Generated icon/splash derivatives in `branding/generated/`.
