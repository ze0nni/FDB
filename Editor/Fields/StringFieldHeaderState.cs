using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class StringFieldHeaderState : FieldHeaderState
    {
        private int _minLines;
        private MethodInfo _condition;

        public StringFieldHeaderState(string path, Type ownerType, FieldInfo field) : base(path, field)
        {
            if (field != null)
            {
                var multilineAttr = field.GetCustomAttribute<MultilineTextAttribute>();
                if (multilineAttr != null)
                {
                    _minLines = multilineAttr.MinLines;
                    if (ownerType != null && !string.IsNullOrEmpty(multilineAttr.Condition))
                    {
                        _condition = ownerType.GetMethod(multilineAttr.Condition, BindingFlags.Static | BindingFlags.NonPublic);
                    }
                }
            }
        }

        public static object[] _conditionArgs = new object[1];
        public bool IsMultiline(object owner, out int minLines)
        {
            minLines = _minLines;
            if (_condition == null)
            {
                return false;
            }

            if (minLines > 1 && _condition == null)
            {
                return true;
            }
            _conditionArgs[0] = owner;
            return (bool)_condition.Invoke(null, _conditionArgs);
        }

        public override float GetFieldHeight(in PageContext context, object config)
        {
            if (IsMultiline(config, out var minLines))
            {
                return minLines * GUIConst.RowFieldHeight;
            }
            return GUIConst.RowFieldHeight;
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object value)
        {
            var str = value.ToString();

            EditorGUI.BeginChangeCheck();
            string newValue;
            if (!IsMultiline(config, out var _))
            {
                newValue = GUI.TextField(lineRect, str);
            }
            else
            {
                newValue = GUI.TextField(rect, str, FDBEditorStyles.WordWrapTextArea);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, newValue);
            }
        }
    }
}