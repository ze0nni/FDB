using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FDB.Editor
{
    public abstract class FieldHeader : Header
    {
        public readonly FieldInfo Field;

        public FieldHeader(Type headerType, string path, FieldInfo field) : base(
            headerType,
            path,
            field?.GetCustomAttributes().ToArray(),
            field?.Name ?? "Item",
            null)
        {
            Field = field;
        }

        public override object Get(object config, int? collectionIndex)
        {
            if (Field != null)
                return Field.GetValue(config);
            else
            {
                var collection = (IList)config;
                return collection[collectionIndex.Value];
            }
        }

        public override void Set(object config, int? collectionIndex, object value)
        {
            if (Field != null)
            {
                Field.SetValue(config, value);
                GUI.changed = true;
            }
            else
            {
                var collection = (IList)config;
                collection[collectionIndex.Value] = value;
                GUI.changed = true;
            }
        }
    }

}