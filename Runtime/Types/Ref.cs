using System;

namespace FDB
{
    public interface Ref : IEquatable<Ref>
    {
        Kind Kind { get; }
        object Model { get; }
    }

    public struct Ref<T> : Ref where T : class
    {
        public readonly T Model;
        private DBResolver _resolver;

        public Ref(DBResolver resolver, T model)
        {
            Model = model;
            _resolver = resolver;
        }

        public Kind<T> Kind
        {
            get
            {
                if (Model == null)
                {
                    return new Kind<T>(String.Empty);
                }
                var kindField = typeof(T).GetField("Kind");
                return (Kind<T>)kindField.GetValue(Model);
            }
        }

        Kind Ref.Kind => Kind;
        object Ref.Model => Model;

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
                    return this.Model == otherT.Model;
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
