using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

        public static object Field(DBResolver resolver, HeaderState header, object owner, object rawValue)
        {
            switch (header)
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

                        var newValue = EditorGUILayout.TextField(kind.Value, GUILayout.Width(header.Width));

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
                            header.Width);
                    }

                case EnumFieldHeaderState enumHeader:
                    {
                        var index = Array.IndexOf(enumHeader.Values, rawValue);
                        var newIndex = EditorGUILayout.Popup(index, enumHeader.Names, GUILayout.Width(header.Width));
                        return enumHeader.Values.GetValue(newIndex);
                    }

                case BoolFieldHeaderState boolField:
                    {
                        return EditorGUILayout.Toggle((bool)rawValue, GUILayout.Width(header.Width));
                    }

                case IntFieldHeaderState intHeader:
                    {
                        return EditorGUILayout.IntField((int)rawValue, GUILayout.Width(header.Width));
                    }

                case FloatFieldHeaderState floatHeader:
                    {
                        return EditorGUILayout.FloatField((float)rawValue, GUILayout.Width(header.Width));
                    }

                case StringFieldHeaderState stringHeader:
                    {
                        if (stringHeader.IsMultiline(owner, out var minLines, out var maxLines))
                        {
                            return EditorGUILayout.TextArea((string)rawValue, GUILayout.Width(header.Width), GUILayout.MinHeight(minLines * 16), GUILayout.MaxHeight(maxLines * 16));
                        }
                        else
                        {
                            return EditorGUILayout.TextField((string)rawValue, GUILayout.Width(header.Width));
                        }
                    }

                case AssetReferenceFieldHeaderState assetRefHeader:
                    {
                        return AssetReferenceField.Field(rawValue as AssetReference, GUILayout.Width(header.Width));
                    }
                case ColorFieldHeaderState colorHeader:
                    {
                        return EditorGUILayout.ColorField((Color)rawValue, GUILayout.Width(header.Width));
                    }
                case AnimationCurveFieldHeaderState _:
                    {
                        return EditorGUILayout.CurveField((AnimationCurve)rawValue, GUILayout.Width(header.Width));
                    }

                default:
                    GUILayout.Box(header.GetType().Name, GUILayout.Width(header.Width));
                    return rawValue;
            }            
        }

        internal static Func<object, object[]> BuilDisplayResolver(Type modelType)
        {
            return null;
        }

        internal static string ToString(object obj)
        {
            switch (obj)
            {
                case bool _:
                case int _:
                case float _:
                case string _:
                    return obj.ToString();
                case Kind kind:
                    return kind.Value;
            }
            return obj.ToString();
        }
    }
}