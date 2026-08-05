using System;
using System.IO;
using System.Text;
using MoayadAR.Core;

namespace MoayadAR.Import
{
    /// <summary>
    /// Extension + magic-byte validation. An .obj renamed to .glb fails here, before any parser runs.
    /// GLB: "glTF". FBX binary: "Kaydara FBX Binary". glTF JSON / OBJ: text sniffed with a size cap.
    /// </summary>
    public static class FileValidator
    {
        private static readonly byte[] GlbMagic = Encoding.ASCII.GetBytes("glTF");
        private static readonly byte[] FbxMagic = Encoding.ASCII.GetBytes("Kaydara FBX Binary");

        public static ModelFormat FormatFromExtension(string fileName)
        {
            string ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            switch (ext)
            {
                case ".glb": return ModelFormat.Glb;
                case ".gltf": return ModelFormat.Gltf;
                case ".fbx": return ModelFormat.Fbx;
                case ".obj": return ModelFormat.Obj;
                default: return ModelFormat.Unknown;
            }
        }

        public static Result<ModelFormat> Validate(string fileName, Stream head, long fileBytes, ImportLimits limits)
        {
            var expected = FormatFromExtension(fileName);
            if (expected == ModelFormat.Unknown)
                return Result<ModelFormat>.Fail("import.unsupported_format", "error.unsupportedFormat", fileName);
            if (fileBytes > limits.MaxFileBytes)
                return Result<ModelFormat>.Fail("import.too_large", "error.fileTooLarge",
                    (fileBytes / (1024 * 1024)).ToString());

            byte[] buf = new byte[20];
            int n = ReadUpTo(head, buf, buf.Length);

            switch (expected)
            {
                case ModelFormat.Glb:
                    if (n < 4 || !StartsWith(buf, GlbMagic))
                        return Result<ModelFormat>.Fail("import.magic_mismatch", "error.magicMismatch", "glb magic");
                    break;
                case ModelFormat.Fbx:
                    // Binary FBX has the magic; ASCII FBX is not accepted by the native path.
                    if (n < FbxMagic.Length || !StartsWith(buf, FbxMagic))
                        return Result<ModelFormat>.Fail("import.magic_mismatch", "error.magicMismatch", "fbx magic (ascii fbx unsupported)");
                    break;
                case ModelFormat.Gltf:
                case ModelFormat.Obj:
                    if (n == 0 || LooksBinary(buf, n))
                        return Result<ModelFormat>.Fail("import.magic_mismatch", "error.magicMismatch", "text-format file looks binary");
                    break;
            }
            return Result<ModelFormat>.Success(expected);
        }

        private static int ReadUpTo(Stream s, byte[] buf, int count)
        {
            int total = 0;
            while (total < count)
            {
                int r = s.Read(buf, total, count - total);
                if (r <= 0) break;
                total += r;
            }
            return total;
        }

        private static bool StartsWith(byte[] buf, byte[] magic)
        {
            for (int i = 0; i < magic.Length; i++) if (buf[i] != magic[i]) return false;
            return true;
        }

        private static bool LooksBinary(byte[] buf, int n)
        {
            int check = Math.Min(n, 16);
            for (int i = 0; i < check; i++)
                if (buf[i] == 0) return true;
            return false;
        }
    }
}
