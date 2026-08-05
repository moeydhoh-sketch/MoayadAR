namespace MoayadAR.Import
{
    /// <summary>Configurable budgets. Heavy Asset mode raises these with an explicit warning.</summary>
    public sealed class ImportLimits
    {
        public long MaxFileBytes = 256L * 1024 * 1024;      // 256 MB
        public int MaxTriangles = 1_500_000;
        public int MaxVertices = 2_000_000;
        public int MaxTextures = 256;
        public int MaxTextureDimension = 8192;
        public int MaxMaterials = 512;
        public int MaxNodes = 65_536;
        public int MaxBones = 512;
        public int MaxAnimationClips = 128;
        public int MaxObjLineLength = 4096;
        public int MaxObjLines = 40_000_000;

        public static ImportLimits HeavyAsset() => new ImportLimits
        {
            MaxFileBytes = 1024L * 1024 * 1024,
            MaxTriangles = 8_000_000,
            MaxVertices = 10_000_000,
            MaxTextures = 512,
            MaxTextureDimension = 16384,
            MaxMaterials = 2048,
            MaxNodes = 262_144,
            MaxBones = 1024,
            MaxAnimationClips = 256
        };
    }
}
