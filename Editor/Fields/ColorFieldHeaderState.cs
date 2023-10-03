using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class ColorFieldHeaderState : FieldHeaderState
    {
        public ColorFieldHeaderState(string path, FieldInfo field) : base(path, field) { }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var c = (Color)rawValue;

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.ColorField(lineRect, c);

            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, newValue);
            }
        }
    }
}