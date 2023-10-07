using System;
using System.Reflection;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class ListHeader : Header
    {
        public readonly FieldInfo Field;
        public readonly Type ItemType;
        public readonly bool Primitive;
        public readonly Aggregator Aggregator;

        public ListHeader(string path, Type ownerType, FieldInfo field, Type itemType, bool primitive, Header[] headers)
            : base(path, null, field.Name, headers)
        {
            Field = field;
            ItemType = itemType;
            Primitive = primitive;
            Aggregator = new Aggregator(ownerType, field, itemType);
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

        public override bool Filter(object config, string filter)
        {
            return false;
        }

        public override void OnGUI(in PageContext context, Rect rect, object config, int? collectionIndex)
        {
            var lineRect = rect;
            lineRect.height = GUIConst.RowFieldHeight;

            if (GUI.Button(lineRect, "[...]"))
            {
                context.Inspector.ToggleExpandedState(config, this);
            }
        }
    }
}