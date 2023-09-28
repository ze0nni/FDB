using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

                if (!GetDBByPath(ctx.assetPath, out var dbType))
                {
                    throw new Exception($"Not found class associated with {ctx.assetPath}");
                }

                ctx.AddObjectToAsset(".furydb", dbAsset, FDBEditorIcons.Asset);
                ctx.SetMainObject(dbAsset);

                object db;
                DBResolver resolver;

                using (var reader = new StreamReader(ctx.assetPath))
                {
                    db = DBResolver.LoadInternal(dbType, reader, out resolver);
                }

                dbAsset.Entries = new List<FuryDBEntryAsset>();

                foreach (var indexName in resolver.IndexeNames)
                {
                    var entryAsset = ScriptableObject.CreateInstance<FuryDBEntryAsset>();
                    entryAsset.name = indexName;
                    ctx.AddObjectToAsset("Entery/" + indexName, entryAsset);
                    dbAsset.Entries.Add(entryAsset);
                }
            }
            catch (Exception exc)
            {
                Debug.LogException(exc);

                var dbAsset = ScriptableObject.CreateInstance<FuryDBAsset>();
                dbAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
                dbAsset.Errors = new List<string>();
                dbAsset.Errors.Add(exc.Message);
                ctx.AddObjectToAsset(".furydb", dbAsset, FDBEditorIcons.ErrorIcon);
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