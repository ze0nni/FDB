using System;
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

        public static object Field(DBResolver resolver, HeaderState header, object owner, object rawValue, bool isPopup, Action makeDirty)
        {
            var layoutWidth = header.ExpandWidth
                ? GUILayout.ExpandWidth(true)
                : GUILayout.Width(header.Width);
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

                        var newValue = EditorGUILayout.TextField(kind.Value, layoutWidth);

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
                            owner,
                            resolver,
                            refHeader.ModelType,
                            refHeader.AutoRef,
                            (Ref)rawValue,
                            header.Width,
                            layoutWidth,
                            isPopup,
                            makeDirty);
                    }

                case EnumFieldHeaderState enumHeader:
                    {
                        var index = Array.IndexOf(enumHeader.Values, rawValue);
                        var newIndex = EditorGUILayout.Popup(index, enumHeader.Names, layoutWidth);
                        return enumHeader.Values.GetValue(newIndex);
                    }

                case BoolFieldHeaderState boolField:
                    {
                        return EditorGUILayout.Toggle((bool)rawValue, layoutWidth);
                    }

                case IntFieldHeaderState intHeader:
                    {
                        return EditorGUILayout.IntField((int)rawValue, layoutWidth);
                    }

                case FloatFieldHeaderState floatHeader:
                    {
                        return EditorGUILayout.FloatField((float)rawValue, layoutWidth);
                    }

                case StringFieldHeaderState stringHeader:
                    {
                        if (stringHeader.IsMultiline(owner, out var minLines, out var maxLines))
                        {
                            return EditorGUILayout.TextArea((string)rawValue, layoutWidth, GUILayout.MinHeight(minLines * 16), GUILayout.MaxHeight(maxLines * 16));
                        }
                        else
                        {
                            return EditorGUILayout.TextField((string)rawValue, layoutWidth);
                        }
                    }

                case AssetReferenceFieldHeaderState assetRefHeader:
                    {
                        return AssetReferenceField.Field(rawValue as AssetReference, layoutWidth);
                    }
                case ColorFieldHeaderState colorHeader:
                    {
                        return EditorGUILayout.ColorField((Color)rawValue, layoutWidth);
                    }
                case AnimationCurveFieldHeaderState _:
                    {
                        return EditorGUILayout.CurveField((AnimationCurve)rawValue, layoutWidth);
                    }

                default:
                    GUILayout.Box(header.GetType().Name, layoutWidth);
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