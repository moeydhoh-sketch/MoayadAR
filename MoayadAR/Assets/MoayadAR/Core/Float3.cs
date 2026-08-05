using System;

namespace MoayadAR.Core
{
    /// <summary>Engine-agnostic float vector. Unity adapters convert to/from Vector3.</summary>
    [Serializable]
    public struct Float3 : IEquatable<Float3>
    {
        public float X, Y, Z;
        public Float3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static readonly Float3 Zero = new Float3(0, 0, 0);
        public static readonly Float3 One = new Float3(1, 1, 1);
        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public float MaxComponent => Math.Max(X, Math.Max(Y, Z));
        public float MinComponent => Math.Min(X, Math.Min(Y, Z));
        public static Float3 operator +(Float3 a, Float3 b) => new Float3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Float3 operator -(Float3 a, Float3 b) => new Float3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Float3 operator *(Float3 a, float s) => new Float3(a.X * s, a.Y * s, a.Z * s);
        public bool Equals(Float3 o) => X.Equals(o.X) && Y.Equals(o.Y) && Z.Equals(o.Z);
        public override bool Equals(object o) => o is Float3 f && Equals(f);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}
