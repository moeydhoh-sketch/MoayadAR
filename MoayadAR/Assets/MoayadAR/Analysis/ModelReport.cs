using System;
using System.Collections.Generic;
using MoayadAR.Core;

namespace MoayadAR.Analysis
{
    public enum AssetCategory { Unknown, Human, Animal, Furniture, Vehicle, SmallObject, Building }

    /// <summary>Everything the analysis card shows. Counts come from the actual file, never assumed.</summary>
    public sealed class ModelReport
    {
        public string FileName;
        public ModelFormat Format;
        public long FileBytes;
        public TimeSpan ImportDuration;
        public bool CacheHit;

        public long VertexCount, TriangleCount;
        public int NodeCount, SubmeshCount, MaterialCount, TextureCount;

        public Float3 BoundsMin, BoundsMax;     // normalized meters after unit conversion
        public Float3 BoundsSize => BoundsMax - BoundsMin;
        public string SourceUnits = "unknown";
        public string UpAxis = "Y";
        public bool LeftHandedSource;
        public Float3 Pivot = Float3.Zero;

        public bool RigDetected;
        public string SkeletonRootName;
        public int BoneCount, SkinnedMeshCount, MorphTargetCount;
        public List<string> AnimationClips = new List<string>();
        public List<float> AnimationDurationsSec = new List<float>();

        public bool MissingNormals, MissingTangents, MissingTextures, InvalidSkinWeights;
        public bool SuspiciousScale, HighPoly;
        public List<string> Warnings = new List<string>(); // localization keys analysis.warning.*
    }
}
