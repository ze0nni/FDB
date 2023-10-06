using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public static class AutoRefWindow
    {
        internal struct NL0 { }
        internal struct NL1 { }
        internal struct NL2 { }
        internal struct NL3 { }
        internal struct NL4 { }
        internal struct NL5 { }
        internal struct NL6 { }
        internal struct NL7 { }
        internal struct NL8 { }
        internal struct NL9 { }
        internal struct NL10 { }
        internal struct NL11 { }
        internal struct NL12 { }
        internal struct NL13 { }
        internal struct NL14 { }
        internal struct NL15 { }

        public static PopupWindowContent New(
            PageContext context,
            object config,
            Type modelType,
            AutoRefAttribute autoRef,
            Ref currentField,
            Action<string> updateRef)
        {
            switch (context.WindowLevel)
            {
                case 0: return new AutoRefWindow<NL0>(context, config, modelType, autoRef, currentField, updateRef);
                case 1: return new AutoRefWindow<NL1>(context, config, modelType, autoRef, currentField, updateRef);
                case 2: return new AutoRefWindow<NL2>(context, config, modelType, autoRef, currentField, updateRef);
                case 3: return new AutoRefWindow<NL3>(context, config, modelType, autoRef, currentField, updateRef);
                case 4: return new AutoRefWindow<NL4>(context, config, modelType, autoRef, currentField, updateRef);
                case 5: return new AutoRefWindow<NL5>(context, config, modelType, autoRef, currentField, updateRef);
                case 6: return new AutoRefWindow<NL6>(context, config, modelType, autoRef, currentField, updateRef);
                case 7: return new AutoRefWindow<NL7>(context, config, modelType, autoRef, currentField, updateRef);
                case 8: return new AutoRefWindow<NL8>(context, config, modelType, autoRef, currentField, updateRef);
                case 9: return new AutoRefWindow<NL9>(context, config, modelType, autoRef, currentField, updateRef);
                case 10: return new AutoRefWindow<NL10>(context, config, modelType, autoRef, currentField, updateRef);
                case 11: return new AutoRefWindow<NL11>(context, config, modelType, autoRef, currentField, updateRef);
                case 12: return new AutoRefWindow<NL12>(context, config, modelType, autoRef, currentField, updateRef);
                case 13: return new AutoRefWindow<NL13>(context, config, modelType, autoRef, currentField, updateRef);
                case 14: return new AutoRefWindow<NL14>(context, config, modelType, autoRef, currentField, updateRef);
                case 15: return new AutoRefWindow<NL15>(context, config, modelType, autoRef, currentField, updateRef);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class AutoRefWindow<TNestLevel> : PopupWindowContent
    {
        public const float Width = 300;

        readonly PageContext _context;
        readonly object _config;
        readonly Type _modelType;
        readonly List<string> _errors;
        readonly Header[] _headers;
        readonly AutoRefAttribute _autoRef;
        Ref _currentRef;
        readonly Action<string> _updateRef;

        readonly string _linkedKind;
        readonly string _targetKind;
        Vector2 _scrollPosition;

        public AutoRefWindow(
            PageContext context,
            object config,
            Type modelType,
            AutoRefAttribute autoRef,
            Ref currentField,
            Action<string> updateRef)
        {
            _context = context.Nested(() => editorWindow.Repaint());
            _config = config;
            _modelType = modelType;
            _errors = new List<string>();
            _headers = Header.Of(_modelType, 0, "", true, _errors.Add).ToArray();
            _autoRef = autoRef;
            _currentRef = currentField;
            _updateRef = updateRef;

            _targetKind = autoRef == null ? null : autoRef.GetKind(config);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(Width, 300);
        }

        public override void OnGUI(Rect rect)
        {
            if (_errors.Count != 0)
            {
                foreach (var e in _errors)
                {
                    EditorGUILayout.HelpBox(e, MessageType.Error);
                }
                return;
            }

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
                _context.Resolver.GetConfig(_modelType, _targetKind);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(_targetKind);
                GUILayout.FlexibleSpace();
                if (_currentRef.Config == null && resolvedConfig == null)
                {
                    if (GUILayout.Button("Create"))
                    {
                        var index = _context.Resolver.GetIndex(_modelType);
                        var insertIndex = _autoRef.GetInsertIndex(_config, index);
                        var config = DBResolver.Instantate(_modelType, _targetKind);
                        index.Insert(insertIndex, config);

                        _currentRef = DBResolver.CreateRef(_context.Resolver, _modelType, config);
                        _updateRef(_currentRef.Kind.Value);
                    }
                }
                else if (_currentRef.Config == null && resolvedConfig != null)
                {
                    if (GUILayout.Button("Connect"))
                    {
                        _currentRef = DBResolver.CreateRef(_context.Resolver, _modelType, resolvedConfig);
                        _updateRef(_currentRef.Kind.Value);
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
                    foreach (var header in _headers)
                    {
                        if (header is KindHeader)
                        {
                            continue;
                        }
                        if (header is FieldHeader fieldHeader)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                var labelWidth = Width / 3;
                                GUILayout.Label(header.Title, GUILayout.Width(labelWidth));
                                var fieldHeight = header.GetFieldHeight(in _context, config);
                                var fieldRect = GUILayoutUtility.GetRect(0, fieldHeight, GUILayout.ExpandWidth(true));

                                EditorGUI.BeginChangeCheck();
                                header.OnGUI(in _context, fieldRect, config, null);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    _context.MakeDirty();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
