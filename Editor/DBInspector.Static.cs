using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

        bool InitStatic()
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

            if (_loadedModelType == typeof(T))
            {
                return ok;
            }

            try
            {
                InitStaticInternal();
                _loadedModelType = typeof(T);
                _fdbAttr = _loadedModelType.GetCustomAttribute<FuryDBAttribute>();
                _jsonAttr = _loadedModelType.GetCustomAttribute<JsonConverterAttribute>();
            } catch (Exception exc)
            {
                _staticException = exc;
                return false;
            }
            return true;
        }
        
        void InitStaticInternal()
        {
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
                    Headers = GetHeaders(modelType, 0, field.Name, true).ToArray(),
                    Aggregator = new Aggregator(typeof(T), field, modelType)
            });

                _indexes.Add(modelType, field);
            }

            _pageNames = indexList.Select(x => x.Title).ToArray();
            _pageStates = indexList.ToArray();
        }

        public bool TryResolveIndex(Type type, out Index index)
        {
            var field = _indexes[type];
            if (field == null)
            {
                index = default;
                return false;
            }
            index = field.GetValue(_state.Model) as Index;
            return index != null;
        }

        IEnumerable<HeaderState> GetHeaders(Type type, int depth, string rootPath, bool requestKind)
        {
            var kindResolved = false;

            foreach (var field in type.GetFields())
            {
                var path = $"{rootPath}/{field.Name}";

                if (field.FieldType.IsGenericType)
                {
                    var fieldGenericType = field.FieldType.GetGenericTypeDefinition();
                    if (fieldGenericType == typeof(Kind<>))
                    {
                        if (field.Name == "Kind" && field.FieldType.GetGenericArguments()[0] == type)
                        {
                            kindResolved = true;
                        }
                        yield return new KindFieldHeaderState(path, field);
                    }
                    else if (fieldGenericType == typeof(Ref<>))
                    {
                        var modelType = field.FieldType.GetGenericArguments()[0];
                        yield return new RefFieldHeaderState(path, field);
                    }
                    else if (fieldGenericType == typeof(List<>))
                    {
                        var listRoot = $"{path}/{field.Name}";
                        var itemType = field.FieldType.GetGenericArguments()[0];
                        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Ref<>))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new RefFieldHeaderState(listRoot, itemType.GetGenericArguments()[0]) });
                        } else if (itemType.IsEnum)
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new EnumFieldHeaderState(listRoot, itemType) });
                        } else if (itemType == typeof(bool)) {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new BoolFieldHeaderState(listRoot, null) });
                        }
                        else if (itemType == typeof(int))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new IntFieldHeaderState(listRoot, null) });
                        }
                        else if (itemType == typeof(float))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new FloatFieldHeaderState(listRoot, null) });
                        }
                        else if (itemType == typeof(string))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new StringFieldHeaderState(listRoot, null) });
                        } else { 
                            yield return new ListHeaderState(path, type, field, itemType, false,
                                GetHeaders(itemType, depth + 1, listRoot, false).ToArray());
                        }
                    }
                }
                else if (field.FieldType.IsEnum)
                {
                    yield return new EnumFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(bool))
                {
                    yield return new BoolFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(int))
                {
                    yield return new IntFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(float))
                {
                    yield return new FloatFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(string))
                {
                    yield return new StringFieldHeaderState(path, field);
                } else if (field.FieldType == typeof(AssetReference))
                {
                    yield return new AssetReferenceFieldHeaderState(path, field);
                }
            }

            if (requestKind && !kindResolved)
            {
                throw new ArgumentException($"Model {type.Name} must containst field Kind<{type.Name}>");
            }
        }
    }
}
