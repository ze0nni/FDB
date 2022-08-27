using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public static partial class Inspector
    {
        public static bool ApplyFilter(object item, string filter)
        {
            if (item == null)
            {
                return "null".Contains(filter, StringComparison.CurrentCultureIgnoreCase);
            }
            foreach (var field in item.GetType().GetFields())
            {
                var value = field.GetValue(item);
                if ((value?.ToString() ?? "null").Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static object Field(DBResolver resolver, FieldHeaderState fieldHeader, object rawValue)
        {
            switch (fieldHeader)
            {
                case KindFieldHeaderState kindHeader:
                    {
                        var kind = (Kind)rawValue;
                        var index = resolver.GetIndex(kindHeader.ModelType);

                        var color = GUI.color;
                        if (index.IsDuplicateKind(kind.Value))
                        {
                            GUI.color = Color.red;
                        }

                        var newValue = EditorGUILayout.TextField(kind.Value, GUILayout.Width(fieldHeader.Width));

                        GUI.color = color;

                        if (newValue != kind.Value)
                        {
                            return Activator.CreateInstance(kindHeader.Field.FieldType, newValue);
                        }
                    }
                    return rawValue;

                case RefFieldHeaderState refHeader:
                    {
                        return ChooseRefWindow.Field(
                            resolver,
                            refHeader.ModelType,
                            (Ref)rawValue,
                            GUILayout.Width(fieldHeader.Width));
                    }

                case IntFieldHeaderState intHeader:
                    {
                        return EditorGUILayout.IntField((int)rawValue, GUILayout.Width(fieldHeader.Width));
                    }

                case EnumFieldHeaderState enumHeader:
                    {
                        var index = Array.IndexOf(enumHeader.Values, rawValue);
                        var newIndex = EditorGUILayout.Popup(index, enumHeader.Names, GUILayout.Width(fieldHeader.Width));
                        return enumHeader.Values.GetValue(newIndex);
                    }
                default:
                    GUILayout.Space(fieldHeader.Width);
                    return rawValue;
            }            
        }

        internal static Func<object, object[]> BuilDisplayResolver(Type modelType)
        {
            return null;
        }
    }
}