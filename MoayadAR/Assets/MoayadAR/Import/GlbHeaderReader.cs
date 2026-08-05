using System;
using System.IO;
using System.Text;
using MoayadAR.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoayadAR.Import
{
    /// <summary>
    /// Real GLB container reader: validates header, reads the JSON chunk, and extracts statistics
    /// (meshes, accessors, skins, animations, materials, textures) used by the analysis card.
    /// Full geometry/material instantiation on device is delegated to Unity glTFast 6.1.0 —
    /// this reader never replaces it; it powers validation, budgets, and the analysis report.
    /// </summary>
    public static class GlbHeaderReader
    {
        public sealed class GlbInfo
        {
            public int GlbVersion;
            public uint TotalLength;
            public string Generator;          // exporter, when declared — feeds Auto Scale
            public int MeshCount, NodeCount, MaterialCount, TextureCount, SkinCount, AnimationCount;
            public int SceneCount, CameraCount, LightCount;
            public long VertexCount, TriangleCount;
            public bool HasMorphTargets;
            public bool DeclaresMeshesQuantized;
            public string[] AnimationNames = Array.Empty<string>();
            public int BoneCount;             // max joints across skins
            public bool UsesDraco, UsesKtx2, UsesMeshopt;
            public JObject RawJson;           // retained for the analyzer (units hints, extras)
        }

        public static Result<GlbInfo> Read(Stream s, ImportLimits limits)
        {
            try
            {
                using (var br = new BinaryReader(s, Encoding.UTF8, leaveOpen: true))
                {
                    byte[] magic = br.ReadBytes(4);
                    if (magic.Length < 4 || magic[0] != 'g' || magic[1] != 'l' || magic[2] != 'T' || magic[3] != 'F')
                        return Result<GlbInfo>.Fail("import.magic_mismatch", "error.magicMismatch", "glb magic");
                    int version = br.ReadInt32();
                    if (version != 2)
                        return Result<GlbInfo>.Fail("import.glb_version", "error.unsupportedFormat", $"glb version {version}");
                    uint totalLength = br.ReadUInt32();
                    if (totalLength > limits.MaxFileBytes)
                        return Result<GlbInfo>.Fail("import.too_large", "error.fileTooLarge", totalLength.ToString());
                    // Truncation guard: the declared container length must not exceed the actual bytes.
                    if (s.CanSeek && s.Length < totalLength)
                        return Result<GlbInfo>.Fail("import.truncated", "error.magicMismatch",
                            $"declared {totalLength} > actual {s.Length}");

                    uint jsonLen = br.ReadUInt32();
                    if (jsonLen == 0 || jsonLen > 256 * 1024 * 1024)
                        return Result<GlbInfo>.Fail("import.corrupt_chunk", "error.magicMismatch", $"json chunk {jsonLen}");
                    byte[] type = br.ReadBytes(4);
                    if (type.Length < 4 || type[0] != 'J' || type[1] != 'S' || type[2] != 'O' || type[3] != 'N')
                        return Result<GlbInfo>.Fail("import.corrupt_chunk", "error.magicMismatch", "first chunk not JSON");
                    byte[] jsonBytes = br.ReadBytes((int)jsonLen);
                    if (jsonBytes.Length < jsonLen)
                        return Result<GlbInfo>.Fail("import.truncated", "error.magicMismatch", "truncated glb");

                    var json = JObject.Parse(Encoding.UTF8.GetString(jsonBytes));
                    var info = new GlbInfo { GlbVersion = version, TotalLength = totalLength, RawJson = json };
                    info.Generator = json["asset"]?["generator"]?.ToString();
                    info.MeshCount = Count(json, "meshes");
                    info.NodeCount = Count(json, "nodes");
                    info.MaterialCount = Count(json, "materials");
                    info.TextureCount = Count(json, "textures");
                    info.SkinCount = Count(json, "skins");
                    info.AnimationCount = Count(json, "animations");
                    info.SceneCount = Count(json, "scenes");
                    info.CameraCount = Count(json, "cameras");

                    if (json["animations"] is JArray anims)
                    {
                        var names = new string[anims.Count];
                        for (int i = 0; i < anims.Count; i++) names[i] = anims[i]["name"]?.ToString() ?? $"clip_{i}";
                        info.AnimationNames = names;
                    }

                    // Vertex/triangle totals via accessors referenced by mesh primitives.
                    var accessors = json["accessors"] as JArray;
                    if (json["meshes"] is JArray meshes && accessors != null)
                    {
                        foreach (var mesh in meshes)
                        {
                            if (mesh["primitives"] is JArray prims)
                            {
                                foreach (var prim in prims)
                                {
                                    long verts = AccessorCount(accessors, prim["attributes"]?["POSITION"]);
                                    info.VertexCount += verts;
                                    var indices = prim["indices"];
                                    if (indices != null) info.TriangleCount += AccessorCount(accessors, indices) / 3;
                                    else info.TriangleCount += verts / 3;
                                    if (prim["targets"] is JArray tg && tg.Count > 0) info.HasMorphTargets = true;
                                }
                            }
                            if (mesh["weights"] != null) info.HasMorphTargets = true;
                        }
                    }

                    if (json["skins"] is JArray skins)
                        foreach (var skin in skins)
                            info.BoneCount = Math.Max(info.BoneCount, (skin["joints"] as JArray)?.Count ?? 0);

                    // Extension usage that affects import cost / supported path.
                    string ext = json["extensionsUsed"]?.ToString() ?? "";
                    info.UsesDraco = ext.Contains("KHR_draco_mesh_compression");
                    info.UsesKtx2 = ext.Contains("KHR_texture_basisu");
                    info.UsesMeshopt = ext.Contains("EXT_meshopt_compression");
                    info.DeclaresMeshesQuantized = ext.Contains("KHR_mesh_quantization");

                    if (info.NodeCount > limits.MaxNodes)
                        return Result<GlbInfo>.Fail("import.too_many_nodes", "error.tooManyTriangles", info.NodeCount.ToString());
                    return Result<GlbInfo>.Success(info);
                }
            }
            catch (JsonException e)
            {
                return Result<GlbInfo>.Fail("import.bad_json", "error.magicMismatch", e.Message);
            }
            catch (Exception e)
            {
                return Result<GlbInfo>.Fail("import.glb_read", "error.magicMismatch", e.Message);
            }
        }

        private static int Count(JObject json, string name) => (json[name] as JArray)?.Count ?? 0;

        private static long AccessorCount(JArray accessors, JToken indexToken)
        {
            if (indexToken == null) return 0;
            int idx = indexToken.Value<int>();
            if (idx < 0 || idx >= accessors.Count) return 0;
            return accessors[idx]["count"]?.Value<long>() ?? 0;
        }
    }
}
