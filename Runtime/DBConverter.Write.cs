using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB
{
    public sealed partial class DBConverter
    {

#if !UNITY_EDITOR
        public void Write(JsonWriter writer, object value)
        {
            throw new NotImplementedException();
        }
#else
        public bool HasChanges { get; private set; }

        public void Write(JsonWriter writer, object db)
        {
            writer.WriteStartObject();
            foreach (var field in _dbType.GetFields())
            {
                var fieldType = field.FieldType;
                if (fieldType.IsGenericType)
                {
                    var genericFieldType = fieldType.GetGenericTypeDefinition();
                    if (genericFieldType == typeof(Index<>))
                    {
                        writer.WritePropertyName(field.Name);
                        WriteIndex(writer, (Index)field.GetValue(db));
                    }
                }
            }

            writer.WriteEndObject();
        }

        void WriteIndex(JsonWriter writer, Index index)
        {
            writer.WriteStartArray();
            foreach (var model in index.All())
            {
                WriteObject(writer, model);
            }
            writer.WriteEndArray();
        }

        void WriteObject(JsonWriter writer, object originModel)
        {
            if (originModel == null)
            {
                writer.WriteNull();
                return;
            }

            var model = DBResolver.WrapObj(originModel);
            var changed = DBResolver.Invalidate(model);

            if (model != originModel || changed)
            {
                HasChanges = true;
            }

            writer.WriteStartObject();

            foreach (var field in model.GetType().GetFields())
            {
                writer.WritePropertyName(field.Name);
                WriteValue(writer, field.FieldType, field.GetValue(model));
            }

            writer.WriteEndObject();
        }

        void WriteValue(JsonWriter writer, Type type, object value)
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(Kind<>))
                {
                    var kind = (Kind)value;
                    writer.WriteValue(kind.Value ?? string.Empty);
                }
                else if (genericType == typeof(Ref<>))
                {
                    writer.WriteValue(((Ref)value).Kind.Value);
                }
                else if (genericType == typeof(AssetReferenceT<>))
                {
                    var r = (AssetReference)value;
                    if (r != null && r.editorAsset != null)
                    {
                        writer.WriteValue(r.AssetGUID);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                }
                else if (genericType == typeof(List<>))
                {
                    var itemType = type.GetGenericArguments()[0];
                    var collection = (IEnumerable)value;

                    writer.WriteStartArray();
                    if (collection != null)
                    {
                        foreach (var i in collection)
                        {
                            WriteValue(writer, itemType, i);
                        }
                    }
                    writer.WriteEndArray();
                } else
                {
                    writer.WriteUndefined();
                }
            } else if (type.IsEnum)
            {
                writer.WriteValue(value.ToString());
            } else if (type == typeof(bool))
            {
                writer.WriteValue((bool)value);
            } else if (type == typeof(int))
            {
                writer.WriteValue((int)value);
            } else if (type == typeof(float))
            {
                writer.WriteValue((float)value);
            } else if (type == typeof(string))
            {
                writer.WriteValue((string)value);
            } else if (type == typeof(AssetReference)) {
                var r = (AssetReference)value;
                if (r != null && r.editorAsset != null)
                {
                    writer.WriteValue(r.AssetGUID);
                } else
                {
                    writer.WriteNull();
                }
            } else if (type == typeof(Color)) {
                var color = (Color)value;
                writer.WriteStartConstructor("Color");
                writer.WriteValue(color.r);
                writer.WriteValue(color.g);
                writer.WriteValue(color.b);
                writer.WriteValue(color.a);
                writer.WriteEndConstructor();
            } else if (type == typeof(AnimationCurve))
            {
                var curve = (AnimationCurve)value;
                writer.WriteStartArray();
                for (var i = 0; i < curve.length; i++)
                {
                    var key = curve.keys[i];
                    writer.WriteStartConstructor("Key");
                    writer.WriteValue(key.time);
                    writer.WriteValue(key.value);
                    writer.WriteValue(key.inTangent);
                    writer.WriteValue(key.outTangent);
                    writer.WriteValue(key.inWeight);
                    writer.WriteValue(key.outWeight);
                    writer.WriteEndConstructor();
                }
                writer.WriteEndArray();
            } else if (DBResolver.IsSupportedUnityType(type))
            {
                var uObject = (UnityEngine.Object)value;
                if (uObject != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(uObject, out string guid, out long _))
                {
                    writer.WriteValue(guid);
                } else
                {
                    writer.WriteNull();
                }
            } else if (type.IsClass)
            {
                WriteObject(writer, value);
            } else
            {
                writer.WriteComment(type.Name);
                writer.WriteUndefined();
            }
        }
#endif
    }
}
