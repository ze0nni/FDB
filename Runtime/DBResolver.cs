using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FDB
{
    public sealed class DBResolver
    {
        internal static DBResolver Current;

        public static T New<T>(out DBResolver resolver)
        {
            return Load<T>(new StreamReader("{}"), out resolver);
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

        private Dictionary<Type, Index> _indexByType = new Dictionary<Type, Index>();
        readonly List<(object Model, FieldInfo Field, string RefValue)> _fields =
            new List<(object, FieldInfo, string)>();
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
                i.Field.SetValue(i.Model, Activator.CreateInstance(types.RefType, this, GetModel(types.ModelType, i.RefValue)));
            }
            _fields.Clear();
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
