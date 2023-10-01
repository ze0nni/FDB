using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class FloatFieldHeaderState : FieldHeaderState
    {
        public FloatFieldHeaderState(string path, FieldInfo field) : base(path, field) { }

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