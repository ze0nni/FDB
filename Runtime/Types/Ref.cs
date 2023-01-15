using System;

namespace FDB
{
    public interface Ref : IEquatable<Ref>
    {
        Kind Kind { get; }
        object Config { get; }
    }

    public struct Ref<T> : Ref where T : class
    {
        public readonly T Config;
        private DBResolver _resolver;

        public Ref(DBResolver resolver, T config)
        {
            Config = config;
            _resolver = resolver;
        }

        public Kind<T> Kind
        {
            get
            {
                if (Config == null)
                {
                    return new Kind<T>(String.Empty);
                }
                var kindField = typeof(T).GetField("Kind");
                return (Kind<T>)kindField.GetValue(Config);
            }
        }

        Kind Ref.Kind => Kind;
        object Ref.Config => Config;

        public override bool Equals(object obj)
        {
            if (!(obj is Ref other))
            {
                return false;
            }
            return Kind.GetType() == other.Kind.GetType()
                && Kind.Value == other.Kind.Value;
        }

        public bool Equals(Ref other)
        {
            switch (other)
            {
                case Ref<T> otherT:
                    return this.Config == otherT.Config;
            }
            return false;
        }

        public static bool operator ==(Ref<T> a, Ref<T> b)
        {
            return a.Kind == b.Kind;
        }

        public static bool operator !=(Ref<T> a, Ref<T> b)
        {
            return a.Kind != b.Kind;
        }

        public override string ToString()
        {
            return $"Ref<{typeof(T).Name}>({((Ref)this).Kind.Value})";
        }
    }
}
