using System.IO;
using MoayadAR.Analysis;
using MoayadAR.Core;
using MoayadAR.Import;
using MoayadAR.Interaction;
using NUnit.Framework;

namespace MoayadAR.Tests.EditMode
{
    /// <summary>
    /// Unity Test Runner mirrors of the platform-agnostic suite (the full suite runs in tests-net/
    /// on any machine with .NET 8 — see BUILD_STATUS.md for the 35/35 result from 2026-08-05).
    /// </summary>
    public sealed class CoreLogicEditModeTests
    {
        [Test]
        public void Obj_Never_Reports_Rig()
        {
            var report = new ModelReport { Format = ModelFormat.Obj };
            Assert.IsFalse(report.RigDetected, "OBJ must never report a rig (master prompt §3)");
        }

        [Test]
        public void AutoScale_UnknownMetadata_IsHonest()
        {
            var rec = AutoScaleRecommender.Recommend(new Float3(9999, 1, 1), float.NaN, AssetCategory.Unknown);
            Assert.LessOrEqual(rec.Confidence01, 0.3f);
            Assert.AreEqual("autoscale.reason.unknown", rec.ReasonKey);
        }

        [Test]
        public void UndoRedo_NewPush_ClearsRedo()
        {
            var current = TransformPose.Identity;
            var stack = new UndoRedoStack();
            var a = new TransformPose { Position = new Float3(1, 0, 0), Rotation = Float4.Identity, Scale = Float3.One };
            stack.Push(new SetPoseCommand("a", TransformPose.Identity, a, p => current = p));
            stack.Undo();
            stack.Push(new SetPoseCommand("b", TransformPose.Identity, a, p => current = p));
            Assert.IsFalse(stack.CanRedo);
        }

        [Test]
        public void Validator_Rejects_Renamed_Obj_As_Glb()
        {
            byte[] obj = System.Text.Encoding.UTF8.GetBytes("v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");
            var r = FileValidator.Validate("fake.glb", new MemoryStream(obj), obj.Length, new ImportLimits());
            Assert.IsFalse(r.Ok);
            Assert.AreEqual("import.magic_mismatch", r.ErrorCode);
        }
    }
}
