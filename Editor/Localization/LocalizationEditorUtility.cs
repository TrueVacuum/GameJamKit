using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameJamKit.Localization;
using GameJamKit.Localization.UGUI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameJamKit.Editor.Localization
{
    internal enum LocalizationIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    internal readonly struct LocalizationIssue
    {
        public LocalizationIssue(LocalizationIssueSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public LocalizationIssueSeverity Severity { get; }
        public string Message { get; }
    }

    internal static class LocalizationEditorUtility
    {
        private static readonly Regex WordBoundaryRegex =
            new Regex("([a-z0-9])([A-Z])", RegexOptions.Compiled);
        private static readonly Regex InvalidKeyCharactersRegex =
            new Regex("[^a-zA-Z0-9]+", RegexOptions.Compiled);
        private static readonly Regex FormatArgumentRegex =
            new Regex(@"(?<!\{)\{(\d+)(?:[^{}]*)\}(?!\})", RegexOptions.Compiled);

        public static LocalizationProfile FindProfile(LocalizationManager preferredManager = null)
        {
            if (preferredManager != null && preferredManager.Profile != null)
            {
                return preferredManager.Profile;
            }

            LocalizationManager sceneManager = UnityEngine.Object.FindFirstObjectByType<LocalizationManager>(
                FindObjectsInactive.Include);
            if (sceneManager != null && sceneManager.Profile != null)
            {
                return sceneManager.Profile;
            }

            string[] guids = AssetDatabase.FindAssets("t:LocalizationProfile");
            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LocalizationProfile>(path);
        }

        public static List<LocalizationCsvDocument> LoadDocuments(LocalizationProfile profile)
        {
            List<LocalizationCsvDocument> documents = new List<LocalizationCsvDocument>();
            if (profile == null)
            {
                return documents;
            }

            IReadOnlyList<TextAsset> sources = profile.CsvSources;
            for (int i = 0; i < sources.Count; i++)
            {
                if (LocalizationCsvDocument.TryLoad(sources[i], out LocalizationCsvDocument document, out _))
                {
                    documents.Add(document);
                }
            }

            return documents;
        }

        public static List<string> GetKeys(IEnumerable<LocalizationCsvDocument> documents)
        {
            return documents
                .SelectMany(document => document.GetKeys())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
        }

        public static string FindUniqueKeyByTranslation(
            IEnumerable<LocalizationCsvDocument> documents,
            string localeCode,
            string translation)
        {
            if (string.IsNullOrEmpty(localeCode) || string.IsNullOrEmpty(translation))
            {
                return null;
            }

            string match = null;
            foreach (LocalizationCsvDocument document in documents)
            {
                int localeColumn = document.FindLocaleColumn(localeCode);
                if (localeColumn < 0)
                {
                    continue;
                }

                IReadOnlyList<string[]> rows = document.Rows;
                for (int i = 0; i < rows.Count; i++)
                {
                    string[] row = rows[i];
                    if (row.Length <= localeColumn || string.IsNullOrWhiteSpace(row[0]) ||
                        !string.Equals(
                            row[localeColumn].Trim(),
                            translation.Trim(),
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string key = row[0].Trim();
                    if (match != null && !string.Equals(match, key, StringComparison.Ordinal))
                    {
                        return null;
                    }

                    match = key;
                }
            }

            return match;
        }

        public static string SuggestKey(TMP_Text text, IEnumerable<string> existingKeys)
        {
            string objectName = text != null ? text.gameObject.name : "text";
            objectName = objectName
                .Replace("(TMP)", string.Empty)
                .Replace("TextMeshPro", string.Empty);
            objectName = WordBoundaryRegex.Replace(objectName, "$1_$2");
            objectName = InvalidKeyCharactersRegex.Replace(objectName, "_")
                .Trim('_')
                .ToLowerInvariant();

            if (string.IsNullOrEmpty(objectName))
            {
                objectName = "text";
            }

            string baseKey = $"ui.{objectName}";
            HashSet<string> keys = new HashSet<string>(existingKeys, StringComparer.Ordinal);
            if (!keys.Contains(baseKey))
            {
                return baseKey;
            }

            int suffix = 2;
            while (keys.Contains($"{baseKey}_{suffix}"))
            {
                suffix++;
            }

            return $"{baseKey}_{suffix}";
        }

        public static void AssignKey(LocalizedText localizedText, string key)
        {
            SerializedObject serializedText = new SerializedObject(localizedText);
            serializedText.FindProperty("_key").stringValue = key;
            serializedText.ApplyModifiedProperties();
            EditorUtility.SetDirty(localizedText);

            if (localizedText.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(localizedText.gameObject.scene);
            }
        }

        public static List<LocalizationIssue> Validate(LocalizationProfile profile)
        {
            List<LocalizationIssue> issues = new List<LocalizationIssue>();
            if (profile == null)
            {
                issues.Add(new LocalizationIssue(
                    LocalizationIssueSeverity.Error,
                    "Assign a Localization Profile."));
                return issues;
            }

            IReadOnlyList<LocalizationLocale> locales = profile.SupportedLocales;
            HashSet<string> configuredLocales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < locales.Count; i++)
            {
                LocalizationLocale locale = locales[i];
                if (locale == null || string.IsNullOrEmpty(locale.Code))
                {
                    issues.Add(new LocalizationIssue(
                        LocalizationIssueSeverity.Error,
                        $"Supported locale element {i} has no code."));
                    continue;
                }

                if (!configuredLocales.Add(locale.Code))
                {
                    issues.Add(new LocalizationIssue(
                        LocalizationIssueSeverity.Error,
                        $"Supported locale '{locale.Code}' is duplicated."));
                }
            }

            if (!configuredLocales.Contains(profile.DefaultLocaleCode))
            {
                issues.Add(new LocalizationIssue(
                    LocalizationIssueSeverity.Error,
                    $"Default locale '{profile.DefaultLocaleCode}' is not configured."));
            }

            if (!configuredLocales.Contains(profile.FallbackLocaleCode))
            {
                issues.Add(new LocalizationIssue(
                    LocalizationIssueSeverity.Error,
                    $"Fallback locale '{profile.FallbackLocaleCode}' is not configured."));
            }

            List<LocalizationCsvDocument> documents = new List<LocalizationCsvDocument>();
            IReadOnlyList<TextAsset> sources = profile.CsvSources;
            for (int i = 0; i < sources.Count; i++)
            {
                if (LocalizationCsvDocument.TryLoad(
                        sources[i],
                        out LocalizationCsvDocument document,
                        out string error))
                {
                    documents.Add(document);
                }
                else
                {
                    issues.Add(new LocalizationIssue(LocalizationIssueSeverity.Error, error));
                }
            }

            Dictionary<string, string> keySources =
                new Dictionary<string, string>(StringComparer.Ordinal);

            for (int documentIndex = 0; documentIndex < documents.Count; documentIndex++)
            {
                LocalizationCsvDocument document = documents[documentIndex];

                foreach (string localeCode in configuredLocales)
                {
                    if (document.FindLocaleColumn(localeCode) < 0)
                    {
                        issues.Add(new LocalizationIssue(
                            LocalizationIssueSeverity.Error,
                            $"{document.Asset.name} is missing locale column '{localeCode}'."));
                    }
                }

                foreach (string[] row in document.Rows)
                {
                    if (row.Length == 0 || string.IsNullOrWhiteSpace(row[0]) ||
                        row[0].TrimStart().StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string key = row[0].Trim();
                    if (keySources.TryGetValue(key, out string previousSource))
                    {
                        issues.Add(new LocalizationIssue(
                            LocalizationIssueSeverity.Error,
                            $"Key '{key}' appears in both {previousSource} and {document.Asset.name}."));
                    }
                    else
                    {
                        keySources.Add(key, document.Asset.name);
                    }

                    string referenceSignature = null;
                    foreach (string localeCode in configuredLocales)
                    {
                        int column = document.FindLocaleColumn(localeCode);
                        if (column < 0)
                        {
                            continue;
                        }

                        string translation = row[column];
                        if (string.IsNullOrEmpty(translation))
                        {
                            issues.Add(new LocalizationIssue(
                                LocalizationIssueSeverity.Warning,
                                $"Key '{key}' has no '{localeCode}' translation in {document.Asset.name}."));
                            continue;
                        }

                        string signature = GetFormatSignature(translation);
                        referenceSignature ??= signature;
                        if (!string.Equals(referenceSignature, signature, StringComparison.Ordinal))
                        {
                            issues.Add(new LocalizationIssue(
                                LocalizationIssueSeverity.Error,
                                $"Key '{key}' uses different format arguments in locale '{localeCode}'."));
                        }
                    }
                }
            }

            if (issues.Count == 0)
            {
                issues.Add(new LocalizationIssue(
                    LocalizationIssueSeverity.Info,
                    "No localization issues found."));
            }

            return issues;
        }

        private static string GetFormatSignature(string text)
        {
            MatchCollection matches = FormatArgumentRegex.Matches(text ?? string.Empty);
            SortedSet<int> indices = new SortedSet<int>();
            for (int i = 0; i < matches.Count; i++)
            {
                if (int.TryParse(matches[i].Groups[1].Value, out int index))
                {
                    indices.Add(index);
                }
            }

            StringBuilder builder = new StringBuilder();
            foreach (int index in indices)
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(index);
            }

            return builder.ToString();
        }
    }
}
