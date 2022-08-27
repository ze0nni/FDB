using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FDB
{
    public sealed partial class DBConverter<T>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();            

            var type = value.GetType();
            foreach (var field in type.GetFields())
            {
                var fieldType = field.FieldType;
                if (fieldType.IsGenericType)
                {
                    var genericFieldType = fieldType.GetGenericTypeDefinition();
                    if (genericFieldType == typeof(Index<>))
                    {
                        writer.WritePropertyName(field.Name);
                        WriteIndex(writer, (Index)field.GetValue(value));
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

        void WriteObject(JsonWriter writer, object model)
        {
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
                } else if (genericType == typeof(Ref<>)) {
                    writer.WriteValue(((Ref)value).Kind.Value);
                } else if (genericType == typeof(List<>))
                {
                    var itemType = type.GetGenericArguments()[0];
                    var collection = (IEnumerable)value;

                    writer.WriteStartArray();
                    foreach (var i in collection)
                    {
                        WriteValue(writer, itemType, i);
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
            } else
            {
                writer.WriteUndefined();
            }
        }
    }
}
