using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace MoayadAR.TestsNet
{
    /// <summary>Builds real (minimal but spec-valid) GLB/OBJ bytes so parsers are tested against genuine containers.</summary>
    public static class TestAssets
    {
        public static byte[] MakeGlb(int vertexCount = 3, int triangleIndices = 3, bool withSkin = false,
            bool withAnimation = false, string generator = "MoayadAR-Test")
        {
            var json = new JObject
            {
                ["asset"] = new JObject { ["version"] = "2.0", ["generator"] = generator },
                ["scenes"] = new JArray(new JObject { ["nodes"] = new JArray(0) }),
                ["nodes"] = new JArray(new JObject { ["mesh"] = 0 }),
                ["meshes"] = new JArray(new JObject
                {
                    ["primitives"] = new JArray(new JObject
                    {
                        ["attributes"] = new JObject { ["POSITION"] = 0 },
                        ["indices"] = 1
                    })
                }),
                ["accessors"] = new JArray(
                    new JObject { ["count"] = vertexCount, ["type"] = "VEC3", ["componentType"] = 5126 },
                    new JObject { ["count"] = triangleIndices, ["type"] = "SCALAR", ["componentType"] = 5123 }),
                ["materials"] = new JArray(new JObject { ["name"] = "mat0" }),
                ["textures"] = new JArray(new JObject { ["source"] = 0 })
            };
            if (withSkin)
                json["skins"] = new JArray(new JObject { ["joints"] = new JArray(0, 1, 2, 3, 4), ["name"] = "rig" });
            if (withAnimation)
                json["animations"] = new JArray(new JObject { ["name"] = "walk" }, new JObject { ["name"] = "idle" });

            byte[] jsonBytes = Pad4(Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None)), 0x20);
            byte[] bin = Pad4(new byte[vertexCount * 12], 0x00); // dummy BIN chunk

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Encoding.ASCII.GetBytes("glTF"));
                bw.Write(2); // version
                bw.Write(12 + 8 + jsonBytes.Length + 8 + bin.Length);
                bw.Write(jsonBytes.Length);
                bw.Write(Encoding.ASCII.GetBytes("JSON"));
                bw.Write(jsonBytes);
                bw.Write(bin.Length);
                bw.Write(Encoding.ASCII.GetBytes("BIN\0"));
                bw.Write(bin);
                bw.Flush();
                return ms.ToArray();
            }
        }

        public static byte[] MakeObj(int verts, int faces, bool withNormals = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# MoayadAR test asset");
            var rnd = new Random(42);
            for (int i = 0; i < verts; i++)
                sb.AppendLine(FormattableString.Invariant($"v {rnd.NextDouble() * 2 - 1:0.###} {rnd.NextDouble():0.###} {rnd.NextDouble() * 2 - 1:0.###}"));
            if (withNormals) sb.AppendLine("vn 0 1 0");
            sb.AppendLine("vt 0 0");
            for (int i = 0; i < faces; i++)
            {
                int a = 1 + (i * 3) % verts, b = 1 + (i * 3 + 1) % verts, c = 1 + (i * 3 + 2) % verts;
                sb.AppendLine(FormattableString.Invariant($"f {a}/1/1 {b}/1/1 {c}/1/1"));
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static byte[] MakeFbxHeader()
        {
            // Real binary FBX magic: "Kaydara FBX Binary  \x00" + version
            var bytes = new byte[27];
            var magic = Encoding.ASCII.GetBytes("Kaydara FBX Binary  ");
            Array.Copy(magic, bytes, magic.Length);
            bytes[20] = 0x00; bytes[21] = 0x1A; bytes[22] = 0x00;
            BitConverter.GetBytes(7400).CopyTo(bytes, 23); // FBX 7.4
            return bytes;
        }

        private static byte[] Pad4(byte[] data, byte pad)
        {
            int rem = data.Length % 4;
            if (rem == 0) return data;
            var padded = new byte[data.Length + (4 - rem)];
            Array.Copy(data, padded, data.Length);
            for (int i = data.Length; i < padded.Length; i++) padded[i] = pad;
            return padded;
        }
    }
}
