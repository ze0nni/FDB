using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class AnimationCurveHeader : FieldHeader
    {
        public AnimationCurveHeader(string path, FieldInfo field) : base(path, field) { }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var c = (AnimationCurve)rawValue;

            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUI.CurveField(lineRect, c);

            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, newValue);
            }
        }
    }
}