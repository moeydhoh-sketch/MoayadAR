using System;
using System.Globalization;
using System.IO;
using MoayadAR.Core;

namespace MoayadAR.Import
{
    /// <summary>
    /// Streaming Wavefront OBJ statistics reader. OBJ carries no skeleton or animation data —
    /// callers must surface "No rig detected" rather than invent rig controls (master prompt §3).
    /// Guards: line-length cap, line-count cap, strict numeric parsing (no float overflow to Infinity).
    /// </summary>
    public static class ObjModelReader
    {
        public sealed class ObjInfo
        {
            public long VertexCount, NormalCount, TexCoordCount, FaceCount, TriangleEstimate;
            public int GroupCount, MaterialRefCount;
            public bool HasNormals, HasTexCoords;
            public Float3 BoundsMin, BoundsMax;
        }

        public static Result<ObjInfo> Read(Stream s, ImportLimits limits)
        {
            try
            {
                var info = new ObjInfo();
                float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
                bool anyV = false;

                using (var sr = new StreamReader(s, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
                           bufferSize: 64 * 1024, leaveOpen: true))
                {
                    string line;
                    long lineNo = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (++lineNo > limits.MaxObjLines)
                            return Result<ObjInfo>.Fail("import.obj_too_many_lines", "error.tooManyTriangles", lineNo.ToString());
                        if (line.Length > limits.MaxObjLineLength)
                            return Result<ObjInfo>.Fail("import.obj_line_too_long", "error.magicMismatch", $"line {lineNo}");
                        if (line.Length < 2 || line[0] == '#') continue;

                        if (line[0] == 'v' && line[1] == ' ')
                        {
                            info.VertexCount++;
                            anyV = true;
                            var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 4 &&
                                TryFloat(parts[1], out float x) && TryFloat(parts[2], out float y) && TryFloat(parts[3], out float z))
                            {
                                if (x < minX) minX = x; if (x > maxX) maxX = x;
                                if (y < minY) minY = y; if (y > maxY) maxY = y;
                                if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
                            }
                        }
                        else if (line.StartsWith("vn ")) { info.NormalCount++; info.HasNormals = true; }
                        else if (line.StartsWith("vt ")) { info.TexCoordCount++; info.HasTexCoords = true; }
                        else if (line[0] == 'f' && line[1] == ' ')
                        {
                            info.FaceCount++;
                            int verts = CountFaceVerts(line);
                            info.TriangleEstimate += Math.Max(1, verts - 2); // fan triangulation estimate
                        }
                        else if (line[0] == 'g' && line[1] == ' ') info.GroupCount++;
                        else if (line.StartsWith("usemtl")) info.MaterialRefCount++;
                    }
                }

                if (info.VertexCount > limits.MaxVertices)
                    return Result<ObjInfo>.Fail("import.too_many_vertices", "error.tooManyTriangles", info.VertexCount.ToString());
                if (info.TriangleEstimate > limits.MaxTriangles)
                    return Result<ObjInfo>.Fail("import.too_many_triangles", "error.tooManyTriangles", info.TriangleEstimate.ToString());

                info.BoundsMin = anyV ? new Float3(minX, minY, minZ) : Float3.Zero;
                info.BoundsMax = anyV ? new Float3(maxX, maxY, maxZ) : Float3.Zero;
                return Result<ObjInfo>.Success(info);
            }
            catch (Exception e)
            {
                return Result<ObjInfo>.Fail("import.obj_read", "error.magicMismatch", e.Message);
            }
        }

        private static bool TryFloat(string s, out float v)
        {
            bool ok = float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            return ok && !float.IsInfinity(v) && !float.IsNaN(v);
        }

        private static int CountFaceVerts(string line)
        {
            int spaces = 0;
            bool inSpace = true;
            for (int i = 1; i < line.Length; i++)
            {
                char c = line[i];
                if (c == ' ' || c == '\t') { if (!inSpace) { spaces++; inSpace = true; } }
                else inSpace = false;
            }
            if (!inSpace) spaces++;
            return spaces;
        }
    }
}
