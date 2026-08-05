# Signing & APK Packaging — Moayad AR

## One-time keystore setup (local machine, never committed)

```bash
cp keystore.properties.example keystore.properties   # fill in real values locally
bash scripts/generate-keystore.sh                     # interactive keytool, or uses env vars
```

`generate-keystore.sh` creates `moayad-release.keystore` in the repo root (git-ignored) using `keytool -genkeypair -v -keystore moayad-release.keystore -alias moayad -keyalg RSA -keysize 4096 -validity 10950`. It reads `MOAYAD_STORE_PASSWORD` / `MOAYAD_KEY_PASSWORD` from the environment if set; otherwise it prompts. Values are never echoed to logs.

> **Losing this keystore means you can never update the app under the same signature.** Back it up somewhere safe and private. `keystore.properties` and the keystore itself are excluded by `.gitignore` and must never be committed.

## Unity wiring

**Player Settings → Publishing Settings:** enable *Custom Keystore*, point to `moayad-release.keystore`, alias `moayad`. Unity reads passwords from `keystore.properties` only if you wire it via an editor build script; the safe path is entering them in the Editor UI (stored in the OS credential store, not in the project).

## Build outputs

- APK (priority — personal install on the S25 Ultra): `Moayad-AR-v<version>-s25ultra-arm64-release.apk`
- Checksum: `<same>.sha256` via `sha256sum`
- AAB: optional, only if Play distribution is ever wanted

```bash
sha256sum Moayad-AR-v1.0.0-s25ultra-arm64-release.apk > Moayad-AR-v1.0.0-s25ultra-arm64-release.apk.sha256
```

## If no signing inputs are available

Build a debug APK and/or an unsigned release artifact and state exactly that in the delivery report — do not generate a keystore with a hardcoded/public password, and never commit one.

## Install

```bash
adb install -r Moayad-AR-v1.0.0-s25ultra-arm64-release.apk
```
