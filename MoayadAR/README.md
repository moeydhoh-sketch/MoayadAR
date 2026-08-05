# Moayad AR · مؤيد AR

[English](#english) · [العربية](#العربية)

---

## English

Mobile augmented-reality app for the **Samsung Galaxy S25 Ultra**: import GLB/glTF/FBX/OBJ models at runtime, anchor them in your room, and integrate them convincingly with real depth occlusion, person occlusion, HDR light estimation, and physically based rendering. Arabic + English UI with true RTL support.

**Package ID:** `com.moayad.ar` · **Unity:** 6000.3.21f1 (6.3 LTS) · **Pipeline:** URP, Linear, Vulkan

### Repository status (honest)

This repository was authored in an environment **without Unity Editor, Android SDK, or a Galaxy S25 Ultra**. What that means:

- ✅ Complete source: all modules, asmdefs, pinned manifest, localization, UI, docs, signing/CI scaffolding.
- ✅ The platform-agnostic core (import validation & parsers, analysis/auto-scale, persistence, localization, undo/redo, AI-backend selection, realism presets) is **compiled and unit-tested with .NET 8** — see `tests-net/` and `BUILD_STATUS.md` (35/35 passing).
- 🟡 Unity-layer code (AR, occlusion, capture, rig editor) is written against AR Foundation 6.3.5 APIs and marked Editor/Device-pending. **It has not been compiled by Unity or run on a device here.**
- ❌ No APK was produced. Build steps below produce it on a Unity-equipped machine.

### Build the APK (requires a machine with Unity)

1. Install **Unity 6000.3.21f1** with *Android Build Support* (SDK/NDK, OpenJDK) via Unity Hub.
2. Open this folder in Unity. The pinned `Packages/manifest.json` resolves AR Foundation 6.3.5, ARCore XR Plugin, glTFast 6.1.0 automatically.
3. In **Project Settings → XR Plug-in Management → Android**, enable **ARCore**. Set color space **Linear**, graphics API **Vulkan** (GLES3 fallback only if a proven incompatibility appears), target architectures **ARM64** only.
4. Assign the generated icons in **Player Settings** from `Assets/MoayadAR/Branding/` (adaptive foreground/background/monochrome + legacy densities are already generated).
5. Signing: copy `keystore.properties.example` → `keystore.properties`, run `scripts/generate-keystore.sh`, then **File → Build**. Details: `docs/signing.md`. Output name: `Moayad-AR-v<version>-s25ultra-arm64-release.apk` + `.sha256`.
6. Install on the S25 Ultra: `adb install -r <apk>`.

FBX/OBJ native import additionally requires building the Assimp arm64 bridge — see `docs/known-limitations.md` (top item) before promising FBX support.

### Verify the tested core without Unity

```bash
cd tests-net
dotnet build
dotnet bin/Debug/net8.0/MoayadAR.TestsNet.dll
```

### Layout

`Assets/MoayadAR/` modules: Core · Persistence · Localization · Import · Analysis · Interaction · OnDeviceAI · Rendering · AR · RenderingUnity · Rigging · Capture · UI · PlatformAndroid · Tests. Docs in `docs/`, brand source + generated assets in `branding/`, signing/CI in `scripts/` and `.github/`.

---

## العربية

تطبيق واقع معزز للهاتف **Samsung Galaxy S25 Ultra**: استورد مجسمات GLB/glTF/FBX/OBJ أثناء التشغيل، ثبّتها في غرفتك، وادمجها بشكل مقنع مع احتجاب العمق الحقيقي، واحتجاب الأشخاص، وتقدير الإضاءة HDR، وعرض فيزيائي دقيق. واجهة عربية وإنجليزية مع دعم RTL حقيقي.

**معرّف الحزمة:** `com.moayad.ar` · **Unity:** 6000.3.21f1 (إصدار LTS 6.3) · **المعالجة:** URP، ألوان Linear، Vulkan

### حالة المستودع (بصراحة)

كُتب هذا المستودع في بيئة **بدون محرر Unity وبدون Android SDK وبدون جهاز Galaxy S25 Ultra**. ويعني ذلك:

- ✅ المصدر كامل: كل الوحدات، وملفات asmdef، والحزم المثبّتة، والترجمة، والواجهة، والتوثيق، وسكربتات التوقيع وCI.
- ✅ النواة المستقلة عن المحرك (التحقق من الاستيراد والمحللات، التحليل والمقياس التلقائي، الحفظ، الترجمة، التراجع/الإعادة، اختيار محرك الذكاء الاصطناعي، إعدادات الواقعية) **مُترجمة ومختبرة فعلياً عبر .NET 8** — انظر `tests-net/` و`BUILD_STATUS.md` (نجاح 35/35).
- 🟡 شيفرة طبقة Unity (الواقع المعزز، الاحتجاب، التصوير، محرر الهيكل) مكتوبة ضد واجهات AR Foundation 6.3.5 ومعلَّمة بأنها بانتظار المحرر/الجهاز. **لم تُترجم في Unity ولم تعمل على جهاز هنا.**
- ❌ لم يُنتَج أي ملف APK. خطوات البناء أدناه تنتجه على جهاز فيه Unity.

### بناء APK (يتطلب جهازاً فيه Unity)

1. ثبّت **Unity 6000.3.21f1** مع *Android Build Support* عبر Unity Hub.
2. افتح هذا المجلد في Unity؛ سيجلب `Packages/manifest.json` المثبّت الحزم تلقائياً.
3. في **XR Plug-in Management → Android** فعّل **ARCore**. اضبط الألوان **Linear** وواجهة الرسوميات **Vulkan** ومعمارية **ARM64** فقط.
4. عيّن الأيقونات المولّدة من `Assets/MoayadAR/Branding/` في إعدادات Player.
5. التوقيع: انسخ `keystore.properties.example` إلى `keystore.properties`، شغّل `scripts/generate-keystore.sh`، ثم **File → Build**. التفاصيل في `docs/signing.md`.
6. التثبيت على الجهاز: `adb install -r <apk>`.

دعم FBX/OBJ الأصلي يتطلب إضافة جسر Assimp arm64 — راجع `docs/known-limitations.md` قبل الوعد بدعم FBX.

### الترخيص

انظر `LICENSES.md` و`THIRD_PARTY_NOTICES.md`.
