using System;

namespace MoayadAR.Core
{
    /// <summary>Anchor-relative local pose. This — never a camera-relative pose — is what gets persisted.</summary>
    [Serializable]
    public struct TransformPose : IEquatable<TransformPose>
    {
        public Float3 Position;
        public Float4 Rotation;
        public Float3 Scale;
        public static TransformPose Identity => new TransformPose
        { Position = Float3.Zero, Rotation = Float4.Identity, Scale = Float3.One };
        public bool Equals(TransformPose o) => Position.Equals(o.Position) && Rotation.Equals(o.Rotation) && Scale.Equals(o.Scale);
        public override bool Equals(object o) => o is TransformPose t && Equals(t);
        public override int GetHashCode() => HashCode.Combine(Position, Rotation, Scale);
    }
}
