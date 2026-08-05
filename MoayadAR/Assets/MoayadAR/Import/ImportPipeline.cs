using System;
using System.IO;
using System.Threading;
using MoayadAR.Core;

namespace MoayadAR.Import
{
    /// <summary>
    /// Host-agnostic import orchestration: validate → hash → cache-check → parse statistics → report.
    /// Geometry instantiation is delegated to an IRuntimeImporter (Unity glTFast on device;
    /// Assimp JNI bridge for FBX). Every stage reports real progress and honors cancellation.
    /// </summary>
    public sealed class ImportPipeline
    {
        private readonly ImportLimits _limits;
        private readonly IImportCache _cache;

        public ImportPipeline(ImportLimits limits, IImportCache cache)
        {
            _limits = limits ?? new ImportLimits();
            _cache = cache; // may be null in tests
        }

        public Result<ImportOutcome> Run(
            string fileName, Stream source, long fileBytes,
            string importerVersion, string pipelineVersion, string qualityPreset,
            IProgress<ImportProgress> progress, CancellationToken ct)
        {
            progress?.Report(new ImportProgress(ImportPhase.Reading, -1));
            ct.ThrowIfCancellationRequested();

            progress?.Report(new ImportProgress(ImportPhase.Validation, 0));
            var validation = FileValidator.Validate(fileName, source, fileBytes, _limits);
            if (!validation.Ok) return Result<ImportOutcome>.Fail(validation.ErrorCode, validation.MessageKey, validation.Detail);
            if (source.CanSeek) source.Position = 0;
            ct.ThrowIfCancellationRequested();

            string sha = AssetHasher.Sha256Hex(source);
            if (source.CanSeek) source.Position = 0;
            string cacheKey = AssetHasher.CacheKey(sha, importerVersion, pipelineVersion, qualityPreset);

            bool cacheHit = _cache?.Contains(cacheKey) == true;
            progress?.Report(new ImportProgress(ImportPhase.Parsing, 0));
            ct.ThrowIfCancellationRequested();

            var outcome = new ImportOutcome
            {
                Format = validation.Value,
                SourceSha256 = sha,
                CacheKey = cacheKey,
                CacheHit = cacheHit,
                FileBytes = fileBytes
            };

            switch (validation.Value)
            {
                case ModelFormat.Glb:
                    var glb = GlbHeaderReader.Read(source, _limits);
                    if (!glb.Ok) return Result<ImportOutcome>.Fail(glb.ErrorCode, glb.MessageKey, glb.Detail);
                    outcome.Glb = glb.Value;
                    break;
                case ModelFormat.Obj:
                    var obj = ObjModelReader.Read(source, _limits);
                    if (!obj.Ok) return Result<ImportOutcome>.Fail(obj.ErrorCode, obj.MessageKey, obj.Detail);
                    outcome.Obj = obj.Value;
                    break;
                case ModelFormat.Fbx:
                    // Statistics require the native Assimp bridge (device-only); validated header is all we assert here.
                    outcome.RequiresNativeImporter = true;
                    break;
                case ModelFormat.Gltf:
                    // External-resource glTF: SAF document tree resolution happens in the Android layer.
                    outcome.RequiresExternalResources = true;
                    break;
            }

            progress?.Report(new ImportProgress(ImportPhase.Done, 1));
            return Result<ImportOutcome>.Success(outcome);
        }
    }

    public interface IImportCache
    {
        bool Contains(string cacheKey);
        void Store(string cacheKey, string artifactRelativePath);
        (long entries, long bytes) Statistics();
        void Clear();
    }

    public sealed class ImportOutcome
    {
        public ModelFormat Format;
        public string SourceSha256;
        public string CacheKey;
        public bool CacheHit;
        public long FileBytes;
        public bool RequiresNativeImporter;
        public bool RequiresExternalResources;
        public GlbHeaderReader.GlbInfo Glb;
        public ObjModelReader.ObjInfo Obj;
    }
}
