using System.Collections.Generic;
using MoayadAR.Persistence;
using UnityEngine;

namespace MoayadAR.AR
{
    /// <summary>
    /// Realistic Behind-Walls Mode (master prompt §7). Uses the scanned WallSegment list from the
    /// RoomRecord — geometry derived from real vertical-plane/depth scans, never fabricated.
    /// When a wall plane separates camera and model, the model's renderers are masked by the wall
    /// (stencil/depth pass). When the scan is too thin, the mode reports "need more scanning"
    /// instead of pretending. DEVICE-PENDING.
    /// </summary>
    public sealed class BehindWallsMode : MonoBehaviour
    {
        [SerializeField] private Camera _arCamera;
        [SerializeField, Range(0f, 1f)] private float _minWallConfidence = 0.5f;
        [SerializeField] private float _minScanCoverage = 0.4f;

        public bool ModeEnabled { get; set; } = true;
        public bool ScanSufficient { get; private set; }

        private RoomRecord _room;
        private readonly List<Renderer> _roomRenderers = new List<Renderer>();

        private static readonly int WallMaskEnabledProp = Shader.PropertyToID("_WallMaskEnabled");

        public void BindRoom(RoomRecord room)
        {
            _room = room;
            ScanSufficient = room != null
                && room.ScanCoverage01 >= _minScanCoverage
                && room.Walls.Exists(w => w.Confidence01 >= _minWallConfidence);
        }

        public void RegisterModel(Renderer[] renderers)
        {
            _roomRenderers.Clear();
            _roomRenderers.AddRange(renderers);
        }

        private void LateUpdate()
        {
            if (!ModeEnabled || !ScanSufficient || _room == null || _arCamera == null) return;
            Vector3 camPos = _arCamera.transform.position;

            foreach (var r in _roomRenderers)
            {
                if (r == null) continue;
                bool blocked = false;
                Vector3 modelPos = r.bounds.center;
                foreach (var wall in _room.Walls)
                {
                    if (wall.Confidence01 < _minWallConfidence) continue;
                    if (SegmentCrossesWall(camPos, modelPos, wall)) { blocked = true; break; }
                }
                foreach (var mat in r.materials)
                    mat.SetFloat(WallMaskEnabledProp, blocked ? 1f : 0f);
            }
        }

        /// <summary>2D (top-down) segment/wall test: does the camera→model ray cross the wall's extent?</summary>
        public static bool SegmentCrossesWall(Vector3 a, Vector3 b, WallSegment wall)
        {
            Vector3 n = wall.Normal;
            float da = Dot2(a - wall.Center, n);
            float db = Dot2(b - wall.Center, n);
            if (da * db >= 0f) return false; // same side — no crossing

            float t = da / (da - db);
            Vector3 hit = a + (b - a) * t;
            Vector3 tangent = Vector3.Cross(Vector3.up, n).normalized;
            float along = Mathf.Abs(Dot2(hit - wall.Center, tangent));
            float heightOk = hit.y >= wall.Center.y - 0.05f && hit.y <= wall.Center.y + wall.HeightMeters;
            return along <= wall.WidthMeters * 0.5f && heightOk;
        }

        private static float Dot2(Vector3 v, Vector3 n) => v.x * n.x + v.z * n.z;
    }
}
