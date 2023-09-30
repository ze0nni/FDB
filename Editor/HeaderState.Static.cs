using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB.Editor
{
    public abstract partial class HeaderState
    {
        public static IEnumerable<HeaderState> Of(Type type, int depth, string rootPath, bool requestKind, Action<string> addError)
        {
            var kindResolved = false;

            foreach (var field in type.GetFields())
            {
                var path = $"{rootPath}/{field.Name}";

                if (field.Name == DBResolver.__GUID)
                {
                    continue;
                }

                if (field.FieldType.IsGenericType)
                {
                    var fieldGenericType = field.FieldType.GetGenericTypeDefinition();
                    if (fieldGenericType == typeof(Kind<>))
                    {
                        if (field.Name == "Kind" && field.FieldType.GetGenericArguments()[0] == type)
                        {
                            kindResolved = true;
                        }
                        yield return new KindFieldHeaderState(path, field);
                    }
                    else if (fieldGenericType == typeof(Ref<>))
                    {
                        var modelType = field.FieldType.GetGenericArguments()[0];
                        yield return new RefFieldHeaderState(path, field);
                    }
                    else if (fieldGenericType == typeof(AssetReferenceT<>))
                    {
                        var assetType = field.FieldType.GetGenericArguments()[0];
                       
                        yield return new AssetReferenceFieldHeaderState(path, field, assetType);
                    }
                    else if (fieldGenericType == typeof(List<>))
                    {
                        var listRoot = $"{path}/{field.Name}";
                        var itemType = field.FieldType.GetGenericArguments()[0];
                        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Ref<>))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new RefFieldHeaderState(listRoot, itemType.GetGenericArguments()[0]) });
                        }
                        else if (itemType.IsEnum)
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new EnumFieldHeaderState(listRoot, itemType) });
                        }
                        else if (itemType == typeof(bool))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new BoolFieldHeaderState(listRoot, null) });
                        }
                        else if (itemType == typeof(int))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new IntFieldHeaderState(listRoot, null) });
                        }
                        else if (itemType == typeof(float))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new FloatFieldHeaderState(listRoot, null) });
                        }
                        else if (itemType == typeof(string))
                        {
                            yield return new ListHeaderState(path, type, field, itemType, true,
                                new[] { new StringFieldHeaderState(listRoot, null, null) });
                        }
                        else if (itemType == typeof(AssetReference) || itemType == typeof(AssetReferenceT<>))
                        {
                            Debug.LogWarning("List of AssetReference not supported");
                        }
                        else if (DBResolver.IsSupportedUnityType(itemType))
                        {
                            Debug.LogWarning("List of UnityEngine.Object not supported");
                        }
                        else
                        {
                            yield return new ListHeaderState(path, type, field, itemType, false,
                                Of(itemType, depth + 1, listRoot, false, addError).ToArray());
                        }
                    }
                }
                else if (field.FieldType.IsEnum)
                {
                    yield return new EnumFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(bool))
                {
                    yield return new BoolFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(int))
                {
                    yield return new IntFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(float))
                {
                    yield return new FloatFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(string))
                {
                    yield return new StringFieldHeaderState(path, type, field);
                }
                else if (field.FieldType == typeof(AssetReference))
                {
                    yield return new AssetReferenceFieldHeaderState(path, field, typeof(object));
                }
                else if (field.FieldType == typeof(Color))
                {
                    yield return new ColorFieldHeaderState(path, field);
                }
                else if (field.FieldType == typeof(AnimationCurve))
                {
                    yield return new AnimationCurveFieldHeaderState(path, field);
                }
                else if (DBResolver.IsSupportedUnityType(field.FieldType))
                {
                    yield return new UnityObjectFieldHeaderState(path, field);
                }
            }

            if (requestKind && !kindResolved)
            {
                addError($"Model {type.Name} must containst field Kind<{type.Name}>");
            }
        }
    }
}