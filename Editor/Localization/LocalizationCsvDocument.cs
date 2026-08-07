using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameJamKit.Localization;
using UnityEditor;
using UnityEngine;

namespace GameJamKit.Editor.Localization
{
    internal sealed class LocalizationCsvDocument
    {
        private readonly List<string[]> _rows;

        private LocalizationCsvDocument(TextAsset asset, string[] headers, List<string[]> rows)
        {
            Asset = asset;
            Headers = headers;
            _rows = rows;
        }

        public TextAsset Asset { get; }
        public string[] Headers { get; }
        public IReadOnlyList<string[]> Rows => _rows;

        public static bool TryLoad(
            TextAsset asset,
            out LocalizationCsvDocument document,
            out string error)
        {
            document = null;
            error = null;

            if (asset == null)
            {
                error = "CSV source is missing.";
                return false;
            }

            List<string[]> parsedRows;
            try
            {
                parsedRows = LocalizationCsvParser.Parse(asset.text);
            }
            catch (FormatException exception)
            {
                error = $"{asset.name}: {exception.Message}";
                return false;
            }

            if (parsedRows.Count == 0 || parsedRows[0].Length < 2)
            {
                error = $"{asset.name}: CSV must contain a key column and at least one language.";
                return false;
            }

            string[] headers = parsedRows[0];
            headers[0] = headers[0].TrimStart('\uFEFF').Trim();
            if (!string.Equals(headers[0], "key", StringComparison.OrdinalIgnoreCase))
            {
                error = $"{asset.name}: the first header cell must be 'key'.";
                return false;
            }

            for (int i = 1; i < headers.Length; i++)
            {
                headers[i] = headers[i].Trim();
            }

            List<string[]> rows = new List<string[]>();
            for (int i = 1; i < parsedRows.Count; i++)
            {
                string[] normalizedRow = new string[headers.Length];
                string[] parsedRow = parsedRows[i];
                int count = Mathf.Min(parsedRow.Length, normalizedRow.Length);
                Array.Copy(parsedRow, normalizedRow, count);

                for (int column = 0; column < normalizedRow.Length; column++)
                {
                    normalizedRow[column] ??= string.Empty;
                }

                rows.Add(normalizedRow);
            }

            document = new LocalizationCsvDocument(asset, headers, rows);
            return true;
        }

        public int FindLocaleColumn(string localeCode)
        {
            for (int i = 1; i < Headers.Length; i++)
            {
                if (string.Equals(
                        Headers[i],
                        localeCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public string[] FindRow(string key)
        {
            return _rows.FirstOrDefault(row =>
                row.Length > 0 &&
                string.Equals(row[0].Trim(), key, StringComparison.Ordinal));
        }

        public IEnumerable<string> GetKeys()
        {
            return _rows
                .Where(row => row.Length > 0 &&
                              !string.IsNullOrWhiteSpace(row[0]) &&
                              !row[0].TrimStart().StartsWith("#", StringComparison.Ordinal))
                .Select(row => row[0].Trim());
        }

        public bool AddKey(string key, string sourceLocaleCode, string sourceText, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                error = "Localization key cannot be empty.";
                return false;
            }

            key = key.Trim();
            if (FindRow(key) != null)
            {
                error = $"Key '{key}' already exists in {Asset.name}.";
                return false;
            }

            int sourceColumn = FindLocaleColumn(sourceLocaleCode);
            if (sourceColumn < 0)
            {
                error = $"{Asset.name} does not contain locale column '{sourceLocaleCode}'.";
                return false;
            }

            string[] row = new string[Headers.Length];
            for (int i = 0; i < row.Length; i++)
            {
                row[i] = string.Empty;
            }

            row[0] = key;
            row[sourceColumn] = sourceText ?? string.Empty;
            _rows.Add(row);
            return true;
        }

        public string GetTranslation(string key, string localeCode)
        {
            string[] row = FindRow(key);
            int column = FindLocaleColumn(localeCode);
            return row != null && column >= 0 ? row[column] : null;
        }

        public void Save()
        {
            string assetPath = AssetDatabase.GetAssetPath(Asset);
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new IOException("Cannot determine the CSV asset path.");
            }

            StringBuilder builder = new StringBuilder();
            AppendRow(builder, Headers);
            for (int i = 0; i < _rows.Count; i++)
            {
                AppendRow(builder, _rows[i]);
            }

            File.WriteAllText(
                Path.GetFullPath(assetPath),
                builder.ToString(),
                new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void AppendRow(StringBuilder builder, string[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(Escape(row[i] ?? string.Empty));
            }

            builder.Append('\n');
        }

        private static string Escape(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
