using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class FloatHeader : FieldHeader
    {
        public FloatHeader(string path, FieldInfo field) : base(path, field) { }

        public override bool Filter(object config, string filter)
        {
            var f = (float)Get(config, null);
            var i = (int)f;
            return int.TryParse(filter, out var n) && i == n;
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var f = (float)rawValue;

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.FloatField(lineRect, f);

            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, newValue);
            }
        }
    }
}