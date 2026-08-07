using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace GameJamKit.Editor.Localization
{
    internal sealed class LocalizationKeyDropdown : AdvancedDropdown
    {
        private sealed class KeyItem : AdvancedDropdownItem
        {
            public KeyItem(string key)
                : base(key)
            {
                Key = key;
            }

            public string Key { get; }
        }

        private readonly IReadOnlyList<string> _keys;
        private readonly Action<string> _selected;

        public LocalizationKeyDropdown(
            AdvancedDropdownState state,
            IReadOnlyList<string> keys,
            Action<string> selected)
            : base(state)
        {
            _keys = keys;
            _selected = selected;
            minimumSize = new UnityEngine.Vector2(420f, 320f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Localization Keys");
            for (int i = 0; i < _keys.Count; i++)
            {
                root.AddChild(new KeyItem(_keys[i]));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is KeyItem keyItem)
            {
                _selected?.Invoke(keyItem.Key);
            }
        }
    }
}
