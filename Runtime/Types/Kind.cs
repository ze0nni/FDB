namespace FDB
{
    public interface Kind
    {
        string Value { get; }
    }

    public readonly struct Kind<T> : Kind
    {
        public readonly string Value;
        public Kind(string value) => Value = value;

        string Kind.Value => Value ?? string.Empty;

        public override string ToString()
        {
            return $"{nameof(Kind)}<{typeof(T).Name}>({Value})";
        }
    }
}
