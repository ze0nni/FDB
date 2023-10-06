using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class EnumHeader : FieldHeader
    {
        public readonly Array Values;
        public readonly string[] Names;

        public EnumHeader(string path, FieldInfo field) : base(path, field)
        {
            Values = field.FieldType.GetEnumValues();
            Names = field.FieldType.GetEnumNames();
        }

        public EnumHeader(string path, Type enumType) : base(path, null)
        {
            Values = enumType.GetEnumValues();
            Names = enumType.GetEnumNames();
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var index = Array.IndexOf(Values, rawValue);

            EditorGUI.BeginChangeCheck();

            var newIndex = EditorGUI.Popup(lineRect, index, Names);
            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, Values.GetValue(newIndex));
            }
        }
    }
}