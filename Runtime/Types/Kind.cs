using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace FDB
{
    public interface Kind
    {
        string Value { get; }
    }

    [JsonConverter(typeof(KindJsonConverter))]
    public readonly struct Kind<T> : Kind, ISerializable
    {
        public readonly string Value;
        public Kind(string value) => Value = value;

        string Kind.Value => Value ?? string.Empty;
        
        public override string ToString()
        {
            return $"{nameof(Kind)}<{typeof(T).Name}>({Value})";
        }

        public Kind(SerializationInfo info, StreamingContext context)
        {
            Value = info.GetString("Value");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Value", Value);
        }
    }

    public sealed class KindJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType
                && objectType.GetGenericTypeDefinition() == typeof(Kind<>);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Undefined:
                case JsonToken.Null:
                    return Activator.CreateInstance(objectType, string.Empty);
                case JsonToken.String:
                    return Activator.CreateInstance(objectType, (string)reader.Value);
                default:
                    throw new JsonReaderException($"Unexcepted token type {reader.TokenType} when parse {objectType}");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((Kind)value).Value);
        }
    }
}
