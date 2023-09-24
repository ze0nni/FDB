using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FDB
{
    public interface Kind
    {
        string Value { get; }
        bool CanExport { get; }
    }

    [Serializable]
    [JsonConverter(typeof(KindJsonConverter))]
    public readonly struct Kind<T> : Kind , IEquatable<Kind<T>>
    {
        static Regex KindPatter = new Regex(@"^[a-zA-Z][\w_]*$");

        public readonly string Value;
        public Kind(string value) => Value = value;

        string Kind.Value => Value ?? string.Empty;

        public bool CanExport => Value != null && KindPatter.IsMatch(Value);

        public override string ToString()
        {
            return $"{nameof(Kind)}<{typeof(T).Name}>({Value})";
        }

        public static bool operator ==(Kind<T> a, Kind<T> b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(Kind<T> a, Kind<T> b)
        {
            return a.Value != b.Value;
        }

        public bool Equals(Kind<T> other)
        {
            return this.Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Kind<T>))
            {
                return false;
            }

            var kind = (Kind<T>)obj;
            return Value == kind.Value;
        }

        public override int GetHashCode()
        {
            var hashCode = 1637693444;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
            return hashCode;
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
