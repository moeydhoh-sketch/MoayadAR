using System;
using MoayadAR.Core;

namespace MoayadAR.Analysis
{
    public readonly struct ScaleRecommendation
    {
        public readonly float UniformScale;      // multiply source scale by this
        public readonly AssetCategory Category;
        public readonly float Confidence01;
        public readonly string ReasonKey;        // autoscale.reason.* localization key
        public readonly float MinMeters, MaxMeters; // suggested range for humans etc.
        public ScaleRecommendation(float scale, AssetCategory cat, float conf, string reason, float min, float max)
        { UniformScale = scale; Category = cat; Confidence01 = conf; ReasonKey = reason; MinMeters = min; MaxMeters = max; }
    }

    /// <summary>
    /// Auto Scale. Honest by construction: files without unit metadata get an estimate with
    /// LOW confidence and the reason "unknown" — never a fabricated certainty (master prompt §6).
    /// Humans get a plausible range, not one imposed height.
    /// </summary>
    public static class AutoScaleRecommender
    {
        // Typical real-world heights/lengths (meters) per category.
        private static readonly (AssetCategory cat, float min, float max, float typical)[] Ranges =
        {
            (AssetCategory.Human,       1.4f,  2.1f,   1.75f),
            (AssetCategory.Animal,      0.15f, 2.5f,   0.8f),
            (AssetCategory.Furniture,   0.3f,  2.4f,   0.9f),
            (AssetCategory.Vehicle,     1.5f,  6.0f,   4.5f),
            (AssetCategory.SmallObject, 0.02f, 0.5f,   0.15f),
            (AssetCategory.Building,    3.0f,  80.0f,  10.0f),
        };

        /// <param name="sourceSize">Bounds size in source units.</param>
        /// <param name="metersPerSourceUnit">From explicit unit metadata when present; NaN when unknown.</param>
        /// <param name="category">Optional local classifier output; Unknown falls back to heuristics.</param>
        public static ScaleRecommendation Recommend(Float3 sourceSize, float metersPerSourceUnit, AssetCategory category)
        {
            float largest = sourceSize.MaxComponent;
            if (largest <= 1e-6f)
                return new ScaleRecommendation(1f, AssetCategory.Unknown, 0.1f, "autoscale.reason.unknown", 0.02f, 1f);

            // Path 1: explicit units — highest confidence.
            if (!float.IsNaN(metersPerSourceUnit) && metersPerSourceUnit > 0)
            {
                float meters = largest * metersPerSourceUnit;
                var cat = category != AssetCategory.Unknown ? category : GuessCategoryFromMeters(meters);
                float conf = ReasonableForCategory(meters, cat) ? 0.9f : 0.6f;
                float scale = 1f;
                if (!ReasonableForCategory(meters, cat))
                {
                    float target = TypicalFor(cat);
                    scale = target / meters;
                }
                var (min, max) = RangeFor(cat);
                return new ScaleRecommendation(scale, cat, conf, "autoscale.reason.units", min, max);
            }

            // Path 2: classified category, unknown units — estimate scale from category typical size.
            if (category != AssetCategory.Unknown)
            {
                float target = TypicalFor(category);
                float scale = target / largest;
                var (min, max) = RangeFor(category);
                return new ScaleRecommendation(scale, category, 0.5f, "autoscale.reason.bounds", min, max);
            }

            // Path 3: nothing reliable — normalize to a viewable size and say so.
            float genericTarget = largest > 100f || largest < 0.01f ? 0.5f : largest;
            float genericScale = genericTarget / largest;
            return new ScaleRecommendation(genericScale, AssetCategory.Unknown, 0.25f, "autoscale.reason.unknown", 0.05f, 5f);
        }

        public static AssetCategory GuessCategoryFromMeters(float largestMeters)
        {
            if (largestMeters >= 3.0f) return AssetCategory.Building;
            if (largestMeters >= 1.5f && largestMeters <= 2.2f) return AssetCategory.Human;
            if (largestMeters > 2.2f && largestMeters < 3.0f) return AssetCategory.Vehicle;
            if (largestMeters >= 0.3f && largestMeters < 1.5f) return AssetCategory.Furniture;
            if (largestMeters < 0.3f) return AssetCategory.SmallObject;
            return AssetCategory.Unknown;
        }

        private static float TypicalFor(AssetCategory cat)
        {
            foreach (var r in Ranges) if (r.cat == cat) return r.typical;
            return 0.5f;
        }

        private static (float min, float max) RangeFor(AssetCategory cat)
        {
            foreach (var r in Ranges) if (r.cat == cat) return (r.min, r.max);
            return (0.02f, 5f);
        }

        private static bool ReasonableForCategory(float meters, AssetCategory cat)
        {
            var (min, max) = RangeFor(cat);
            return meters >= min && meters <= max;
        }
    }
}
