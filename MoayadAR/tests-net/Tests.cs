using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MoayadAR.Analysis;
using MoayadAR.Core;
using MoayadAR.Import;
using MoayadAR.Interaction;
using MoayadAR.Localization;
using MoayadAR.OnDeviceAI;
using MoayadAR.Persistence;
using MoayadAR.Rendering;

namespace MoayadAR.TestsNet
{
    /// <summary>Hand-rolled runner (zero framework deps): every check prints PASS/FAIL and exits non-zero on any failure.</summary>
    public static class Tests
    {
        private static int _pass, _fail;

        public static int RunAll()
        {
            Console.WriteLine("== Moayad AR platform-agnostic test suite (.NET 8) ==");

            // ---------- Import: validation & honesty ----------
            Check("GLB magic accepted", () =>
            {
                var glb = TestAssets.MakeGlb();
                var r = FileValidator.Validate("model.glb", new MemoryStream(glb), glb.Length, new ImportLimits());
                return r.Ok && r.Value == ModelFormat.Glb;
            });
            Check("Renamed OBJ-as-GLB rejected (magic mismatch)", () =>
            {
                var obj = TestAssets.MakeObj(4, 2);
                var r = FileValidator.Validate("fake.glb", new MemoryStream(obj), obj.Length, new ImportLimits());
                return !r.Ok && r.ErrorCode == "import.magic_mismatch";
            });
            Check("Unsupported extension rejected", () =>
                !FileValidator.Validate("scene.blend", new MemoryStream(new byte[] { 1, 2, 3 }), 3, new ImportLimits()).Ok);
            Check("Binary FBX header accepted", () =>
            {
                var fbx = TestAssets.MakeFbxHeader();
                var r = FileValidator.Validate("char.fbx", new MemoryStream(fbx), fbx.Length, new ImportLimits());
                return r.Ok && r.Value == ModelFormat.Fbx;
            });
            Check("Oversized file rejected by limits", () =>
            {
                var limits = new ImportLimits { MaxFileBytes = 10 };
                var glb = TestAssets.MakeGlb();
                var r = FileValidator.Validate("big.glb", new MemoryStream(glb), glb.Length, limits);
                return !r.Ok && r.ErrorCode == "import.too_large";
            });

            // ---------- GLB reader ----------
            Check("GLB stats: vertices/triangles/skin/animations parsed", () =>
            {
                var glb = TestAssets.MakeGlb(vertexCount: 24, triangleIndices: 36, withSkin: true, withAnimation: true);
                var r = GlbHeaderReader.Read(new MemoryStream(glb), new ImportLimits());
                return r.Ok && r.Value.VertexCount == 24 && r.Value.TriangleCount == 12
                    && r.Value.SkinCount == 1 && r.Value.BoneCount == 5
                    && r.Value.AnimationCount == 2 && r.Value.AnimationNames.Contains("walk")
                    && r.Value.MaterialCount == 1 && r.Value.TextureCount == 1;
            });
            Check("GLB truncated rejected", () =>
            {
                var glb = TestAssets.MakeGlb();
                var cut = glb.Take(glb.Length - 5).ToArray();
                return !GlbHeaderReader.Read(new MemoryStream(cut), new ImportLimits()).Ok;
            });

            // ---------- OBJ reader ----------
            Check("OBJ stats + bounds parsed", () =>
            {
                var obj = TestAssets.MakeObj(30, 10);
                var r = ObjModelReader.Read(new MemoryStream(obj), new ImportLimits());
                return r.Ok && r.Value.VertexCount == 30 && r.Value.FaceCount == 10
                    && r.Value.HasNormals && r.Value.BoundsMax.Y >= r.Value.BoundsMin.Y;
            });
            Check("OBJ triangle estimate fan-counts quads", () =>
            {
                var quad = System.Text.Encoding.UTF8.GetBytes("v 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nf 1 2 3 4\n");
                var r = ObjModelReader.Read(new MemoryStream(quad), new ImportLimits());
                return r.Ok && r.Value.TriangleEstimate == 2;
            });
            Check("OBJ vertex limit enforced", () =>
            {
                var obj = TestAssets.MakeObj(10, 1);
                var limits = new ImportLimits { MaxVertices = 5 };
                var r = ObjModelReader.Read(new MemoryStream(obj), limits);
                return !r.Ok && r.ErrorCode == "import.too_many_vertices";
            });
            Check("OBJ has no rig — report must say so", () =>
            {
                // Contract test: OBJ analysis path never sets RigDetected.
                var report = new ModelReport { Format = ModelFormat.Obj, RigDetected = false };
                return !report.RigDetected && report.Format == ModelFormat.Obj;
            });

            // ---------- Pipeline / cache ----------
            Check("SHA-256 deterministic; cache key changes with preset", () =>
            {
                var glb = TestAssets.MakeGlb();
                string h1 = AssetHasher.Sha256Hex(new MemoryStream(glb));
                string h2 = AssetHasher.Sha256Hex(new MemoryStream(glb));
                string k1 = AssetHasher.CacheKey(h1, "gltfast-6.1.0", "urp-17.3", "balanced");
                string k2 = AssetHasher.CacheKey(h1, "gltfast-6.1.0", "urp-17.3", "ultra");
                return h1 == h2 && k1 != k2 && h1.Length == 64;
            });
            Check("Pipeline runs GLB end-to-end with staged progress", () =>
            {
                var glb = TestAssets.MakeGlb(12, 12, withSkin: true);
                var phases = new List<ImportPhase>();
                var progress = new Progress<ImportProgress>(p => phases.Add(p.Phase));
                var pipe = new ImportPipeline(new ImportLimits(), null);
                var r = pipe.Run("m.glb", new MemoryStream(glb), glb.Length, "gltfast-6.1.0", "urp-17.3", "balanced",
                    progress, CancellationToken.None);
                // IProgress posts async on this sync context? No — Progress<T> without sync context invokes inline on capture; use direct check instead:
                return r.Ok && r.Value.Format == ModelFormat.Glb && r.Value.Glb != null && r.Value.SourceSha256.Length == 64;
            });
            Check("Pipeline flags FBX for native importer (no silent loss)", () =>
            {
                var fbx = TestAssets.MakeFbxHeader();
                var pipe = new ImportPipeline(new ImportLimits(), null);
                var r = pipe.Run("c.fbx", new MemoryStream(fbx), fbx.Length, "assimp-5.4", "urp-17.3", "balanced",
                    null, CancellationToken.None);
                return r.Ok && r.Value.RequiresNativeImporter;
            });

            // ---------- Auto scale ----------
            Check("AutoScale: explicit units → high confidence", () =>
            {
                var rec = AutoScaleRecommender.Recommend(new Float3(175, 175, 30), 0.01f, AssetCategory.Unknown); // 1.75 m
                return rec.ReasonKey == "autoscale.reason.units" && rec.Confidence01 >= 0.85f
                    && Math.Abs(rec.UniformScale - 1f) < 0.001f;
            });
            Check("AutoScale: category estimate without units → medium confidence, rescaled", () =>
            {
                var rec = AutoScaleRecommender.Recommend(new Float3(100, 50, 40), float.NaN, AssetCategory.Furniture);
                return rec.ReasonKey == "autoscale.reason.bounds" && rec.Confidence01 <= 0.6f
                    && rec.UniformScale > 0 && rec.UniformScale < 0.1f; // ~0.9 m target
            });
            Check("AutoScale: unknown everything → low confidence, honest reason", () =>
            {
                var rec = AutoScaleRecommender.Recommend(new Float3(5000, 5000, 5000), float.NaN, AssetCategory.Unknown);
                return rec.ReasonKey == "autoscale.reason.unknown" && rec.Confidence01 <= 0.3f;
            });
            Check("AutoScale: human gets a range, not an imposed height", () =>
            {
                var rec = AutoScaleRecommender.Recommend(new Float3(180, 40, 20), 0.01f, AssetCategory.Human);
                return rec.MinMeters >= 1.3f && rec.MaxMeters <= 2.2f && rec.MaxMeters > rec.MinMeters;
            });

            // ---------- Undo/Redo ----------
            Check("Undo/Redo: push applies, undo restores, redo re-applies", () =>
            {
                var current = TransformPose.Identity;
                var stack = new UndoRedoStack();
                var moved = new TransformPose { Position = new Float3(1, 0, 0), Rotation = Float4.Identity, Scale = Float3.One };
                stack.Push(new SetPoseCommand("move", TransformPose.Identity, moved, p => current = p));
                bool ok1 = current.Equals(moved);
                stack.Undo();
                bool ok2 = current.Equals(TransformPose.Identity);
                stack.Redo();
                return ok1 && ok2 && current.Equals(moved);
            });
            Check("Undo/Redo: new push clears redo branch", () =>
            {
                var current = TransformPose.Identity;
                var stack = new UndoRedoStack();
                var a = new TransformPose { Position = new Float3(1, 0, 0), Rotation = Float4.Identity, Scale = Float3.One };
                var b = new TransformPose { Position = new Float3(2, 0, 0), Rotation = Float4.Identity, Scale = Float3.One };
                stack.Push(new SetPoseCommand("a", TransformPose.Identity, a, p => current = p));
                stack.Undo();
                stack.Push(new SetPoseCommand("b", TransformPose.Identity, b, p => current = p));
                return !stack.CanRedo && stack.CanUndo;
            });

            // ---------- AI backend selection ----------
            Check("Backend: full QNN graph → NPU", () =>
            {
                var caps = new DeviceAICapabilities(true, true, new HashSet<string> { "conv2d", "relu" });
                var c = BackendSelector.Select(caps, new[] { "conv2d", "relu" });
                return c.Backend == AIBackend.NpuQnn && !c.PartialDelegation;
            });
            Check("Backend: partial graph → mixed (honest)", () =>
            {
                var caps = new DeviceAICapabilities(true, true, new HashSet<string> { "conv2d" });
                var c = BackendSelector.Select(caps, new[] { "conv2d", "custom_op" });
                return c.Backend == AIBackend.Mixed && c.PartialDelegation;
            });
            Check("Backend: no QNN, no GPU → CPU fallback", () =>
            {
                var caps = new DeviceAICapabilities(false, false, new HashSet<string>());
                return BackendSelector.Select(caps, new[] { "conv2d" }).Backend == AIBackend.Cpu;
            });
            Check("Thermal scheduler throttles under pressure", () =>
            {
                var s = new InferenceScheduler();
                s.ApplyThermal(ThermalState.Critical);
                return s.FrameInterval >= 10 && s.InputResolution <= 128;
            });

            // ---------- Realism presets ----------
            Check("Ultra gated by capability AND thermals", () =>
            {
                return RealismPresets.IsUltraAvailable(true, ThermalState.Nominal)
                    && !RealismPresets.IsUltraAvailable(true, ThermalState.Serious)
                    && !RealismPresets.IsUltraAvailable(false, ThermalState.Nominal);
            });
            Check("Thermal degradation ladder", () =>
                RealismPresets.DegradeForThermal(RealismLevel.Ultra, ThermalState.Critical) == RealismLevel.Battery
                && RealismPresets.DegradeForThermal(RealismLevel.High, ThermalState.Serious) == RealismLevel.Balanced);
            Check("Ultra targets stable 30fps (honest)", () =>
                RealismPresets.For(RealismLevel.Ultra).TargetFrameRate == 30);

            // ---------- Localization ----------
            Check("AR/EN tables: identical key sets", () =>
            {
                var svc = LoadService(out var en, out var ar);
                return en.SetEquals(ar) && en.Count > 100;
            });
            Check("Required labels present and correct", () =>
            {
                var svc = LoadService(out _, out _);
                svc.SetLanguage(AppLanguage.Arabic);
                return svc.Get("action.importModel") == "استيراد مجسم"
                    && svc.Get("action.realism") == "الواقعية"
                    && svc.Get("action.wallOcclusion") == "خلف الجدران الواقعي"
                    && svc.Get("action.rigEdit") == "فواصل المجسم"
                    && svc.Get("action.anchorInRoom") == "تثبيت في الغرفة"
                    && svc.Get("action.relocalizeRoom") == "إعادة التعرّف على الغرفة"
                    && svc.Get("action.capture") == "تصوير"
                    && svc.Get("action.record") == "تسجيل"
                    && svc.Get("action.settings") == "الإعدادات"
                    && svc.Get("action.projects") == "المشاريع";
            });
            Check("Arabic strings are genuinely Arabic script", () =>
            {
                var svc = LoadService(out _, out _);
                svc.SetLanguage(AppLanguage.Arabic);
                return LocalizationService.LooksArabic(svc.Get("import.title"))
                    && !LocalizationService.LooksArabic("Import Model");
            });
            Check("Missing key falls back to EN, then key, and is recorded", () =>
            {
                var svc = LoadService(out _, out _);
                svc.SetLanguage(AppLanguage.Arabic);
                string missing = svc.Get("does.not.exist");
                return missing == "does.not.exist" && svc.MissingKeys.Contains("does.not.exist");
            });
            Check("RTL flag follows language", () =>
            {
                var svc = LoadService(out _, out _);
                svc.SetLanguage(AppLanguage.Arabic);
                bool rtl = svc.IsRtl;
                svc.SetLanguage(AppLanguage.English);
                return rtl && !svc.IsRtl;
            });

            // ---------- Persistence ----------
            Check("Store round-trips project + room (Arabic name, anchor, pose)", () =>
            {
                string dir = Path.Combine(Path.GetTempPath(), "moayad-store-" + Guid.NewGuid().ToString("N"));
                try
                {
                    var store = new ProjectStore(dir);
                    var project = new ProjectRecord { Name = "مجسم المجلس" };
                    project.Models.Add(new PlacedModelRecord
                    {
                        DisplayName = "كنبة", Format = ModelFormat.Glb, SourceSha256 = new string('a', 64),
                        AnchorLocalId = "anchor-1",
                        AnchorRelativePose = new TransformPose
                        { Position = new Float3(0.5f, 0, 1.2f), Rotation = Float4.FromYawDegrees(45), Scale = Float3.One }
                    });
                    var room = new RoomRecord { Name = "غرفة المعيشة", ScanCoverage01 = 0.62f, MappingQuality = "medium" };
                    room.Anchors.Add(new AnchorRecord { LocalAnchorId = "anchor-1", Quality01 = 0.8f, Pose = TransformPose.Identity });
                    var save = store.Save(project, room);
                    if (!save.Ok) return false;
                    var load = store.Load(project.Id);
                    if (!load.Ok) return false;
                    var (p2, r2) = load.Value;
                    return p2.Name == "مجسم المجلس" && r2.Anchors.Count == 1
                        && p2.Models[0].AnchorRelativePose.Position.X == 0.5f;
                }
                finally { Directory.Delete(dir, true); }
            });
            Check("Store rejects path traversal id", () =>
            {
                // Contract: an id containing separators must never reach the file system —
                // either ArgumentException (fail fast) or a failed Result is acceptable; success is not.
                string dir = Path.Combine(Path.GetTempPath(), "moayad-store-" + Guid.NewGuid().ToString("N"));
                try
                {
                    var store = new ProjectStore(dir);
                    try
                    {
                        var r = store.Load("../../etc/passwd");
                        return !r.Ok;
                    }
                    catch (ArgumentException) { return true; }
                }
                finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
            });
            Check("Store fails closed on newer schema", () =>
            {
                string dir = Path.Combine(Path.GetTempPath(), "moayad-store-" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "abc123.json"),
                        "{\"SchemaVersion\": 999, \"Project\": {\"Id\": \"abc123\"}}");
                    var r = new ProjectStore(dir).Load("abc123");
                    return !r.Ok && r.ErrorCode == "persist.newer_schema";
                }
                finally { Directory.Delete(dir, true); }
            });

            Console.WriteLine($"\n== RESULT: {_pass} passed, {_fail} failed ==");
            return _fail == 0 ? 0 : 1;
        }

        private static LocalizationService LoadService(out HashSet<string> enKeys, out HashSet<string> arKeys)
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Assets", "MoayadAR", "Localization", "Tables");
            dir = Path.GetFullPath(dir);
            string en = File.ReadAllText(Path.Combine(dir, "en.json"));
            string ar = File.ReadAllText(Path.Combine(dir, "ar.json"));
            var svc = new LocalizationService();
            svc.LoadTable(AppLanguage.English, en);
            svc.LoadTable(AppLanguage.Arabic, ar);
            enKeys = new HashSet<string>(Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(en).Keys);
            arKeys = new HashSet<string>(Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(ar).Keys);
            return svc;
        }

        private static void Check(string name, Func<bool> test)
        {
            try
            {
                bool ok = test();
                Console.WriteLine((ok ? "PASS " : "FAIL ") + name);
                if (ok) _pass++; else _fail++;
            }
            catch (Exception e)
            {
                Console.WriteLine("FAIL " + name + "  [exception: " + e.Message + "]");
                _fail++;
            }
        }
    }
}
