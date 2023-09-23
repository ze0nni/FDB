using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public class AutoRefWindow : PopupWindowContent
    {
        readonly object _owner;
        readonly DBResolver _resolver;
        readonly Type _modelType;
        readonly HeaderState[] _modelHeaders;
        readonly AutoRefAttribute _autoRef;
        Ref _currentRef;
        readonly float _width;
        readonly Action _makeDirty;
        readonly Action<Ref> _updateRef;

        readonly string _linkedKind;
        readonly string _targetKind;
        Vector2 _scrollPosition;

        public AutoRefWindow(
            DBResolver resolver,
            object owner,
            Type modelType,
            AutoRefAttribute autoRef,
            Ref currentField,
            float width,
            Action makeDirty,
            Action<Ref> updateRef)
        {
            _owner = owner;
            _resolver = resolver;
            _modelType = modelType;
            _modelHeaders = HeaderState.Of(_modelType, 0, "", true).ToArray();
            _autoRef = autoRef;
            _currentRef = currentField;
            _width = width;
            _makeDirty = makeDirty;
            _updateRef = updateRef;

            _targetKind = autoRef == null ? null : autoRef.GetKind(owner);
        }

        public override Vector2 GetWindowSize()
        {
            var size = base.GetWindowSize();
            size.x = _width * 2;
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            if (_currentRef.Config != null)
            {
                OnConfigGUI(_currentRef.Config, true);
                return;
            }

            if (_autoRef == null)
            {
                return;
            }

            OnAutoRefGUI(out var config);
            if (config != null) {
                OnConfigGUI(config, _targetKind == _currentRef.Kind.Value);
            }
        }

        void OnAutoRefGUI(out object resolvedConfig)
        {
            if (_targetKind == null)
            {
                GUILayout.Label("Can't resolve kind");
                resolvedConfig = default;
                return;
            }
            resolvedConfig =
                _resolver.GetConfig(_modelType, _targetKind);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(_targetKind);
                GUILayout.FlexibleSpace();
                if (_currentRef.Config == null && resolvedConfig == null)
                {
                    if (GUILayout.Button("Create"))
                    {
                        var index = _resolver.GetIndex(_modelType);
                        var insertIndex = _autoRef.GetInsertIndex(_owner, index);
                        var config = DBResolver.Instantate(_modelType, _targetKind);
                        index.Insert(insertIndex, config);

                        _currentRef = DBResolver.CreateRef(_resolver, _modelType, config);
                        _updateRef(_currentRef);
                    }
                }
                else if (_currentRef.Config == null && resolvedConfig != null)
                {
                    if (GUILayout.Button("Connect"))
                    {
                        _currentRef = DBResolver.CreateRef(_resolver, _modelType, resolvedConfig);
                        _updateRef(_currentRef);
                        _makeDirty?.Invoke();
                    }
                }
            }
        }

        void OnConfigGUI(object config, bool enabled)
        {
            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                using (new EditorGUI.DisabledScope(!enabled))
                {
                    foreach (var header in _modelHeaders)
                    {
                        if (header is KindFieldHeaderState)
                        {
                            continue;
                        }

                        if (header is FieldHeaderState fieldHeader)
                        {
                            header.ExpandWidth = true;
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(header.Title, GUILayout.Width(_width / 2));
                                var value = fieldHeader.Field.GetValue(config);
                                var newValue = Inspector.Field(_resolver, header, config, value, true, _makeDirty);
                                if (!value.Equals(newValue))
                                {
                                    fieldHeader.Field.SetValue(config, newValue);
                                    GUI.changed = true;
                                    _makeDirty?.Invoke();
                                }
                            }
                        }
                    }
                    _scrollPosition = scrollView.scrollPosition;
                }
            }
        }
    }
}
