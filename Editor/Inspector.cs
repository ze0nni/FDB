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

        public static object Field(DBResolver resolver, HeaderState header, object owner, object rawValue, int nestLevel, Action makeDirty)
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

                        var canExport = kind.CanExport;
                        if (index.IsDuplicateKind(kind.Value))
                        {
                            GUI.color = Color.red;
                        }
                        else if (!canExport)
                        {
                            GUI.color = Color.gray;
                        }

                        var newValue = EditorGUILayout.TextField(kind.Value, layoutWidth);
                        GUI.color = color;

                        if (!canExport)
                        {
                            var rect = GUILayoutUtility.GetLastRect();
                            GUI.Box(
                                new Rect(
                                    rect.xMax - rect.height,
                                    rect.y,
                                    rect.height,
                                    rect.height),
                                FDBEditorIcons.NotExportIcon);
                        }

                        if (newValue != kind.Value)
                        {
                            return Activator.CreateInstance(kindHeader.Field.FieldType, newValue);
                        }
                    }
                    return rawValue;

                case RefFieldHeaderState refHeader:
                    {
                        Func<object, DBResolver, Type, AutoRefAttribute, Ref, float, GUILayoutOption, int, Action, Ref> field =
                            nestLevel == 0 ? ChooseRefWindow<NestLevel0>.Field
                            : nestLevel == 1 ? ChooseRefWindow<NestLevel1>.Field
                            : nestLevel == 2 ? ChooseRefWindow<NestLevel2>.Field
                            : nestLevel == 3 ? ChooseRefWindow<NestLevel3>.Field
                            : nestLevel == 4 ? ChooseRefWindow<NestLevel4>.Field
                            : nestLevel == 5 ? ChooseRefWindow<NestLevel5>.Field
                            : nestLevel == 6 ? ChooseRefWindow<NestLevel6>.Field
                            : nestLevel == 7 ? ChooseRefWindow<NestLevel7>.Field
                            : null;

                        if (field == null)
                        {
                            GUILayout.Label("Too deep ref windows", layoutWidth);
                            return rawValue;
                        }

                        return field(
                            owner,
                            resolver,
                            refHeader.ModelType,
                            refHeader.AutoRef,
                            (Ref)rawValue,
                            header.Width,
                            layoutWidth,
                            nestLevel,
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
                            return EditorGUILayout.TextArea(
                                (string)rawValue,
                                FDBEditorStyles.WordWrapTextArea,
                                layoutWidth, 
                                GUILayout.MinHeight(minLines * 16),
                                GUILayout.MaxHeight(maxLines * 16));
                        }
                        else
                        {
                            return EditorGUILayout.TextField((string)rawValue, layoutWidth);
                        }
                    }

                case AssetReferenceFieldHeaderState assetRefHeader:
                    {
                        return AssetReferenceField.Field(rawValue as AssetReference, assetRefHeader.AssetType, layoutWidth);
                    }
                case ColorFieldHeaderState colorHeader:
                    {
                        return EditorGUILayout.ColorField((Color)rawValue, layoutWidth);
                    }
                case AnimationCurveFieldHeaderState _:
                    {
                        return EditorGUILayout.CurveField((AnimationCurve)rawValue, layoutWidth);
                    }
                case UnityObjectFieldHeaderState unityObjectField:
                    {
                        return UnityObjectField.Field((UnityEngine.Object)rawValue, unityObjectField.Field.FieldType, layoutWidth, makeDirty);
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