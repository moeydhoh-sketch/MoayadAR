using System.Collections.Generic;
using MoayadAR.Core;
using MoayadAR.Persistence;
using UnityEngine;

namespace MoayadAR.Rigging
{
    /// <summary>
    /// Pose editing on a detected skeleton (master prompt §12). FK rotation per bone, CCD-based
    /// two-bone-plus IK when the chain allows it, optional human joint limits, mirror, snapshots.
    /// Poses live on an override layer conceptually: source AnimationClips are never mutated;
    /// snapshots persist as versioned local transform data in the project record.
    /// EDITOR-PENDING.
    /// </summary>
    public sealed class RigEditController : MonoBehaviour
    {
        [SerializeField] private bool _respectHumanJointLimits = true;
        [SerializeField] private int _ikIterations = 10;
        [SerializeField] private float _ikTolerance = 0.005f;

        private readonly Dictionary<string, Transform> _bonesByName = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Quaternion> _bindLocalRotations = new Dictionary<string, Quaternion>();

        public Transform SelectedBone { get; private set; }
        public bool HasRig => _bonesByName.Count > 0;

        public void BindSkeleton(Transform root)
        {
            _bonesByName.Clear();
            _bindLocalRotations.Clear();
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (string.IsNullOrEmpty(t.name)) continue;
                _bonesByName[t.name] = t;
                _bindLocalRotations[t.name] = t.localRotation;
            }
        }

        public IEnumerable<string> BoneNames => _bonesByName.Keys;

        public bool SelectBone(string name)
        {
            if (_bonesByName.TryGetValue(name, out var t)) { SelectedBone = t; return true; }
            return false;
        }

        /// <summary>FK: rotate the selected bone in local axes with optional joint clamping.</summary>
        public void RotateSelectedLocal(Vector3 eulerDelta)
        {
            if (SelectedBone == null) return;
            Quaternion delta = Quaternion.Euler(eulerDelta);
            Quaternion target = SelectedBone.localRotation * delta;
            if (_respectHumanJointLimits && LooksLikeHumanJoint(SelectedBone.name))
                target = ClampHumanJoint(SelectedBone.name, target);
            SelectedBone.localRotation = target;
        }

        /// <summary>CCD IK from end-effector toward a world target; only when the chain is valid.</summary>
        public bool SolveIk(Transform endEffector, Vector3 worldTarget, int chainLength)
        {
            if (endEffector == null || chainLength < 2) return false;
            var chain = new List<Transform>();
            var cur = endEffector;
            for (int i = 0; i < chainLength && cur != null; i++) { chain.Add(cur); cur = cur.parent; }
            if (chain.Count < 2) return false;

            for (int iter = 0; iter < _ikIterations; iter++)
            {
                if ((endEffector.position - worldTarget).sqrMagnitude < _ikTolerance * _ikTolerance) break;
                for (int i = chain.Count - 2; i >= 0; i--)
                {
                    Transform bone = chain[i];
                    Vector3 toEnd = endEffector.position - bone.position;
                    Vector3 toTarget = worldTarget - bone.position;
                    if (toEnd.sqrMagnitude < 1e-10f || toTarget.sqrMagnitude < 1e-10f) continue;
                    Quaternion rot = Quaternion.FromToRotation(toEnd, toTarget);
                    bone.rotation = rot * bone.rotation;
                }
            }
            return (endEffector.position - worldTarget).magnitude < _ikTolerance * 4f;
        }

        /// <summary>Mirror L/R bone rotations across the sagittal plane for symmetric rigs.</summary>
        public void MirrorPose()
        {
            foreach (var kv in _bonesByName)
            {
                string other = MirrorName(kv.Key);
                if (other == null || !_bonesByName.TryGetValue(other, out var otherT)) continue;
                (kv.Value.localRotation, otherT.localRotation) =
                    (MirrorQuat(otherT.localRotation), MirrorQuat(kv.Value.localRotation));
            }
        }

        public PoseSnapshot CapturePose(string name)
        {
            var snap = new PoseSnapshot { Name = name };
            foreach (var kv in _bonesByName)
            {
                var t = kv.Value;
                snap.BoneLocalPoses[kv.Key] = new TransformPose
                {
                    Position = new Float3(t.localPosition.x, t.localPosition.y, t.localPosition.z),
                    Rotation = new Float4(t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w),
                    Scale = new Float3(t.localScale.x, t.localScale.y, t.localScale.z)
                };
            }
            return snap;
        }

        public void ApplyPose(PoseSnapshot snap)
        {
            foreach (var kv in snap.BoneLocalPoses)
            {
                if (!_bonesByName.TryGetValue(kv.Key, out var t)) continue;
                var p = kv.Value;
                t.localPosition = new Vector3(p.Position.X, p.Position.Y, p.Position.Z);
                t.localRotation = new Quaternion(p.Rotation.X, p.Rotation.Y, p.Rotation.Z, p.Rotation.W);
                t.localScale = new Vector3(p.Scale.X, p.Scale.Y, p.Scale.Z);
            }
        }

        public void ResetPose()
        {
            foreach (var kv in _bindLocalRotations)
                if (_bonesByName.TryGetValue(kv.Key, out var t)) t.localRotation = kv.Value;
        }

        private static Quaternion MirrorQuat(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);

        private static string MirrorName(string name)
        {
            if (name.Contains("_L")) return name.Replace("_L", "_R");
            if (name.Contains("_R")) return name.Replace("_R", "_L");
            if (name.StartsWith("Left")) return "Right" + name.Substring(4);
            if (name.StartsWith("Right")) return "Left" + name.Substring(5);
            return null;
        }

        private static bool LooksLikeHumanJoint(string name) =>
            name.Contains("Elbow") || name.Contains("Knee") || name.Contains("Shoulder") || name.Contains("Hip");

        private static Quaternion ClampHumanJoint(string name, Quaternion localRot)
        {
            // Conservative single-axis clamp for hinge joints (elbow/knee): [0, 150°].
            Vector3 e = localRot.eulerAngles;
            if (name.Contains("Elbow") || name.Contains("Knee"))
            {
                float x = e.x > 180f ? e.x - 360f : e.x;
                x = Mathf.Clamp(x, 0f, 150f);
                e.x = x;
            }
            return Quaternion.Euler(e);
        }
    }
}
