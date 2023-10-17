using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class BoolHeader : FieldHeader
    {
        public BoolHeader(string path, FieldInfo field) : base(typeof(bool), path, field) { }

        public override bool Filter(object config, string filter)
        {
            var value = (bool)Get(config, null);
            return filter.ToLower() == (value ? "true" : "false");
        }

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