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
        readonly T _config;
        private DBResolver _resolver;

        public Ref(DBResolver resolver, T config)
        {
            _config = config;
            _resolver = resolver;
        }

        public T Config
        {
            get
            {
#if UNITY_EDITOR
                if (_config == null)
                {
                    return null;
                }
                var index = _resolver.GetIndex(typeof(T));
                return index.Contains(_config) ? _config : null;
#else
                return _config;
#endif
            }
        }

        public Kind<T> Kind
        {
            get
            {
                var cfg = Config;
                if (cfg == null)
                {
                    return new Kind<T>(String.Empty);
                }
                var kindField = typeof(T).GetField("Kind");
                return (Kind<T>)kindField.GetValue(cfg);
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
            return Config == other.Config;
        }

        public bool Equals(Ref other)
        {
            switch (other)
            {
                case Ref<T> otherT:
                    return this._config == otherT._config;
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

        public override int GetHashCode()
        {
            return Kind.Value == null ? 0 : Kind.Value.GetHashCode();
        }

        public override string ToString()
        {
            return $"Ref<{typeof(T).Name}>({((Ref)this).Kind.Value})";
        }
    }
}
