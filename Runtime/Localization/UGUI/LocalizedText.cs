using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace GameJamKit.Localization.UGUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly HashSet<string> WarnedFontLocalePairs = new HashSet<string>();
#endif

        [SerializeField] private LocalizationManager _manager;
        [SerializeField] private TMP_Text _target;
        [SerializeField] private string _key;

        private object[] _arguments;
        private TMP_FontAsset _originalFont;
        private bool _hasOriginalFont;

        public string Key => _key;

        private void OnEnable()
        {
            ResolveReferences();
            CacheOriginalFont();
            if (_manager == null)
            {
                Debug.LogWarning("Localized Text requires a Localization Manager.", this);
                ApplyMissingText();
                return;
            }

            _manager.Initialize();
            _manager.LanguageChanged += HandleLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.LanguageChanged -= HandleLanguageChanged;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_target == null)
            {
                _target = GetComponent<TMP_Text>();
            }
        }
#endif

        public void SetKey(string key)
        {
            _key = key;
            Refresh();
        }

        public void SetArguments(params object[] arguments)
        {
            _arguments = arguments;
            Refresh();
        }

        public void Refresh()
        {
            ResolveReferences();
            if (_target == null)
            {
                return;
            }

            if (_manager == null)
            {
                ApplyMissingText();
                return;
            }

            ApplyLocalizedFont();
            _target.text = _manager.GetText(_key, _arguments);
            WarnIfFontIsMissingCharacters(_target.text);
        }

        private void ResolveReferences()
        {
            if (_target == null)
            {
                _target = GetComponent<TMP_Text>();
            }

            if (_manager == null)
            {
                _manager = LocalizationManager.Instance;
            }

            if (_manager == null)
            {
                _manager = FindFirstObjectByType<LocalizationManager>();
            }
        }

        private void HandleLanguageChanged(string localeCode)
        {
            Refresh();
        }

        private void CacheOriginalFont()
        {
            if (!_hasOriginalFont && _target != null)
            {
                _originalFont = _target.font;
                _hasOriginalFont = true;
            }
        }

        private void ApplyLocalizedFont()
        {
            TMP_FontAsset localizedFont = _manager.Profile != null
                ? _manager.Profile.GetFont(_manager.CurrentLocaleCode)
                : null;

            _target.font = localizedFont != null ? localizedFont : _originalFont;
        }

        private void ApplyMissingText()
        {
            if (_target != null)
            {
                _target.text = $"[MISSING: {_key}]";
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void WarnIfFontIsMissingCharacters(string text)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TMP_FontAsset font = _target != null ? _target.font : null;
            if (font == null || string.IsNullOrEmpty(text) ||
                font.HasCharacters(text, out uint[] missingCharacters, true, true))
            {
                return;
            }

            string localeCode = _manager != null ? _manager.CurrentLocaleCode : string.Empty;
            string warningId = $"{font.GetInstanceID()}:{localeCode}";
            if (!WarnedFontLocalePairs.Add(warningId))
            {
                return;
            }

            Debug.LogWarning(
                $"Localization font '{font.name}' is missing characters for locale " +
                $"'{localeCode}': {FormatMissingCharacters(missingCharacters)}. " +
                "Assign a suitable font in the Localization Profile or add it to the " +
                "TMP font asset's Fallback Font Assets list.",
                this);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static string FormatMissingCharacters(uint[] missingCharacters)
        {
            const int maximumDisplayedCharacters = 8;
            StringBuilder result = new StringBuilder();
            int count = Math.Min(missingCharacters.Length, maximumDisplayedCharacters);

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    result.Append(' ');
                }

                uint unicode = missingCharacters[i];
                result.Append('[');
                result.Append(char.ConvertFromUtf32((int)unicode));
                result.Append("] (U+");
                result.Append(unicode.ToString("X4"));
                result.Append(')');
            }

            if (missingCharacters.Length > count)
            {
                result.Append($" and {missingCharacters.Length - count} more");
            }

            return result.ToString();
        }
#endif
    }
}
