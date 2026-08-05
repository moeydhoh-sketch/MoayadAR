using System;
using System.Collections.Generic;
using System.IO;
using MoayadAR.Core;
using Newtonsoft.Json;

namespace MoayadAR.Persistence
{
    /// <summary>
    /// Versioned per-project JSON store. Human-inspectable, diff-friendly, atomic writes.
    /// Schema migrations run through Migrate(); unknown newer versions fail closed.
    /// </summary>
    public sealed class ProjectStore
    {
        public const int CurrentSchemaVersion = 1;

        private sealed class Envelope
        {
            public int SchemaVersion;
            public ProjectRecord Project;
            public RoomRecord Room;
        }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public string RootDirectory { get; }

        public ProjectStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory)) throw new ArgumentException(nameof(rootDirectory));
            RootDirectory = rootDirectory;
            Directory.CreateDirectory(rootDirectory);
        }

        public Result<ProjectRecord> Save(ProjectRecord project, RoomRecord room)
        {
            try
            {
                var env = new Envelope { SchemaVersion = CurrentSchemaVersion, Project = project, Room = room };
                string path = PathFor(project.Id);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(env, JsonSettings));
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
                project.ModifiedUtc = DateTime.UtcNow;
                return Result<ProjectRecord>.Success(project);
            }
            catch (Exception e)
            {
                return Result<ProjectRecord>.Fail("persist.save_failed", "error.persistence", e.Message);
            }
        }

        public Result<(ProjectRecord project, RoomRecord room)> Load(string projectId)
        {
            try
            {
                string path = PathFor(projectId);
                if (!File.Exists(path))
                    return Result<(ProjectRecord, RoomRecord)>.Fail("persist.not_found", "error.projectNotFound", projectId);
                var env = JsonConvert.DeserializeObject<Envelope>(File.ReadAllText(path), JsonSettings);
                if (env == null)
                    return Result<(ProjectRecord, RoomRecord)>.Fail("persist.corrupt", "error.persistence", "null envelope");
                if (env.SchemaVersion > CurrentSchemaVersion)
                    return Result<(ProjectRecord, RoomRecord)>.Fail("persist.newer_schema", "error.projectNewerVersion",
                        $"schema {env.SchemaVersion} > {CurrentSchemaVersion}");
                Migrate(env);
                return Result<(ProjectRecord, RoomRecord)>.Success((env.Project, env.Room));
            }
            catch (Exception e)
            {
                return Result<(ProjectRecord, RoomRecord)>.Fail("persist.load_failed", "error.persistence", e.Message);
            }
        }

        public IReadOnlyList<ProjectRecord> ListProjects()
        {
            var list = new List<ProjectRecord>();
            foreach (var f in Directory.EnumerateFiles(RootDirectory, "*.json"))
            {
                try
                {
                    var env = JsonConvert.DeserializeObject<Envelope>(File.ReadAllText(f), JsonSettings);
                    if (env?.Project != null && env.SchemaVersion <= CurrentSchemaVersion) list.Add(env.Project);
                }
                catch { /* skip corrupt file; never crash the projects list */ }
            }
            list.Sort((a, b) => b.ModifiedUtc.CompareTo(a.ModifiedUtc));
            return list;
        }

        public bool Delete(string projectId)
        {
            string path = PathFor(projectId);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        private void Migrate(Envelope env) { /* v1: nothing to migrate; hook for future versions */ }

        private string PathFor(string id)
        {
            // Defend against path traversal: only a GUID-like id may reach the file system.
            foreach (char c in id)
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    throw new ArgumentException("invalid project id", nameof(id));
            return Path.Combine(RootDirectory, id + ".json");
        }
    }
}
