using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MoayadAR.Localization
{
    public enum AppLanguage { Arabic, English }

    /// <summary>
    /// Table-driven localization. No user-facing string may be hardcoded in feature code —
    /// everything resolves through Get(). Missing keys fall back to English, then to the key itself,
    /// and are recorded so QA can find them.
    /// </summary>
    public sealed class LocalizationService
    {
        public AppLanguage Language { get; private set; } = AppLanguage.Arabic;
        public bool IsRtl => Language == AppLanguage.Arabic;

        private readonly Dictionary<AppLanguage, Dictionary<string, string>> _tables =
            new Dictionary<AppLanguage, Dictionary<string, string>>();
        private readonly List<string> _missingKeys = new List<string>();
        public IReadOnlyList<string> MissingKeys => _missingKeys;

        public void LoadTable(AppLanguage lang, string json)
        {
            var table = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                        ?? new Dictionary<string, string>();
            _tables[lang] = table;
        }

        public void SetLanguage(AppLanguage lang) => Language = lang;

        public string Get(string key)
        {
            if (key == null) return string.Empty;
            if (_tables.TryGetValue(Language, out var t) && t.TryGetValue(key, out var v)) return v;
            if (Language != AppLanguage.English &&
                _tables.TryGetValue(AppLanguage.English, out var en) && en.TryGetValue(key, out v)) return v;
            if (!_missingKeys.Contains(key)) _missingKeys.Add(key);
            return key;
        }

        public string Get(string key, params object[] args)
        {
            string raw = Get(key);
            try { return string.Format(raw, args); }
            catch (FormatException) { return raw; }
        }

        /// <summary>Direction-aware string for layout tests: Arabic strings must not start with Latin letters.</summary>
        public static bool LooksArabic(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (c >= '؀' && c <= 'ۿ') return true;   // Arabic block
                else if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c)) return false;
            return false;
        }
    }
}
