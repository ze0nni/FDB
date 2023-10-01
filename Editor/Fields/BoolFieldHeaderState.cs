using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class BoolFieldHeaderState : FieldHeaderState
    {
        public BoolFieldHeaderState(string path, FieldInfo field) : base(path, field) { }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var b = (bool)rawValue;

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.Toggle(lineRect, b);

            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, newValue);
            }
        }
    }
}