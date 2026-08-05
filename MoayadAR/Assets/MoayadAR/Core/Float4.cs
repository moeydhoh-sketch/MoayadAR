using System;

namespace MoayadAR.Core
{
    /// <summary>Quaternion (x,y,z,w), engine-agnostic.</summary>
    [Serializable]
    public struct Float4 : IEquatable<Float4>
    {
        public float X, Y, Z, W;
        public Float4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
        public static readonly Float4 Identity = new Float4(0, 0, 0, 1);
        public static Float4 FromYawDegrees(float yawDeg)
        {
            float half = yawDeg * (float)(Math.PI / 360.0);
            return new Float4(0, (float)Math.Sin(half), 0, (float)Math.Cos(half));
        }
        public Float4 Normalized()
        {
            float m = (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
            return m < 1e-8f ? Identity : new Float4(X / m, Y / m, Z / m, W / m);
        }
        public bool Equals(Float4 o) => X.Equals(o.X) && Y.Equals(o.Y) && Z.Equals(o.Z) && W.Equals(o.W);
        public override bool Equals(object o) => o is Float4 f && Equals(f);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    }
}
