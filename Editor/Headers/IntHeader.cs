using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class IntHeader : FieldHeader
    {
        public IntHeader(string path, FieldInfo field) : base(path, field) { }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var i = (int)rawValue;

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.IntField(lineRect, i);

            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, newValue);
            }
        }
    }
}