# Privacy — Moayad AR

## Defaults

- **On-device only.** Camera frames, depth, person/hand masks, and room data are processed locally. No camera frame is ever uploaded for analysis.
- **No telemetry.** No analytics, no crash reporting, no ad SDKs, no tracking identifiers — none are included, so none can leak.
- **No background media storage.** Raw room images/video are never written unless the user explicitly captures a photo or video (saved via MediaStore to their gallery).
- **Cloud Anchors: opt-in only.** Disabled by default; requires the user's own Google Cloud project configuration. No API keys or service credentials are stored by the app or present in this repository.

## Permissions (requested at point of use)

| Permission | When requested | Never requested for |
|---|---|---|
| Camera | First AR session | anything else |
| Microphone | Starting a video recording with sound | silent AR, photos |
| (none) for files | — | Storage uses SAF `ACTION_OPEN_DOCUMENT`; no `MANAGE_EXTERNAL_STORAGE` |

## Data stored locally

`ProjectStore` JSON: project/room metadata, anchor IDs, transforms, settings. No images, no identifiers of persons, no location data. Deleting a project file removes its data; the cache can be cleared from Settings (`action.clearCache`).

## What we deliberately do NOT log

API keys, signing passwords, personal file paths, raw camera frames. The CI workflow includes a secret scan; `.gitignore` excludes keystores, `keystore.properties`, captured media, and imported user models.

## Arabic summary (shown in-app, `privacy.summary`)

تُعالج إطارات الكاميرا وبيانات الغرفة على هذا الجهاز. لا توجد تحليلات أو قياس عن بُعد أو تقارير أعطال. المرتكزات السحابية معطّلة ما لم تضبطها بنفسك.
