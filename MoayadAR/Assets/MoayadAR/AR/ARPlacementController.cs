using System.Collections.Generic;
using MoayadAR.Core;
using MoayadAR.Persistence;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MoayadAR.AR
{
    /// <summary>
    /// Placement: content is parented to an ARAnchor, never to the camera. During user manipulation
    /// automatic anchor corrections are suspended; the final anchor-relative pose is committed on release.
    /// DEVICE-PENDING: requires Unity + ARCore device; not executed in the authoring environment.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    [RequireComponent(typeof(ARAnchorManager))]
    public sealed class ARPlacementController : MonoBehaviour
    {
        [SerializeField] private ARRaycastManager _raycastManager;
        [SerializeField] private ARAnchorManager _anchorManager;
        [SerializeField] private Camera _arCamera;

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
        private readonly Dictionary<string, ARAnchor> _anchorsByLocalId = new Dictionary<string, ARAnchor>();

        private bool _correctionsSuspended;
        private Transform _manipulatedContent;

        public bool TryPlaceAtScreenPoint(Vector2 screenPoint, GameObject content, AnchorRecord record)
        {
            if (!_raycastManager.Raycast(screenPoint, Hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
                return false;

            Pose hitPose = Hits[0].pose;
            var anchor = _anchorManager.AttachAnchor(
                Hits[0].trackable as ARPlane, hitPose);
            if (anchor == null) return false;

            anchor.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            content.transform.SetParent(anchor.transform, worldPositionStays: false);
            content.transform.localPosition = Vector3.zero;
            content.transform.localRotation = Quaternion.identity;

            string localId = string.IsNullOrEmpty(record.LocalAnchorId)
                ? System.Guid.NewGuid().ToString("N") : record.LocalAnchorId;
            record.LocalAnchorId = localId;
            record.Pose = ToPose(anchor.transform);
            record.CreatedUtc = System.DateTime.UtcNow;
            _anchorsByLocalId[localId] = anchor;
            return true;
        }

        /// <summary>Call when a drag/pinch/twist starts: freezes logical content against anchor corrections.</summary>
        public void BeginManipulation(Transform content)
        {
            _manipulatedContent = content;
            _correctionsSuspended = true;
        }

        /// <summary>Call on release: commits the anchor-relative pose for persistence.</summary>
        public TransformPose EndManipulation()
        {
            _correctionsSuspended = false;
            if (_manipulatedContent == null) return TransformPose.Identity;
            var pose = new TransformPose
            {
                Position = ToFloat3(_manipulatedContent.localPosition),
                Rotation = new Float4(_manipulatedContent.localRotation.x, _manipulatedContent.localRotation.y,
                    _manipulatedContent.localRotation.z, _manipulatedContent.localRotation.w),
                Scale = ToFloat3(_manipulatedContent.localScale)
            };
            _manipulatedContent = null;
            return pose;
        }

        public bool CorrectionsSuspended => _correctionsSuspended;

        public bool TryGetAnchor(string localId, out ARAnchor anchor) =>
            _anchorsByLocalId.TryGetValue(localId, out anchor);

        private static TransformPose ToPose(Transform t) => new TransformPose
        {
            Position = ToFloat3(t.localPosition),
            Rotation = new Float4(t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w),
            Scale = ToFloat3(t.localScale)
        };

        private static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);

        private void Reset()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _anchorManager = GetComponent<ARAnchorManager>();
            _arCamera = Camera.main;
        }
    }
}
