using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB
{
    public sealed partial class DBResolver
    {
        public const string DBExt = ".furydb";

        public static T New<T>(out DBResolver resolver)
        {
            return LoadInternal<T>(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes("{}"))), null, out resolver);
        }

        public static T LoadInternal<T>(
            StreamReader tReader,
            DBConverter.UnityResolverDelegate unityResolver,
            out DBResolver resolver)
        {
            resolver = new DBResolver();
            using (var jReader = new JsonTextReader(tReader))
            {
                var dbConverter = new DBConverter(typeof(T), resolver, unityResolver);
                jReader.Read();
                return (T)dbConverter.Read(jReader);
            }
        }

        public static T Load<T>()
        {
            var fdb = typeof(T).GetCustomAttribute<FuryDBAttribute>();
            var fullPath = fdb.SourcePath;
            var pattern = new Regex(@"[\\\/]Resources[\\\/](.*)");
            var match = pattern.Match(fullPath);
            if (!match.Success)
            {
                throw new IOException("DB not constraint in Resources folder");
            }
            return LoadFromResources<T>(match.Groups[1].Value);
        }

        public static T LoadFromResources<T>(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var name = path.Substring(0, path.Length - ext.Length);
            switch (ext)
            {
                case ".txt":
                case ".json":
                    {
                        var textAsset = Resources.Load<TextAsset>(name);
                        if (textAsset == null)
                        {
                            throw new NullReferenceException($"Can't resolve resource '{name}' with type {nameof(TextAsset)}");
                        }
                        return Load<T>(textAsset);
                    }
                case DBExt:
                    {
                        var fdbAsset = Resources.Load<FuryDBAsset>(name);
                        if (fdbAsset == null)
                        {
                            throw new NullReferenceException($"Can't resolve resource '{name}' with type {nameof(FuryDBAsset)}");
                        }
                        return Load<T>(fdbAsset);
                    }
                default:
                    throw new ArgumentOutOfRangeException($"Invalid file extension of file {name}");

            }
            //return db;
        }

        public static T Load<T>(TextAsset textAsset)
        {
            return (T) LoadInternal(typeof(T),
                new StreamReader(new MemoryStream(textAsset.bytes)),
                null,
                out _);
        }

        public static T Load<T>(FuryDBAsset fdbAsset)
        {
            return (T)LoadInternal(typeof(T),
                new StreamReader(new MemoryStream(fdbAsset.JsonData)),
                fdbAsset.ResolveDependency,
                out _);
        }

        public static object LoadInternal(
            Type dbType,
            StreamReader tReader,
            DBConverter.UnityResolverDelegate unityResolver,
            out DBResolver resolver)
        {
            resolver = new DBResolver();
            using (var jReader = new JsonTextReader(tReader))
            {
                var dbConverter = new DBConverter(dbType, resolver, unityResolver);
                jReader.Read();
                return dbConverter.Read(jReader);
            }
        }

        private List<Index> _indexes = new List<Index>();
        private List<string> _indexeNames = new List<string>();
        private Dictionary<Type, Index> _indexByType = new Dictionary<Type, Index>();
        readonly List<(object Model, FieldInfo Field, string RefValue)> _fields = new List<(object, FieldInfo, string)>();
        readonly Dictionary<object, List<string>> _listRef = new Dictionary<object, List<string>>();
        public object DB { get; private set; }
        public IReadOnlyList<Index> Indexes => _indexes;
        public IReadOnlyList<string> IndexeNames => _indexeNames;

        public Dictionary<string, List<FuryDBEntryAsset.DependencyRecord>> _entryDependency = 
            new Dictionary<string, List<FuryDBEntryAsset.DependencyRecord>>();

        internal void SetDB(object db)
        {
            DB = db;
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
                    _indexes.Add(index);
                    _indexeNames.Add(field.Name);
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

        internal void AddUnityDependency(string entryName, string guid, UnityEngine.Object obj)
        {
            if (!_entryDependency.TryGetValue(entryName, out var list))
            {
                list = new List<FuryDBEntryAsset.DependencyRecord>();
                _entryDependency.Add(entryName, list);
            }
            list.Add(new FuryDBEntryAsset.DependencyRecord
            {
                GUID = guid,
                Object = obj
            });
        }

        public IEnumerable<FuryDBEntryAsset.DependencyRecord> GetDependency(string entryName)
        {
            if (!_entryDependency.TryGetValue(entryName, out var list))
            {
                yield break ;
            }
            foreach (var i in list)
            {
                yield return i;
            }
        }

        public static bool IsNoWrap(Type type)
        {
            return
                type.IsEnum
                || type.IsPrimitive
                || type == typeof(Ref<>)
                || typeof(UnionBase).IsAssignableFrom(type)
                || type == typeof(bool)
                || type == typeof(int)
                || type == typeof(float)
                || type == typeof(string)
                || type == typeof(AssetReference)
                || IsSupportedUnityType(type);
        }

        public static object Instantate(Type modelType, bool asNew)
        {
            if (modelType == typeof(string))
                return "";
            if (modelType == typeof(Color))
                return Color.white;

            var model = Activator.CreateInstance(Wrap(modelType));
            Instantate(model, asNew);
            if (asNew)
            {
                Invalidate(model);
            }
            return model;
        }

        public static object Instantate(Type modelType, string kindValue)
        {
            var model = Activator.CreateInstance(Wrap(modelType));
            Instantate(model, true);

            var kindType = typeof(Kind<>).MakeGenericType(modelType);
            var kind = (Kind)Activator.CreateInstance(kindType, new object[] { kindValue });
            var kindField = modelType.GetField("Kind");
            kindField.SetValue(model, kind);

            return model;
        }

        public static T Instantate<T>()
        {
            var type = Wrap(typeof(T));
            var model = (T)Activator.CreateInstance(type);
            Instantate(model, true);
            return model;
        }

        static void Instantate(object model, bool asNew)
        {
            foreach (var field in model.GetType().GetFields())
            {
                if (field.FieldType == typeof(string) && field.GetValue(model) == null)
                {
                    field.SetValue(model, string.Empty);
                }

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
                            if (i != null)
                            {
                                Instantate(i, asNew);
                            }
                        }
                    }
                }

                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Index<>))
                {
                    var index = field.GetValue(model);
                    if (index == null)
                    {
                        field.SetValue(model, Activator.CreateInstance(field.FieldType));
                    }
                }

                if (field.FieldType == typeof(AnimationCurve) && field.GetValue(model) == null)
                {
                    field.SetValue(model, new AnimationCurve());
                }

                if (typeof(UnionBase).IsAssignableFrom(field.FieldType) && field.GetValue(model) == null)
                {
                    field.SetValue(model, Activator.CreateInstance(field.FieldType));
                }

                if (asNew)
                {
                    if (field.FieldType == typeof(Color))
                    {
                        field.SetValue(model, Color.black);
                    }
                }
            }
        }

        public static Ref CreateRef(DBResolver resolver, Type configType, object config)
        {
            var refType = typeof(Ref<>).MakeGenericType(configType);
            return (Ref)Activator.CreateInstance(refType, new object[] { resolver, config });
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
                var value = (Ref)Activator.CreateInstance(types.RefType, this, GetConfig(types.ModelType, i.RefValue));
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
                    var value = (Ref)Activator.CreateInstance(refType, this, GetConfig(modelType, r));
                    add.Invoke(list, new[] { value });
                }
            }
            _listRef.Clear();

            foreach (var index in _indexByType.Values)
            {
                foreach (var m in index.All())
                {
                    Instantate(m, false);
                }
            }
        }

        public Index GetIndex(Type indexType)
        {
            _indexByType.TryGetValue(indexType, out var index);
            return index;
        }

        public object GetConfig<T>(string kind)
        {
            return GetConfig(typeof(T), kind);
        }

        public object GetConfig(Type configType, string kind)
        {
            if (!_indexByType.TryGetValue(configType, out var index))
            {
                return null;
            }
            index.TryGet(kind, out var config);
            return config;
        }

        public void SetDirty()
        {
            foreach (var i in _indexByType.Values)
            {
                i.SetDirty();
            }
        }

        public static bool IsSupportedUnityType(Type type)
        {
            return 
                type != typeof(AssetReference)
                && type != typeof(AssetReferenceT<>)
                && typeof(UnityEngine.Object).IsAssignableFrom(type);
        }
    }
}
