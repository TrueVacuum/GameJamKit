using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameJamKit.Localization
{
    [Serializable]
    public sealed class LocalizationLocale
    {
        [SerializeField] private string _code = "en";
        [SerializeField] private string _displayName = "English";
        [SerializeField] private TMP_FontAsset _fontOverride;
        [SerializeField] private List<SystemLanguage> _systemLanguages = new List<SystemLanguage>();

        public string Code => NormalizeCode(_code);
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? Code : _displayName;
        public TMP_FontAsset FontOverride => _fontOverride;
        public IReadOnlyList<SystemLanguage> SystemLanguages => _systemLanguages;

        public bool Matches(SystemLanguage systemLanguage)
        {
            return _systemLanguages != null && _systemLanguages.Contains(systemLanguage);
        }

        internal static string NormalizeCode(string code)
        {
            return string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim();
        }
    }
}
