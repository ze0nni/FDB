using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public partial class DBInspector<T>
    {
        Exception _staticException;
        Type _loadedModelType;
        FuryDBAttribute _fdbAttr;
        JsonConverterAttribute _jsonAttr;
        string[] _pageNames;
        PageState[] _pageStates;
        Dictionary<Type, FieldInfo> _indexes = new Dictionary<Type, FieldInfo>();

        bool OnValidateGUI()
        {
            var ok = true;
            if (_staticException != null)
            {
                ok = false;
                GUILayout.Label(_staticException.ToString(), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }

            if (_fdbAttr == null)
            {
                ok = false;
                GUILayout.Label("Add attribute");
                GUILayout.TextField("[FuryDB(\"Assets/Resources/DB.json.txt\", \"Assets/Kinds.cs\")]");
            }

            if (_jsonAttr == null
                || !_jsonAttr.ConverterType.IsGenericType
                || _jsonAttr.ConverterType.GetGenericTypeDefinition() != typeof(DBConverter<>)
                || _jsonAttr.ConverterType.GetGenericArguments()[0] != typeof(T))
            {
                ok = false;
                GUILayout.Label("Add attribute");
                GUILayout.TextField("[JsonConverter(typeof(DBConverter<" + typeof(T).Name + ">))]");
            }

            if (_loadedModelType != typeof(T))
            {
                ok = false;
            }
            return ok;
        }
        
        void InitStatic()
        {
            try
            {
                _loadedModelType = typeof(T);
                _fdbAttr = _loadedModelType.GetCustomAttribute<FuryDBAttribute>();
                _jsonAttr = _loadedModelType.GetCustomAttribute<JsonConverterAttribute>();

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

                    var modelType = fieldType.GetGenericArguments()[0];

                    indexList.Add(new PageState
                    {
                        Title = field.Name,
                        IndexType = fieldType,
                        ModelType = modelType,
                        ResolveModel = x => field.GetValue(x),
                        Headers = HeaderState.Of(modelType, 0, field.Name, true).ToArray(),
                        Aggregator = new Aggregator(typeof(T), field, modelType)
                    });

                    _indexes.Add(modelType, field);
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
