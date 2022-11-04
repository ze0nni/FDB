using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB
{
    public sealed partial class DBConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var resolver = DBResolver.Current;
            if (resolver == null)
            {
                throw new InvalidOperationException($"Use {nameof(DBResolver)} for instantiate model");
            }

            var model = (T)DBResolver.Instantate(typeof(T), false);

            Contract.Assert(reader.TokenType == JsonToken.StartObject);            

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        {
                            var fieldName = reader.Value.ToString();
                            var field = objectType.GetField(fieldName);
                            if (field == null)
                            {
                                Debug.LogWarning($"field {fieldName} not found in {objectType.FullName}");
                                reader.Skip();

                                break;
                            }

                            var fieldType = field.FieldType;
                            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Index<>))
                            {
                                reader.Read();
                                field.SetValue(model, ReadIndex(resolver, reader, fieldType));
                                break;
                            }

                            Debug.LogWarning($"field {fieldName} in {objectType.FullName} skipped");

                            reader.Skip();
                            break;
                        }

                    case JsonToken.EndObject:
                        goto endObject;

                    default:
                        throw new ArgumentException($"Unexcepted token {reader.TokenType}");
                }

            }
        endObject:
            Contract.Assert(reader.TokenType == JsonToken.EndObject);

            resolver.SetDB(model);
            resolver.Resolve();

            return model;
        }

        object ReadIndex(DBResolver resolver, JsonReader reader, Type indexType)
        {
            var modelType = indexType.GetGenericArguments()[0];
            var index = (Index)Activator.CreateInstance(indexType);

            Contract.Assert(reader.TokenType == JsonToken.StartArray);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        {
                            index.Add(ReadObject(resolver, reader, modelType));
                        }
                        break;

                    case JsonToken.EndArray:
                        goto endArray;

                    default:
                        throw new ArgumentException($"Unexcepted token {reader.TokenType}");
                }
            }
            endArray:
            Contract.Assert(reader.TokenType == JsonToken.EndArray);

            return index;
        }

        object ReadObject(DBResolver resolver, JsonReader reader, Type type)
        {
            var obj = DBResolver.Instantate(type, false);
            
            if (reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.Undefined)
            {
                return obj;
            }

            Contract.Assert(reader.TokenType == JsonToken.StartObject);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        {
                            var fieldName = reader.Value.ToString();
                            var field = type.GetField(fieldName);
                            if (field == null)
                            {
                                Debug.LogWarning($"field {fieldName} not found in {type.FullName}");
                                reader.Skip();
                                break;
                            }

                            reader.Read();

                            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Ref<>))
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonToken.Undefined:
                                    case JsonToken.Null:
                                        break;
                                    case JsonToken.String:
                                        resolver.AddField(obj, field, (string)reader.Value);
                                        break;
                                    default:
                                        throw new ArgumentException($"Unexcepted token {reader.TokenType}");
                                }                                
                            } else {
                                field.SetValue(obj, ReadValue(resolver, reader, field.FieldType));
                            }                            
                            break;
                        }

                    case JsonToken.EndObject:
                        goto endObject;

                    default:
                        throw new ArgumentException($"Unexcepted token {reader.TokenType}");
                }
            }
            endObject:
            Contract.Assert(reader.TokenType == JsonToken.EndObject);

            return obj;
        }

        object ReadValue(DBResolver resolver, JsonReader reader, Type type)
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(Kind<>))
                {
                    Contract.Assert(reader.TokenType == JsonToken.String);

                    return Activator.CreateInstance(type, reader.Value.ToString());
                }
                else if (genericType == typeof(List<>))
                {
                    return ReadList(resolver, reader, type, type.GetGenericArguments()[0]);
                }
                else
                {
                    Debug.LogWarning($"Unknown field type {type.FullName}");
                    reader.Skip();
                    return default;
                }
            }
            else if (type.IsEnum)
            {
                Enum.TryParse(type, reader.Value.ToString(), out var result);
                return result;
            } else if (type == typeof(bool))
            {
                return (bool)reader.Value;
            }
            else if (type == typeof(int))
            {
                return Convert.ToInt32(reader.Value);
            }
            else if (type == typeof(float))
            {
                return Convert.ToSingle(reader.Value);
            } else if (type == typeof(string))
            {
                return (string)reader.Value;
            } else if (type == typeof(AssetReference))
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Undefined:
                    case JsonToken.Null:
                        return null;
                    case JsonToken.String:
                        return new AssetReference((string)reader.Value);
                    default:
                        throw new ArgumentException($"Unexcepted token {reader.TokenType}");

                }
            } else if (type == typeof(Color)) {
                switch (reader.TokenType)
                {
                    case JsonToken.Undefined:
                    case JsonToken.Null:
                        return Color.black;
                    case JsonToken.StartConstructor:
                        Contract.Assert((string)reader.Value == "Color", "Unknown constructor Color");
                        var r = (float)reader.ReadAsDouble();
                        var g = (float)reader.ReadAsDouble();
                        var b = (float)reader.ReadAsDouble();
                        var a = (float)reader.ReadAsDouble();
                        reader.Read();
                        Contract.Assert(reader.TokenType == JsonToken.EndConstructor, "Excepted end of constructor");

                        return new Color(r, g, b, a);
                }
            } else if (type == typeof(AnimationCurve)) {
                Contract.Assert(reader.TokenType == JsonToken.StartArray);
                var keys = new List<Keyframe>();
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.StartConstructor:
                            Contract.Assert((string)reader.Value == "Key", "Unknown constructor Key");
                            keys.Add(new Keyframe(
                                ReadFloat(reader),
                                ReadFloat(reader),
                                ReadFloat(reader),
                                ReadFloat(reader),
                                ReadFloat(reader),
                                ReadFloat(reader)));
                            reader.Read();
                            Contract.Assert(reader.TokenType == JsonToken.EndConstructor, "Excepted end of constructor");
                            break;

                        case JsonToken.EndArray:
                            return new AnimationCurve(keys.ToArray());

                        default:
                            throw new ArgumentException($"Unexcepted token {reader.TokenType}");
                    }
                }
            } else if (type.IsClass)
            {
                return ReadObject(resolver, reader, type);
            }

            Debug.LogWarning($"Unknown field type {type.FullName}");
            reader.Skip();
            return default;
        }


        object ReadList(DBResolver resolver, JsonReader reader, Type listType, Type itemType)
        {
            var list = Activator.CreateInstance(listType);
            if (reader.TokenType == JsonToken.Undefined || reader.TokenType == JsonToken.Null)
            {
                return list;
            }

            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Ref<>))
            {
                var i = 0;
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.Undefined:
                        case JsonToken.Null:
                            break;

                        case JsonToken.String:
                            resolver.AddListRef(list, (string)reader.Value);
                            break;
                        
                       case JsonToken.EndArray:
                            goto endArray;

                        default:
                            throw new ArgumentException($"Unexcepted token {reader.TokenType}");
                    }
                    i++;
                }
            }
            else
            {
                var add = listType.GetMethod("Add");
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.Undefined:
                        case JsonToken.Null:
                        case JsonToken.Boolean:
                        case JsonToken.Integer:
                        case JsonToken.Float:
                        case JsonToken.String:
                        case JsonToken.StartObject:
                            var value = ReadValue(resolver, reader, itemType);
                            add.Invoke(list, new[] { value });
                            break;

                        case JsonToken.EndArray:
                            goto endArray;

                        default:
                            Debug.Log(reader.Value);
                            throw new ArgumentException($"Unexcepted token {reader.TokenType}");
                    }
                }
            }

            endArray:
            Contract.Assert(reader.TokenType == JsonToken.EndArray);

            return list;
        }

        float ReadFloat(JsonReader reader)
        {
            reader.Read();
            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                    return (int)reader.Value;
                case JsonToken.Float:
                    var d = (double)reader.Value;
                    return (float)d;
                case JsonToken.String:
                    switch ((string)reader.Value)
                    {
                        case "Infinity": return float.PositiveInfinity;
                        case "-Infinity": return float.NegativeInfinity;
                        default: throw new ArgumentException($"Unexcepted float value {reader.Value}");

                    }

                default:
                    throw new ArgumentException($"Unexcepted token {reader.TokenType}");
            }
        }
    }
}
