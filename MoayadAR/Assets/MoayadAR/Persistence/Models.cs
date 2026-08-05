using System;
using System.Collections.Generic;
using MoayadAR.Core;

namespace MoayadAR.Persistence
{
    public sealed class ProjectRecord
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Untitled";
        public string RoomId;
        public string ThumbnailRelativePath;
        public bool Archived;
        public DateTime CreatedUtc = DateTime.UtcNow;
        public DateTime ModifiedUtc = DateTime.UtcNow;
        public List<PlacedModelRecord> Models = new List<PlacedModelRecord>();
    }

    public sealed class RoomRecord
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Room";
        public float ScanCoverage01;          // honest estimate, never 1.0 by default
        public string MappingQuality;         // "low" | "medium" | "high"
        public List<AnchorRecord> Anchors = new List<AnchorRecord>();
        public List<WallSegment> Walls = new List<WallSegment>();
        public DateTime LastScanUtc;
    }

    public sealed class WallSegment
    {
        public Float3 Center;
        public Float3 Normal;
        public float WidthMeters;
        public float HeightMeters;
        public float Confidence01;
    }

    public sealed class AnchorRecord
    {
        public string LocalAnchorId;          // ARCore persistent-anchor id when supported
        public TransformPose Pose;
        public float Quality01;
        public DateTime CreatedUtc;
        public DateTime LastResolvedUtc;
        public string CoordinateNotes;        // e.g. tracking state at save time
    }

    public sealed class PlacedModelRecord
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string DisplayName;
        public string SourceFileName;
        public ModelFormat Format;
        public string SourceSha256;
        public string CacheKey;               // sha256 + importer + pipeline + quality preset
        public string AnchorLocalId;          // references AnchorRecord.LocalAnchorId
        public TransformPose AnchorRelativePose;
        public bool Hidden;
        public bool LockPosition, LockRotation, LockScale;
        public List<PoseSnapshot> Poses = new List<PoseSnapshot>();
    }

    public sealed class PoseSnapshot
    {
        public string Name;
        public DateTime SavedUtc = DateTime.UtcNow;
        public int SchemaVersion = 1;
        public Dictionary<string, TransformPose> BoneLocalPoses = new Dictionary<string, TransformPose>();
    }

    public sealed class AppSettings
    {
        public string Language = "ar";        // Arabic default per product identity
        public string RealismLevel = "Balanced";
        public bool HeavyAssetMode;
        public bool HandInteractionEnabled;
        public bool DiagnosticsEnabled;
        public bool CloudAnchorsOptIn;        // explicit opt-in only, no keys stored
        public bool ReduceMotion;             // mirrors OS preference
    }
}
