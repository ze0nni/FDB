using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace FDB.Editor
{
    [ScriptedImporter(1, "furydb", AllowCaching = false)]
    public class FuruDBImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                var dbAsset = ScriptableObject.CreateInstance<FuryDBAsset>();
                dbAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

                dbAsset.JsonData = File.ReadAllBytes(ctx.assetPath);
                var md5 = new MD5CryptoServiceProvider().ComputeHash(dbAsset.JsonData);
                dbAsset.MD5 = BitConverter.ToString(md5).Replace("-", "");

                if (!GetDBByPath(ctx.assetPath, out var dbType))
                {
                    throw new Exception($"Not found class associated with {ctx.assetPath}");
                }

                dbAsset.DBTypeName = dbType.AssemblyQualifiedName;

                object db;
                DBResolver resolver;

                using (var reader = new StreamReader(ctx.assetPath))
                {
                    db = DBResolver.LoadInternal(dbType, reader, EditorDB.EditorUnityObjectsResolver, out resolver);
                }

                dbAsset.Entries = new List<FuryDBEntryAsset>();

                foreach (var indexName in resolver.IndexeNames)
                {
                    var entryAsset = ScriptableObject.CreateInstance<FuryDBEntryAsset>();
                    entryAsset.name = indexName;
                    ctx.AddObjectToAsset("Entery/" + indexName, entryAsset);
                    dbAsset.Entries.Add(entryAsset);

                    foreach (var dependency in resolver.GetDependency(indexName))
                    {
                        entryAsset.Dependency.Add(dependency);
                    }
                }

                ctx.AddObjectToAsset(".furydb", dbAsset, FDBEditorIcons.Asset);
                ctx.SetMainObject(dbAsset);
            }
            catch (Exception exc)
            {
                Debug.LogException(exc);

                var dbAsset = ScriptableObject.CreateInstance<FuryDBAsset>();
                dbAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
                dbAsset.Errors = new List<string>();
                dbAsset.Errors.Add(exc.Message);
                ctx.AddObjectToAsset(".furydb", dbAsset, FDBEditorIcons.ErrorIcon);
                ctx.SetMainObject(dbAsset);
                return;
            }
        }

        public static bool GetDBByPath(string path, out Type outType)
        {
            foreach (var dbType in TypeCache.GetTypesWithAttribute<FuryDBAttribute>())
            {
                var attrType = dbType.GetCustomAttribute<FuryDBAttribute>();
                if (attrType.SourcePath == path)
                {
                    outType = dbType;
                    return true;
                }
            }
            outType = default;
            return false;
        }
    }
}