using System;
using System.Collections;
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
            : base(field.FieldType, path, null, field.Name, headers)
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

        public override bool GetExpandedList(object config, int? collectionIndex, out IList list, out ListHeader listHeader)
        {
            list = (IList)Get(config, null);
            listHeader = this;
            return true;
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var e = GUI.enabled;
            GUI.enabled = true;
            if (GUI.Button(lineRect, "[...]"))
            {
                context.Inspector.ToggleExpandedState(config, this);
            }
            GUI.enabled = e;
        }
    }
}