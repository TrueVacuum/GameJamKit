using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameJamKit.Localization;
using GameJamKit.Localization.UGUI;
using TMPro;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamKit.Editor.Localization
{
    public sealed class LocalizationWindow : EditorWindow
    {
        private sealed class Candidate
        {
            public TMP_Text Text;
            public bool Selected = true;
            public string Key;
        }

        private LocalizationProfile _profile;
        private readonly List<Candidate> _candidates = new List<Candidate>();
        private List<string> _availableKeys = new List<string>();
        private List<LocalizationIssue> _issues = new List<LocalizationIssue>();
        private readonly AdvancedDropdownState _keyDropdownState = new AdvancedDropdownState();
        private Vector2 _candidateScroll;
        private Vector2 _issueScroll;
        private int _targetSourceIndex;
        private int _sourceLocaleIndex;

        [MenuItem("Tools/Game Jam Kit/Localization")]
        public static void Open()
        {
            LocalizationWindow window = GetWindow<LocalizationWindow>();
            window.titleContent = new GUIContent("Localization");
            window.minSize = new Vector2(640f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            _profile = LocalizationEditorUtility.FindProfile();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Localization Authoring", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _profile = (LocalizationProfile)EditorGUILayout.ObjectField(
                "Profile",
                _profile,
                typeof(LocalizationProfile),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                _candidates.Clear();
                _issues.Clear();
                _targetSourceIndex = 0;
                _sourceLocaleIndex = 0;
            }

            if (_profile == null)
            {
                EditorGUILayout.HelpBox("Assign a Localization Profile.", MessageType.Info);
                return;
            }

            DrawAuthoringSettings();
            DrawToolbar();
            DrawCandidates();
            DrawValidation();
        }

        private void DrawAuthoringSettings()
        {
            IReadOnlyList<TextAsset> sources = _profile.CsvSources;
            string[] sourceNames = sources
                .Select(source => source != null ? source.name : "<missing>")
                .ToArray();

            if (sourceNames.Length > 0)
            {
                _targetSourceIndex = Mathf.Clamp(_targetSourceIndex, 0, sourceNames.Length - 1);
                _targetSourceIndex = EditorGUILayout.Popup(
                    "Target CSV",
                    _targetSourceIndex,
                    sourceNames);
            }
            else
            {
                EditorGUILayout.HelpBox("The profile has no CSV sources.", MessageType.Warning);
            }

            List<LocalizationLocale> locales = GetLocales();
            string[] localeNames = locales.Select(locale => locale.DisplayName).ToArray();
            if (localeNames.Length > 0)
            {
                _sourceLocaleIndex = Mathf.Clamp(_sourceLocaleIndex, 0, localeNames.Length - 1);
                _sourceLocaleIndex = EditorGUILayout.Popup(
                    "Current Text Language",
                    _sourceLocaleIndex,
                    localeNames);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Current Scene"))
                {
                    ScanScene();
                }

                if (GUILayout.Button("Validate CSV"))
                {
                    _issues = LocalizationEditorUtility.Validate(_profile);
                }

                using (new EditorGUI.DisabledScope(
                           _candidates.All(candidate => !candidate.Selected)))
                {
                    if (GUILayout.Button("Localize Selected"))
                    {
                        LocalizeSelected();
                    }
                }
            }
        }

        private void DrawCandidates()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Unlocalized TMP Texts ({_candidates.Count})",
                EditorStyles.boldLabel);

            if (_candidates.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Scan the current scene to find TMP Text components without Localized Text.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All", GUILayout.Width(90f)))
                {
                    _candidates.ForEach(candidate => candidate.Selected = true);
                }

                if (GUILayout.Button("Select None", GUILayout.Width(90f)))
                {
                    _candidates.ForEach(candidate => candidate.Selected = false);
                }
            }

            _candidateScroll = EditorGUILayout.BeginScrollView(
                _candidateScroll,
                GUILayout.MinHeight(140f),
                GUILayout.MaxHeight(280f));

            for (int i = 0; i < _candidates.Count; i++)
            {
                Candidate candidate = _candidates[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    candidate.Selected = EditorGUILayout.Toggle(
                        candidate.Selected,
                        GUILayout.Width(20f));

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(
                            candidate.Text,
                            typeof(TMP_Text),
                            true,
                            GUILayout.Width(190f));
                    }

                    candidate.Key = EditorGUILayout.TextField(candidate.Key);

                    using (new EditorGUI.DisabledScope(_availableKeys.Count == 0))
                    {
                        if (GUILayout.Button("...", GUILayout.Width(28f)))
                        {
                            Rect buttonRect = GUILayoutUtility.GetLastRect();
                            Candidate selectedCandidate = candidate;
                            LocalizationKeyDropdown dropdown = new LocalizationKeyDropdown(
                                _keyDropdownState,
                                _availableKeys,
                                key =>
                                {
                                    selectedCandidate.Key = key;
                                    Repaint();
                                });
                            dropdown.Show(buttonRect);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawValidation()
        {
            if (_issues.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Validation ({_issues.Count})",
                EditorStyles.boldLabel);

            _issueScroll = EditorGUILayout.BeginScrollView(
                _issueScroll,
                GUILayout.MinHeight(100f),
                GUILayout.MaxHeight(220f));

            int visibleCount = Mathf.Min(_issues.Count, 80);
            for (int i = 0; i < visibleCount; i++)
            {
                LocalizationIssue issue = _issues[i];
                EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));
            }

            if (_issues.Count > visibleCount)
            {
                EditorGUILayout.LabelField(
                    $"...and {_issues.Count - visibleCount} more issues.");
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanScene()
        {
            _candidates.Clear();
            Scene activeScene = SceneManager.GetActiveScene();
            List<LocalizationCsvDocument> documents =
                LocalizationEditorUtility.LoadDocuments(_profile);
            _availableKeys = LocalizationEditorUtility.GetKeys(documents);
            HashSet<string> usedKeys = new HashSet<string>(_availableKeys, StringComparer.Ordinal);
            List<LocalizationLocale> locales = GetLocales();
            string sourceLocaleCode = locales.Count > 0
                ? locales[Mathf.Clamp(_sourceLocaleIndex, 0, locales.Count - 1)].Code
                : string.Empty;

            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text.gameObject.scene != activeScene ||
                    string.IsNullOrWhiteSpace(text.text) ||
                    text.GetComponent<LocalizedText>() != null ||
                    text.GetComponentInParent<TMP_Dropdown>(true) != null)
                {
                    continue;
                }

                string key = LocalizationEditorUtility.FindUniqueKeyByTranslation(
                    documents,
                    sourceLocaleCode,
                    text.text);

                if (string.IsNullOrEmpty(key))
                {
                    key = LocalizationEditorUtility.SuggestKey(text, usedKeys);
                }

                usedKeys.Add(key);
                _candidates.Add(new Candidate
                {
                    Text = text,
                    Key = key
                });
            }

            _candidates.Sort((left, right) => string.Compare(
                left.Text.gameObject.name,
                right.Text.gameObject.name,
                StringComparison.OrdinalIgnoreCase));
        }

        private void LocalizeSelected()
        {
            IReadOnlyList<TextAsset> sources = _profile.CsvSources;
            List<LocalizationLocale> locales = GetLocales();
            if (sources.Count == 0 || locales.Count == 0 ||
                _targetSourceIndex >= sources.Count || _sourceLocaleIndex >= locales.Count)
            {
                EditorUtility.DisplayDialog(
                    "Localization",
                    "Configure a target CSV and source language first.",
                    "OK");
                return;
            }

            if (!LocalizationCsvDocument.TryLoad(
                    sources[_targetSourceIndex],
                    out LocalizationCsvDocument targetDocument,
                    out string loadError))
            {
                EditorUtility.DisplayDialog("Localization", loadError, "OK");
                return;
            }

            List<LocalizationCsvDocument> allDocuments =
                LocalizationEditorUtility.LoadDocuments(_profile);
            HashSet<string> existingKeys = new HashSet<string>(
                LocalizationEditorUtility.GetKeys(allDocuments),
                StringComparer.Ordinal);
            string sourceLocaleCode = locales[_sourceLocaleIndex].Code;
            int addedRows = 0;
            int boundTexts = 0;
            List<string> errors = new List<string>();
            List<(Candidate Candidate, string Key)> bindings =
                new List<(Candidate Candidate, string Key)>();

            for (int i = 0; i < _candidates.Count; i++)
            {
                Candidate candidate = _candidates[i];
                if (!candidate.Selected || candidate.Text == null ||
                    string.IsNullOrWhiteSpace(candidate.Key))
                {
                    continue;
                }

                string key = candidate.Key.Trim();
                if (!existingKeys.Contains(key))
                {
                    if (!targetDocument.AddKey(
                            key,
                            sourceLocaleCode,
                            candidate.Text.text,
                            out string addError))
                    {
                        errors.Add(addError);
                        continue;
                    }

                    existingKeys.Add(key);
                    addedRows++;
                }

                bindings.Add((candidate, key));
            }

            try
            {
                if (addedRows > 0)
                {
                    targetDocument.Save();
                }
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                EditorUtility.DisplayDialog(
                    "Localization",
                    "Could not write the CSV, so no scene components were changed. " +
                    "Close it in spreadsheet software and try again.\n\n" + exception.Message,
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Localize TMP Texts");

            for (int i = 0; i < bindings.Count; i++)
            {
                Candidate candidate = bindings[i].Candidate;
                LocalizedText localizedText = candidate.Text.GetComponent<LocalizedText>();
                if (localizedText == null)
                {
                    localizedText = Undo.AddComponent<LocalizedText>(candidate.Text.gameObject);
                }

                LocalizationEditorUtility.AssignKey(localizedText, bindings[i].Key);
                boundTexts++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            ScanScene();
            _issues = LocalizationEditorUtility.Validate(_profile);

            string result = $"Bound {boundTexts} TMP texts and added {addedRows} CSV rows.";
            if (errors.Count > 0)
            {
                result += "\n\n" + string.Join("\n", errors);
            }

            EditorUtility.DisplayDialog("Localization", result, "OK");
        }

        private List<LocalizationLocale> GetLocales()
        {
            return _profile.SupportedLocales
                .Where(locale => locale != null && !string.IsNullOrEmpty(locale.Code))
                .ToList();
        }

        private static MessageType ToMessageType(LocalizationIssueSeverity severity)
        {
            return severity switch
            {
                LocalizationIssueSeverity.Error => MessageType.Error,
                LocalizationIssueSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };
        }
    }
}
