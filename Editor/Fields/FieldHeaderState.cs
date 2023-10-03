using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FDB.Editor
{
    public abstract class FieldHeaderState : HeaderState
    {
        public readonly FieldInfo Field;

        public FieldHeaderState(string path, FieldInfo field) : base(
            path,
            field?.GetCustomAttributes().ToArray(),
            field?.Name ?? "Item",
            null)
        {
            Field = field;
        }

        public override object Get(object config, int? collectionIndex)
        {
            return Field.GetValue(config);
        }

        public override void Set(object config, int? collectionIndex, object value)
        {
            Field.SetValue(config, value);
            GUI.changed = true;
        }
    }

}