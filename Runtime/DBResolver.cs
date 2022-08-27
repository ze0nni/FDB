using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FDB
{
    public sealed class DBResolver
    {
        internal static DBResolver Current;

        public static T New<T>(out DBResolver resolver)
        {
            return Load<T>(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes("{}"))), out resolver);
        }

        public static T Load<T>(StreamReader reader, out DBResolver resolver)
        {
            var serializer = new JsonSerializer();

            resolver = new DBResolver();
            using (var jsonReader = new JsonTextReader(reader))
            {
                try
                {
                    Current = resolver;
                    return serializer.Deserialize<T>(jsonReader);
                } finally
                {
                    Current = null;
                }
            }
        }

        public static T FromResources<T>(string path)
        {
            var textAsset = Resources.Load<TextAsset>(path);
            var db = Load<T>(new StreamReader(new MemoryStream(textAsset.bytes)), out _);
            return db;
        }

        private Dictionary<Type, Index> _indexByType = new Dictionary<Type, Index>();
        readonly List<(object Model, FieldInfo Field, string RefValue)> _fields = new List<(object, FieldInfo, string)>();
        readonly Dictionary<object, List<string>> _listRef = new Dictionary<object, List<string>>();
        public object Model { get; private set; }

        internal void SetDB(object db)
        {
            foreach (var field in db.GetType().GetFields())
            {
                if (field.FieldType.IsGenericType
                    && field.FieldType.GetGenericTypeDefinition() == typeof(Index<>))
                {
                    var index = (Index)field.GetValue(db);
                    if (index == null)
                    {
                        index = (Index)Activator.CreateInstance(field.FieldType);
                        field.SetValue(db, index);
                    }

                    var modelType = field.FieldType.GetGenericArguments()[0];
                    _indexByType[modelType] = index;
                }
            }
        }

        internal void AddField(object model, FieldInfo field, string refValue)
        {
            _fields.Add((model, field, refValue));
        }

        internal void AddListRef(object list, string refValue)
        {
            if (!_listRef.TryGetValue(list, out var refs))
            {
                refs = new List<string>();
                _listRef.Add(list, refs);
            }
            refs.Add(refValue);
        }

        public static object Instantate(Type modelType)
        {
            var model = Activator.CreateInstance(modelType);
            Instantate(model);
            return model;
        }

        static void Instantate(object model)
        {
            foreach (var field in model.GetType().GetFields())
            {
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var list = field.GetValue(model);
                    if (list == null)
                    {
                        field.SetValue(model, Activator.CreateInstance(field.FieldType));
                    } else
                    {
                        foreach (var i in (IEnumerable)list)
                        {
                            Instantate(i);
                        }
                    }
                }
            }
        }

        internal void Resolve()
        {
            var _modelRefTypes = new Dictionary<FieldInfo, (Type RefType, Type ModelType)>();

            foreach (var i in _fields)
            {
                if (!_modelRefTypes.TryGetValue(i.Field, out var types)) {
                    var modelType = i.Field.FieldType.GetGenericArguments()[0];
                    var refType = typeof(Ref<>).MakeGenericType(modelType);
                    types = (refType, modelType);
                    _modelRefTypes[i.Field] = types;
                }
                var value = (Ref)Activator.CreateInstance(types.RefType, this, GetModel(types.ModelType, i.RefValue));
                i.Field.SetValue(i.Model, value);
            }
            _fields.Clear();

            foreach (var list in _listRef.Keys)
            {
                var refType = list.GetType().GetGenericArguments()[0];
                var modelType = refType.GetGenericArguments()[0];
                var refs = _listRef[list];
                var add = list.GetType().GetMethod("Add");                
                foreach (var r in refs)
                {
                    var value = (Ref)Activator.CreateInstance(refType, this, GetModel(modelType, r));
                    add.Invoke(list, new[] { value });
                }
            }
            _listRef.Clear();

            foreach (var index in _indexByType.Values)
            {
                foreach (var m in index.All())
                {
                    Instantate(m);
                }
            }
        }

        public Index GetIndex(Type indexType)
        {
            return _indexByType[indexType];
        }

        public object GetModel(Type modelType, string kind)
        {
            _indexByType[modelType].TryGet(kind, out var model);
            return model;
        }

        public void SetDirty()
        {
            foreach (var i in _indexByType.Values)
            {
                i.SetDirty();
            }
        }
    }
}
