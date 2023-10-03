using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FDB.Editor
{
    public class ChooseUnityObjectWindow : PopupWindowContent
    {
        public struct Item
        {
            public string Path;
            public UnityEngine.Object Object;
        }

        readonly static Item[] None = new Item[] { new Item() };
        UnityEngine.Object _current;
        Action<UnityEngine.Object> _onSelect;

        Item[] _allSources;
        Item[] _sources;

        TextField _searchField;
        ListView _listView;
        Toggle _includePackages;
        int _scrollToIndex;

        public ChooseUnityObjectWindow(
            UnityEngine.Object current,
            Type assetType,
            Action<UnityEngine.Object> onSelect)
        {
            _current = current;
            _onSelect = onSelect;

            var isComponent = typeof(Component).IsAssignableFrom(assetType);
            Type filterType = isComponent
                ? typeof(GameObject)
                : assetType;

            _allSources = AssetDatabase
                .FindAssets($"t:{filterType.Name}")
                .Select(guid =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var obj = AssetDatabase.LoadAssetAtPath(path, filterType);
                    if (isComponent)
                    {
                        if (obj is GameObject go && go.TryGetComponent(assetType, out var component))
                        {
                            return new Item
                            {
                                Path = path,
                                Object = component
                            };
                        }
                        return default;
                    }
                    return new Item
                    {
                        Path = path,
                        Object = obj
                    };
                }).Where(item => item.Object != null)
                .ToArray();
        }

        private void SetResult(UnityEngine.Object result)
        {
            _onSelect(result);
            editorWindow.Close();
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

            _includePackages = new Toggle("Include packages");
            _includePackages.RegisterValueChangedCallback(e => Filter());
            _includePackages.style.paddingBottom = 16;
            editorWindow.rootVisualElement.Add(_includePackages);

            _listView = new ListView();
            _listView.makeItem = MakeObjectItem;
            _listView.bindItem = BindObjectItem;
            _listView.unbindItem = UnblindObjectItem;
            editorWindow.rootVisualElement.Add(_listView);

            Filter();

            _scrollToIndex = Array.FindIndex(_sources, i => i.Object == _current);
        }

        public void Filter()
        {
            var search = _searchField.value;
            var includePackages = _includePackages.value;

            _sources = None.Concat(_allSources.Where(item =>
            {
                if (!includePackages && item.Path.StartsWith("Packages"))
                {
                    return false;
                }
                if (search.Length != 0 && !item.Object.name.Contains(search))
                {
                    return false;
                }
                return true;
            })).ToArray();

            _listView.itemsSource = _sources;
            _listView.Rebuild();
        }

        private VisualElement MakeObjectItem()
        {
            return new ObjectElement();
        }

        private void BindObjectItem(VisualElement e, int index)
        {
            var element = (ObjectElement)e;
            var item = _sources[index];
            element.Bind(this, item, index);
        }

        private void UnblindObjectItem(VisualElement e, int index)
        {
            var element = (ObjectElement)e;
            element.Unbind();
        }

        public class ObjectElement : VisualElement
        {
            public Image _image;
            public Label _label;

            public ObjectElement()
            {
                style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

                _image = new Image();
                _image.pickingMode = PickingMode.Ignore;
                _image.style.width = new StyleLength(new Length(32, LengthUnit.Pixel));
                _image.style.minWidth = new StyleLength(new Length(32, LengthUnit.Pixel));
                Add(_image);

                _label = new Label();
                _label.pickingMode = PickingMode.Ignore;
                _label.style.marginTop = 4;
                Add(_label);
            }

            ChooseUnityObjectWindow _window;
            UnityEngine.Object _object;

            public void Bind(ChooseUnityObjectWindow window, Item item, int index)
            {
                _window = window;
                _object = item.Object;
                _image.image = item.Object == null ? null : AssetDatabase.GetCachedIcon(item.Path);
                _label.text = item.Object == null ? "Null" : item.Object.name;
            }

            public void Unbind()
            {
                _window = null;
                _object = null;
            }

            public override void HandleEvent(EventBase evt)
            {
                base.HandleEvent(evt);
                if (evt is MouseDownEvent mouseDown && mouseDown.button == 0 && mouseDown.clickCount == 2)
                {
                    _window.SetResult(_object);
                }
            }
        }

        public override void OnGUI(Rect rect)
        {
            if (_scrollToIndex != -1)
            {
                _listView.selectedIndex = _scrollToIndex;
                _listView.ScrollToItem(_scrollToIndex);
                _scrollToIndex = -1;
            }
        }
    }
}