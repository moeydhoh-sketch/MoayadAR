namespace MoayadAR.Import
{
    /// <summary>Staged progress — UI shows the real phase, never a fake bar. Keys map to import.phase.* in localization.</summary>
    public enum ImportPhase
    {
        Reading, Validation, Parsing, Textures, Materials, Skeleton, Animations, Optimizing, GpuUpload, Done
    }

    public readonly struct ImportProgress
    {
        public readonly ImportPhase Phase;
        public readonly float PhaseFraction01; // honest fraction within the phase; -1 when indeterminate
        public ImportProgress(ImportPhase phase, float fraction01) { Phase = phase; PhaseFraction01 = fraction01; }
        public string LocalizationKey => "import.phase." + Phase.ToString().ToLowerInvariant();
    }
}
