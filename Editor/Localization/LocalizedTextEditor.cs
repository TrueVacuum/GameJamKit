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

namespace GameJamKit.Editor.Localization
{
    [CustomEditor(typeof(LocalizedText))]
    [CanEditMultipleObjects]
    public sealed class LocalizedTextEditor : UnityEditor.Editor
    {
        private SerializedProperty _key;
        private SerializedProperty _manager;
        private SerializedProperty _target;
        private LocalizationProfile _profile;
        private List<LocalizationCsvDocument> _documents = new List<LocalizationCsvDocument>();
        private List<string> _keys = new List<string>();
        private readonly AdvancedDropdownState _dropdownState = new AdvancedDropdownState();
        private bool _showAdvancedReferences;
        private bool _showCreateKey;
        private string _newKey;
        private int _sourceIndex;
        private int _sourceLocaleIndex;

        private void OnEnable()
        {
            _key = serializedObject.FindProperty("_key");
            _manager = serializedObject.FindProperty("_manager");
            _target = serializedObject.FindProperty("_target");
            RefreshData();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_key);
            DrawKeyTools();
            DrawTranslationPreview();
            DrawCreateKey();
            DrawAdvancedReferences();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawKeyTools()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_keys.Count == 0))
                {
                    if (GUILayout.Button("Browse Keys..."))
                    {
                        Rect buttonRect = GUILayoutUtility.GetLastRect();
                        LocalizationKeyDropdown dropdown = new LocalizationKeyDropdown(
                            _dropdownState,
                            _keys,
                            SelectKey);
                        dropdown.Show(buttonRect);
                    }
                }

                if (GUILayout.Button("Refresh CSV", GUILayout.Width(100f)))
                {
                    RefreshData();
                }
            }

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "No Localization Profile was found. Add a Localization Manager to the scene or assign one under Advanced References.",
                    MessageType.Info);
            }
        }

        private void DrawTranslationPreview()
        {
            if (_profile == null || string.IsNullOrWhiteSpace(_key.stringValue))
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Translation Preview", EditorStyles.boldLabel);

            IReadOnlyList<LocalizationLocale> locales = _profile.SupportedLocales;
            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < locales.Count; i++)
                {
                    LocalizationLocale locale = locales[i];
                    if (locale == null)
                    {
                        continue;
                    }

                    string translation = FindTranslation(_key.stringValue, locale.Code);
                    EditorGUILayout.TextField(
                        locale.DisplayName,
                        string.IsNullOrEmpty(translation) ? "<missing>" : translation);
                }
            }
        }

        private void DrawCreateKey()
        {
            if (targets.Length != 1 || _profile == null || _documents.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            _showCreateKey = EditorGUILayout.Foldout(
                _showCreateKey,
                "Create Key From Current Text",
                true);

            if (!_showCreateKey)
            {
                return;
            }

            TMP_Text text = ((LocalizedText)target).GetComponent<TMP_Text>();
            if (text == null)
            {
                EditorGUILayout.HelpBox("The target has no TMP Text component.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_newKey))
            {
                _newKey = LocalizationEditorUtility.SuggestKey(text, _keys);
            }

            EditorGUI.indentLevel++;
            _newKey = EditorGUILayout.TextField("New Key", _newKey);

            string[] sourceNames = _documents.Select(document => document.Asset.name).ToArray();
            _sourceIndex = Mathf.Clamp(_sourceIndex, 0, sourceNames.Length - 1);
            _sourceIndex = EditorGUILayout.Popup("Target CSV", _sourceIndex, sourceNames);

            List<LocalizationLocale> locales = _profile.SupportedLocales
                .Where(locale => locale != null && !string.IsNullOrEmpty(locale.Code))
                .ToList();
            string[] localeNames = locales.Select(locale => locale.DisplayName).ToArray();
            int defaultIndex = locales.FindIndex(locale => string.Equals(
                locale.Code,
                _profile.DefaultLocaleCode,
                StringComparison.OrdinalIgnoreCase));

            if (_sourceLocaleIndex < 0 || _sourceLocaleIndex >= locales.Count)
            {
                _sourceLocaleIndex = Mathf.Max(0, defaultIndex);
            }

            _sourceLocaleIndex = EditorGUILayout.Popup(
                "Current Text Language",
                _sourceLocaleIndex,
                localeNames);

            using (new EditorGUI.DisabledScope(
                       string.IsNullOrWhiteSpace(_newKey) || locales.Count == 0))
            {
                if (GUILayout.Button("Create and Assign"))
                {
                    CreateAndAssign(text, locales[_sourceLocaleIndex].Code);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawAdvancedReferences()
        {
            EditorGUILayout.Space();
            _showAdvancedReferences = EditorGUILayout.Foldout(
                _showAdvancedReferences,
                "Advanced References",
                true);

            if (!_showAdvancedReferences)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_manager);
            EditorGUILayout.PropertyField(_target);
            bool referencesChanged = EditorGUI.EndChangeCheck();
            EditorGUI.indentLevel--;

            if (referencesChanged)
            {
                serializedObject.ApplyModifiedProperties();
                RefreshData();
                serializedObject.Update();
            }
        }

        private void SelectKey(string key)
        {
            Undo.RecordObjects(targets, "Select Localization Key");
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is LocalizedText localizedText)
                {
                    LocalizationEditorUtility.AssignKey(localizedText, key);
                }
            }

            serializedObject.Update();
            Repaint();
        }

        private void CreateAndAssign(TMP_Text text, string sourceLocaleCode)
        {
            LocalizationCsvDocument document = _documents[_sourceIndex];
            if (!document.AddKey(_newKey, sourceLocaleCode, text.text, out string error))
            {
                EditorUtility.DisplayDialog("Create Localization Key", error, "OK");
                return;
            }

            try
            {
                document.Save();
            }
            catch (IOException exception)
            {
                EditorUtility.DisplayDialog(
                    "Create Localization Key",
                    $"Could not write the CSV. Close it in spreadsheet software and try again.\n\n{exception.Message}",
                    "OK");
                return;
            }

            serializedObject.Update();
            _key.stringValue = _newKey.Trim();
            serializedObject.ApplyModifiedProperties();
            _newKey = string.Empty;
            RefreshData();
        }

        private string FindTranslation(string key, string localeCode)
        {
            for (int i = 0; i < _documents.Count; i++)
            {
                string translation = _documents[i].GetTranslation(key, localeCode);
                if (translation != null)
                {
                    return translation;
                }
            }

            return null;
        }

        private void RefreshData()
        {
            LocalizationManager preferredManager = _manager != null
                ? _manager.objectReferenceValue as LocalizationManager
                : null;
            _profile = LocalizationEditorUtility.FindProfile(preferredManager);
            _documents = LocalizationEditorUtility.LoadDocuments(_profile);
            _keys = LocalizationEditorUtility.GetKeys(_documents);
        }
    }
}
