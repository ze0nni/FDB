using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB.Editor
{
    public abstract partial class Header
    {
        public static IEnumerable<Header> Of(Type type, int depth, string rootPath, bool requestKind, Action<string> addError)
        {
            if (type.IsSealed)
            {
                throw new ArgumentException($"Type {type} is sealed");
            }

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
                        yield return new KindHeader(path, field);
                    }
                    else if (fieldGenericType == typeof(Ref<>))
                    {
                        var modelType = field.FieldType.GetGenericArguments()[0];
                        yield return new RefHeader(path, field);
                    }
                    else if (fieldGenericType == typeof(AssetReferenceT<>))
                    {
                        var assetType = field.FieldType.GetGenericArguments()[0];
                       
                        yield return new AssetReferenceHeader(path, assetType, field);
                    }
                    else if (fieldGenericType == typeof(List<>))
                    {
                        var listRoot = $"{path}/{field.Name}";
                        var itemType = field.FieldType.GetGenericArguments()[0];
                        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Ref<>))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new RefHeader(listRoot, itemType.GetGenericArguments()[0]) });
                        }
                        else if (itemType.IsEnum)
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new EnumHeader(listRoot, itemType) });
                        }
                        else if (itemType == typeof(bool))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new BoolHeader(listRoot, null) });
                        }
                        else if (itemType == typeof(int))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new IntHeader(listRoot, null) });
                        }
                        else if (itemType == typeof(float))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new FloatHeader(listRoot, null) });
                        }
                        else if (itemType == typeof(string))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new StringHeader(listRoot, null, null) });
                        }
                        else if (itemType == typeof(Color))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new ColorHeader(listRoot, null) });
                        }
                        else if (itemType == typeof(AnimationCurve))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new AnimationCurveHeader(listRoot, null) });
                        }
                        else if (itemType == typeof(AssetReference))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new AssetReferenceHeader(listRoot, typeof(UnityEngine.GameObject), null) });
                        }
                        else if (itemType == typeof(AssetReferenceT<>))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new AssetReferenceHeader(listRoot, itemType.GetGenericArguments()[0], null) });
                        }
                        else if (DBResolver.IsSupportedUnityType(itemType))
                        {
                            yield return new ListHeader(path, type, field, itemType, true,
                                new[] { new UnityObjectHeader(listRoot, itemType, null) });
                        }
                        else
                        {
                            yield return new ListHeader(path, type, field, itemType, false,
                                Of(itemType, depth + 1, listRoot, false, addError).ToArray());
                        }
                    }
                }
                else if (UnionHeader.TryGetUnionType(field.FieldType, out var unionBaseType, out var unionTagType))
                {
                    yield return new UnionHeader(path, field, unionBaseType, unionTagType, 
                        Header.Of(field.FieldType, depth + 1, $"{path}/{field.Name}", false, addError).ToArray());
                }
                else if (field.FieldType.IsEnum)
                {
                    yield return new EnumHeader(path, field);
                }
                else if (field.FieldType == typeof(bool))
                {
                    yield return new BoolHeader(path, field);
                }
                else if (field.FieldType == typeof(int))
                {
                    yield return new IntHeader(path, field);
                }
                else if (field.FieldType == typeof(float))
                {
                    yield return new FloatHeader(path, field);
                }
                else if (field.FieldType == typeof(string))
                {
                    yield return new StringHeader(path, type, field);
                }
                else if (field.FieldType == typeof(AssetReference))
                {
                    yield return new AssetReferenceHeader(path, typeof(UnityEngine.Object), field);
                }
                else if (field.FieldType == typeof(Color))
                {
                    yield return new ColorHeader(path, field);
                }
                else if (field.FieldType == typeof(AnimationCurve))
                {
                    yield return new AnimationCurveHeader(path, field);
                }
                else if (DBResolver.IsSupportedUnityType(field.FieldType))
                {
                    yield return new UnityObjectHeader(path, field.FieldType, field);
                } else if (field.FieldType.IsClass)
                {
                    yield return new ObjectHeader(field.FieldType, path, field, 
                        Of(field.FieldType, depth + 1, $"{path}/{field.Name}", false, addError).ToArray());
                }
            }

            if (requestKind && !kindResolved)
            {
                addError($"Model {type.Name} must containst field Kind<{type.Name}>");
            }
        }
    }
}