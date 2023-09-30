using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor {

    internal static class EditorDB
    {
        public static UnityEngine.Object EditorUnityObjectsResolver(string guid, Type type)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath(path, type);
        }
    }

    public class EditorDB<T>
    {
        public static long Version { get; private set; }
        public static bool IsDirty { get; private set; }

        static T _db;
        static DBResolver _resolver;

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
                    if (File.Exists(MetaData.SourcePath)) {
                        _db = Load(out _resolver);
                    } else
                    {
                        _db = DBResolver.New<T>(out _resolver);
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

            GenerateCs(MetaData.CsPath, _db);

            AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(MetaData.CsPath, ImportAssetOptions.ForceUpdate);

            Version++;
            IsDirty = false;

            if (dbConverter.HasChanges)
            {
                Load();
            }
        }

        public static void GenerateCs(string path, T db)
        {
            var sb = new StringBuilder();

            var dbType = typeof(T);

            sb.AppendLine("using FDB;");
            if (!string.IsNullOrEmpty(dbType.Namespace))
            {
                sb.AppendLine($"namespace {dbType.Namespace}");
                sb.AppendLine("{");
            }

            {
                sb.AppendLine($"\tpublic static partial class Kinds");
                sb.AppendLine("\t{");
                foreach (var field in dbType.GetFields())
                {
                    var index = field.GetValue(db) as Index;
                    if (index == null)
                    {
                        continue;
                    }
                    sb.AppendLine($"\t\tpublic static class {field.Name}");
                    sb.AppendLine("\t\t{");
                    var modelType = index.GetType().GetGenericArguments()[0];
                    var kindField = modelType.GetField("Kind");
                    foreach (var config in index.All())
                    {
                        var kind = (Kind)kindField.GetValue(config);
                        if (!kind.CanExport)
                        {
                            sb.AppendLine($"\t\t\t// Skip kind '{kind.Value}'");
                        }
                        else if (index.IsDuplicateKind(kind.Value))
                        {
                            sb.AppendLine($"\t\t\t// Skip duplicate '{kind.Value}'");
                            Debug.LogWarning($"Skip duplicate kind='{kind.Value}' of {modelType.Name}");
                        }
                        else
                        {
                            sb.AppendLine($"\t\t\tpublic static Kind<{modelType.Name}> {kind.Value} = new Kind<{modelType.Name}>(\"{kind.Value}\");");
                        }
                    }
                    sb.AppendLine("\t\t}");

                }
                sb.AppendLine("\t}");
            }

            if (!string.IsNullOrEmpty(dbType.Namespace))
            {
                sb.AppendLine("}");
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}