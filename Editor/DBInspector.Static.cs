using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public partial class DBInspector<T>
    {
        Exception _staticException;
        List<string> _errors = new List<string>();
        bool _isCorrenExt;
        Type _loadedModelType;
        FuryDBAttribute _fdbAttr;
        string[] _pageNames;
        PageState[] _pageStates;
        Dictionary<Type, FieldInfo> _indexes = new Dictionary<Type, FieldInfo>();

        bool OnValidateGUI()
        {
            var ok = true;
            if (_staticException != null)
            {
                ok = false;
                EditorGUILayout.HelpBox(
                    _staticException.ToString(), MessageType.Error);
            }

            if (_errors.Count > 0)
            {
                ok = false;
                foreach (var e in _errors)
                {
                    EditorGUILayout.HelpBox(e, MessageType.Error);
                }
            }

            if (_fdbAttr == null || string.IsNullOrWhiteSpace(_fdbAttr.SourcePath))
            {
                ok = false;
                GUILayout.Label("Add attribute");
                GUILayout.TextField($"[FuryDB(\"Assets/Resources/DB{DBResolver.DBExt}\", \"Assets/Kinds.cs\")]");
            }
            if (_fdbAttr != null && !_isCorrenExt)
            {
                ok = false;
                var currentExt = Path.GetExtension(_fdbAttr.SourcePath);
                if (_fdbAttr.SourcePath.EndsWith(".json.txt"))
                {
                    currentExt = ".json.txt";
                }
                var sourceNameWoExt = _fdbAttr.SourcePath.Substring(0, _fdbAttr.SourcePath.Length - currentExt.Length);

                EditorGUILayout.HelpBox($"Your main database file has wrong extension {currentExt} excepted {DBResolver.DBExt}", MessageType.Error);
                GUILayout.Label(
                    $"[FuryDB(\"{sourceNameWoExt}<color=red>{currentExt}</color>\", ...]",
                    FDBEditorStyles.RichTextLabel);
                GUILayout.Label(
                    $"[FuryDB(\"{sourceNameWoExt}<color=green>{DBResolver.DBExt}</color>\", ...]",
                    FDBEditorStyles.RichTextLabel);
            }

            if (_loadedModelType != typeof(T))
            {
                ok = false;
                EditorGUILayout.HelpBox($"DB types not matched{_loadedModelType} and {typeof(T)}", MessageType.Error);
            }

            if (ok) {
                try
                {
                    var db = EditorDB<T>.DB;
                } catch (Exception exc)
                {
                    ok = false;
                    EditorGUILayout.HelpBox($"Error when load {_fdbAttr.SourcePath}\n {exc.Message}", MessageType.Error);
                    Debug.LogException(exc);
                }
            }

            return ok;
        }
        
        void InitStatic()
        {
            try
            {
                _staticException = null;
                _errors.Clear();

                _loadedModelType = typeof(T);
                _fdbAttr = _loadedModelType.GetCustomAttribute<FuryDBAttribute>();
                if (_fdbAttr != null)
                {
                    _isCorrenExt = _fdbAttr.SourcePath != null && _fdbAttr.SourcePath.EndsWith(DBResolver.DBExt);
                }

                _indexes.Clear();
                var indexList = new List<PageState>();

                foreach (var field in typeof(T).GetFields())
                {
                    var fieldType = field.FieldType;
                    if (!fieldType.IsGenericType)
                    {
                        continue;
                    }
                    var genericType = fieldType.GetGenericTypeDefinition();
                    if (genericType != typeof(Index<>))
                    {
                        continue;
                    }

                    var configType = fieldType.GetGenericArguments()[0];
                    if (_indexes.ContainsKey(configType))
                    {
                        _errors.Add($"Fields {_indexes[configType].Name} and {field.Name} has same type Index<{configType.Name}> ");
                        continue;
                    }

                    var errors = new List<string>();
                    var headers = HeaderState.Of(configType, 0, field.Name, true, errors.Add).ToArray();

                    indexList.Add(new PageState
                    {
                        Title = field.Name,
                        IndexType = fieldType,
                        ModelType = configType,
                        ResolveModel = x => field.GetValue(x),
                        Headers = headers,
                        Errors = errors,
                        Aggregator = new Aggregator(typeof(T), field, configType)
                    });

                    _indexes.Add(configType, field);
                }

                _pageNames = indexList.Select(x => x.Title).ToArray();
                _pageStates = indexList.ToArray();
            }
            catch (Exception exc)
            {
                _staticException = exc;
            }
}

        public bool TryResolveIndex(Type type, out Index index)
        {
            var field = _indexes[type];
            if (field == null)
            {
                index = default;
                return false;
            }
            index = field.GetValue(EditorDB<T>.DB) as Index;
            return index != null;
        }
    }
}
