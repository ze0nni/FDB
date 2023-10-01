using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    internal struct NestLevel0 { }
    internal struct NestLevel1 { }
    internal struct NestLevel2 { }
    internal struct NestLevel3 { }
    internal struct NestLevel4 { }
    internal struct NestLevel5 { }
    internal struct NestLevel6 { }
    internal struct NestLevel7 { }

    internal class AutoRefWindow<TNestLevel> : PopupWindowContent
    {
        readonly PageContext _context;
        readonly object _config;
        readonly Type _modelType;
        readonly List<string> _errors;
        readonly HeaderState[] _modelHeaders;
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
            _context = context;
            _config = config;
            _modelType = modelType;
            _errors = new List<string>();
            _modelHeaders = HeaderState.Of(_modelType, 0, "", true, _errors.Add).ToArray();
            _autoRef = autoRef;
            _currentRef = currentField;
            _updateRef = updateRef;

            _targetKind = autoRef == null ? null : autoRef.GetKind(config);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 300);
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
                    
                }
            }
        }
    }
}
