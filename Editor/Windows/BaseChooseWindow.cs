using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FDB.Editor
{
    public abstract class BaseChooseWindow<T> : PopupWindowContent
        where T : class
    {
        TextField _searchField;
        ListView _listView;
        int _scrollToIndex;

        T _selected;
        T[] _allSources;
        T[] _sources;
        Action<T> _onSelect;

        public BaseChooseWindow(
            T selected,
            IEnumerable<T> sources,
            Action<T> onSelect)
        {
            _selected = selected;
            _allSources = sources.ToArray();
            _onSelect = onSelect;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 300);
        }

        public override void OnOpen()
        {
            _searchField = new TextField();
            _searchField.RegisterValueChangedCallback(e => Filter());
            editorWindow.rootVisualElement.Add(_searchField);

            _listView = new ListView();
            _listView.makeItem = MakeObjectItem;
            _listView.bindItem = BindObjectItem;
            _listView.unbindItem = UnblindObjectItem;
            editorWindow.rootVisualElement.Add(_listView);

            Filter();

            ScrollToItem(_selected);
        }

        protected void Filter()
        {
            var search = _searchField.value;

            _sources = _allSources.Where(item =>
            {
                return Filter(item, search);
            }).ToArray();

            _listView.itemsSource = _sources;
            _listView.Rebuild();
        }

        protected abstract bool Filter(T item, string text);
        protected abstract string ItemText(T item);

        protected void ScrollToItem(T item)
        {
            _scrollToIndex = Array.FindIndex(_sources, i => i == item);
        }

        protected void Select(T item)
        {
            _onSelect(item);
            this.editorWindow.Close();
        }

        public override void OnGUI(Rect rect)
        {
            if (_scrollToIndex != -1)
            {
                _listView.ScrollToItem(_scrollToIndex);
                _listView.selectedIndex = _scrollToIndex;
                _scrollToIndex = -1;
            }
        }

        private VisualElement MakeObjectItem()
        {
            return new StringElement(Select);
        }

        private void BindObjectItem(VisualElement e, int index)
        {
            var item = _sources[index];
            ((StringElement)e).Bind(item, ItemText(item));
        }

        private void UnblindObjectItem(VisualElement e, int index)
        {
            ((StringElement)e).Unbind();
        }

        public class StringElement : VisualElement
        {
            readonly Action<T> _onSelect;

            readonly Label _label;

            public StringElement(Action<T> onSelect)
            {
                _onSelect = onSelect;

                _label = new Label();
                _label.pickingMode = PickingMode.Ignore;
                _label.style.marginTop = 4;
                Add(_label);
            }

            T _value;
            public void Bind(T value, string text)
            {
                _value = value;
                _label.text = text;
            }

            public void Unbind()
            {
                _value = null;
            }

            public override void HandleEvent(EventBase evt)
            {
                base.HandleEvent(evt);
                if (evt is MouseDownEvent mouseDown && mouseDown.button == 0 && mouseDown.clickCount == 2)
                {
                    _onSelect(_value);
                }
            }
        }
    }
}