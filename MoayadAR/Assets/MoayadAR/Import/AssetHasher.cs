using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MoayadAR.Import
{
    public static class AssetHasher
    {
        /// <summary>Streaming SHA-256 — constant memory regardless of file size.</summary>
        public static string Sha256Hex(Stream s)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(s);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>Cache key: content + importer + render pipeline + quality preset. Any change invalidates honestly.</summary>
        public static string CacheKey(string sha256, string importerVersion, string pipelineVersion, string qualityPreset)
        {
            string raw = string.Join("|", sha256 ?? "", importerVersion ?? "", pipelineVersion ?? "", qualityPreset ?? "");
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder(32);
                for (int i = 0; i < 16; i++) sb.Append(hash[i].ToString("x2")); // 128-bit cache key is ample
                return sb.ToString();
            }
        }
    }
}
