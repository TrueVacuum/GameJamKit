using System.Collections.Generic;
using GameJamKit.Palettes;
using UnityEditor;
using UnityEngine;

namespace GameJamKit.Editor.Palettes
{
    [CustomEditor(typeof(PaletteColorBinder), true)]
    [CanEditMultipleObjects]
    public sealed class PaletteColorBinderEditor : UnityEditor.Editor
    {
        private SerializedProperty _controllerProperty;
        private SerializedProperty _colorKeyProperty;
        private SerializedProperty _fallbackColorProperty;
        private SerializedProperty _overrideAlphaProperty;
        private SerializedProperty _alphaProperty;
        private SerializedProperty _applyOnEnableProperty;
        private bool _showAdvancedSettings;

        private void OnEnable()
        {
            _controllerProperty = serializedObject.FindProperty("_controller");
            _colorKeyProperty = serializedObject.FindProperty("_colorKey");
            _fallbackColorProperty = serializedObject.FindProperty("_fallbackColor");
            _overrideAlphaProperty = serializedObject.FindProperty("_overrideAlpha");
            _alphaProperty = serializedObject.FindProperty("_alpha");
            _applyOnEnableProperty = serializedObject.FindProperty("_applyOnEnable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawColorKey();
            EditorGUILayout.PropertyField(_overrideAlphaProperty);

            if (_overrideAlphaProperty.hasMultipleDifferentValues ||
                _overrideAlphaProperty.boolValue)
            {
                EditorGUILayout.Slider(_alphaProperty, 0f, 1f);
            }

            EditorGUILayout.Space();
            _showAdvancedSettings = EditorGUILayout.Foldout(
                _showAdvancedSettings,
                "Advanced Settings",
                true);

            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_controllerProperty);
                EditorGUILayout.PropertyField(_applyOnEnableProperty);
                EditorGUILayout.PropertyField(_fallbackColorProperty);
                EditorGUI.indentLevel--;
            }

            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (!changed)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is PaletteColorBinder binder)
                {
                    binder.RefreshColor();
                    EditorUtility.SetDirty(binder);
                }
            }
        }

        private void DrawColorKey()
        {
            if (_colorKeyProperty.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(_colorKeyProperty);
                return;
            }

            ColorPalette palette = ResolvePalette();
            if (palette == null || palette.Count == 0)
            {
                EditorGUILayout.PropertyField(_colorKeyProperty);
                EditorGUILayout.HelpBox(
                    "Assign a controller with an active palette to select a color key.",
                    MessageType.Info);
                return;
            }

            List<string> keys = new List<string>();
            for (int i = 0; i < palette.Colors.Count; i++)
            {
                PaletteColor entry = palette.Colors[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key) || keys.Contains(entry.Key))
                {
                    continue;
                }

                keys.Add(entry.Key);
            }

            string currentKey = _colorKeyProperty.stringValue;
            int currentIndex = keys.IndexOf(currentKey);
            string[] options = new string[keys.Count + 1];
            options[0] = "<Select color>";

            for (int i = 0; i < keys.Count; i++)
            {
                options[i + 1] = keys[i];
            }

            int selectedIndex = EditorGUILayout.Popup(
                "Color Key",
                currentIndex >= 0 ? currentIndex + 1 : 0,
                options);

            if (selectedIndex > 0)
            {
                _colorKeyProperty.stringValue = keys[selectedIndex - 1];
            }

            if (!string.IsNullOrEmpty(currentKey) && currentIndex < 0)
            {
                EditorGUILayout.HelpBox(
                    $"The active palette does not contain '{currentKey}'.",
                    MessageType.Warning);
            }
        }

        private ColorPalette ResolvePalette()
        {
            ColorPaletteController controller =
                _controllerProperty.objectReferenceValue as ColorPaletteController;

            if (controller != null)
            {
                return controller.ActivePalette;
            }

            if (target is Component component)
            {
                controller = component.GetComponentInParent<ColorPaletteController>(true);
            }

            if (controller == null)
            {
                controller = Object.FindFirstObjectByType<ColorPaletteController>(
                    FindObjectsInactive.Include);
            }

            return controller != null ? controller.ActivePalette : null;
        }
    }
}
