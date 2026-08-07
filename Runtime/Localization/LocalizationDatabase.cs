using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamKit.Localization
{
    public sealed class LocalizationDatabase
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<string> LocaleCodes => _translations.Keys;

        public static LocalizationDatabase Create(
            IEnumerable<TextAsset> csvSources,
            ICollection<string> issues = null)
        {
            LocalizationDatabase database = new LocalizationDatabase();
            if (csvSources == null)
            {
                return database;
            }

            foreach (TextAsset source in csvSources)
            {
                if (source == null)
                {
                    issues?.Add("A localization CSV source is missing.");
                    continue;
                }

                database.MergeCsv(source.text, source.name, issues);
            }

            return database;
        }

        public void MergeCsv(
            string csv,
            string sourceName = "Localization CSV",
            ICollection<string> issues = null)
        {
            List<string[]> rows;
            try
            {
                rows = LocalizationCsvParser.Parse(csv);
            }
            catch (FormatException exception)
            {
                issues?.Add($"{sourceName}: {exception.Message}");
                return;
            }

            if (rows.Count == 0)
            {
                issues?.Add($"{sourceName}: the CSV is empty.");
                return;
            }

            string[] header = rows[0];
            if (header.Length < 2 || !string.Equals(
                    header[0].TrimStart('\uFEFF').Trim(),
                    "key",
                    StringComparison.OrdinalIgnoreCase))
            {
                issues?.Add($"{sourceName}: the first header cell must be 'key'.");
                return;
            }

            string[] localeCodes = new string[header.Length];
            HashSet<string> sourceLocales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int column = 1; column < header.Length; column++)
            {
                string localeCode = LocalizationLocale.NormalizeCode(header[column]);
                if (string.IsNullOrEmpty(localeCode))
                {
                    issues?.Add($"{sourceName}: language column {column + 1} has no locale code.");
                    continue;
                }

                if (!sourceLocales.Add(localeCode))
                {
                    issues?.Add($"{sourceName}: locale column '{localeCode}' is duplicated.");
                    continue;
                }

                localeCodes[column] = localeCode;
                EnsureLocale(localeCode);
            }

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                string key = row.Length > 0 ? row[0].Trim() : string.Empty;
                if (string.IsNullOrEmpty(key) || key.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                for (int column = 1; column < localeCodes.Length; column++)
                {
                    string localeCode = localeCodes[column];
                    if (string.IsNullOrEmpty(localeCode) || column >= row.Length)
                    {
                        continue;
                    }

                    string text = row[column];
                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    Dictionary<string, string> localeEntries = _translations[localeCode];
                    if (localeEntries.ContainsKey(key))
                    {
                        issues?.Add(
                            $"{sourceName}: key '{key}' is duplicated for locale '{localeCode}'. " +
                            "The later value is used.");
                    }

                    localeEntries[key] = text;
                }
            }
        }

        public bool ContainsLocale(string localeCode)
        {
            return _translations.ContainsKey(LocalizationLocale.NormalizeCode(localeCode));
        }

        public bool TryGetText(string localeCode, string key, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _translations.TryGetValue(
                       LocalizationLocale.NormalizeCode(localeCode),
                       out Dictionary<string, string> localeEntries) &&
                   localeEntries.TryGetValue(key, out text);
        }

        private void EnsureLocale(string localeCode)
        {
            if (!_translations.ContainsKey(localeCode))
            {
                _translations.Add(
                    localeCode,
                    new Dictionary<string, string>(StringComparer.Ordinal));
            }
        }
    }
}
