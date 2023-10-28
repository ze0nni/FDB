using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    internal static class EditorDB
    {
        public static UnityEngine.Object EditorUnityObjectsResolver(string guid, Type type)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath(path, type);
        }
    }

    public partial class EditorDB<T>
    {
        public static long Version { get; private set; }
        public static bool IsDirty { get; private set; }

        static T _db;
        static DBResolver _resolver;
        static (string Path, IFuryGenerator<T> Generator)[] _generators;

        static FuryDBAttribute MetaData
        {
            get {
                foreach (var attr in Attribute.GetCustomAttributes(typeof(T)))
                {
                    switch (attr)
                    {
                        case FuryDBAttribute model:
                            return model;
                    }
                }
                throw new ArgumentException($"Type {typeof(T).FullName} must have attribute {nameof(FuryDBAttribute)}");
            }
        }

        public static T DB {
            get {
                if (_db == null)
                {
                    try
                    {
                        if (File.Exists(MetaData.SourcePath))
                        {
                            _db = Load(out _resolver);
                            _generators = ResolveGenerators().ToArray();
                        }
                        else
                        {
                            _db = DBResolver.New<T>(out _resolver);
                            _generators = ResolveGenerators().ToArray();
                        }
                    } catch
                    {
                        _db = default;
                        _resolver = default;
                        _generators = default;
                        throw;
                    }
                }
                return _db;
            }
        }

        public static DBResolver Resolver
        {
            get
            {
                if (_resolver == null)
                {
                    _ = DB;
                }
                return _resolver;
            }
        }

        public static void SetDirty()
        {
            Version++;
            IsDirty = true;
            _resolver.SetDirty();
        }

        public static void Load()
        {
            Version++;
            IsDirty = false;
            _db = Load(out _resolver);
        }

        static T Load(out DBResolver resolver)
        {
            IsDirty = false;
            using (var fileReader = File.OpenRead(MetaData.SourcePath))
            {
                using (var reader = new StreamReader(fileReader))
                {
                    return DBResolver.LoadInternal<T>(reader, EditorDB.EditorUnityObjectsResolver, out resolver);
                }
            }
        }

        public static void Save()
        {
            if (_db == null)
            {
                Debug.LogWarning($"Database {typeof(T)} not loaded");
                return;
            }

            var source = MetaData.SourcePath;
            Directory.CreateDirectory(Path.GetDirectoryName(source));

            var dbConverter = new DBConverter(typeof(T), null, null);
            using (var tWriter = File.CreateText(source))
            {
                using (var jWriter = new JsonTextWriter(tWriter))
                {
                    jWriter.Formatting = Formatting.Indented;
                    dbConverter.Write(jWriter, _db);
                }
            }

            var generatesFiles = new List<string>();
            generatesFiles.Add(MetaData.CsGenPath);

            foreach (var ga in _generators)
            {
                try
                {
                    var sb = new IndentStringBuilder();
                    ga.Generator.Execute(sb, _db);
                    File.WriteAllText(ga.Path, sb.ToString());
                    generatesFiles.Add(ga.Path);
                } catch (Exception exc)
                {
                    Debug.LogError($"Error when execute generator {ga.Generator} => {ga.Path}");
                    Debug.LogException(exc);
                }
            }

            AssetDatabase.ImportAsset(MetaData.SourcePath, ImportAssetOptions.ForceUpdate);
            foreach (var f in generatesFiles)
            {
                AssetDatabase.ImportAsset(f, ImportAssetOptions.ForceUpdate);
            }

            Version++;
            IsDirty = false;

            if (dbConverter.HasChanges)
            {
                Load();
            }
        }
    }
}