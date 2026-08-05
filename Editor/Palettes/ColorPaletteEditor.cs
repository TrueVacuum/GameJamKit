using System;
using System.Collections.Generic;
using GameJamKit.Palettes;
using UnityEditor;

namespace GameJamKit.Editor.Palettes
{
    [CustomEditor(typeof(ColorPalette))]
    public sealed class ColorPaletteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            ColorPalette palette = (ColorPalette)target;
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < palette.Colors.Count; i++)
            {
                PaletteColor entry = palette.Colors[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    EditorGUILayout.HelpBox(
                        $"Entry {i} has an empty key and will be ignored.",
                        MessageType.Warning);
                    continue;
                }

                if (!keys.Add(entry.Key))
                {
                    EditorGUILayout.HelpBox(
                        $"Duplicate key '{entry.Key}'. The final entry wins.",
                        MessageType.Warning);
                }
            }
        }
    }
}
