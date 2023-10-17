using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FuryDB.Editor")]
[assembly: InternalsVisibleTo("FuryDB.Editor.dll")]

namespace FDB
{
    public abstract class Union : UnionBase<string>
    {

    }

    public abstract class Union<T> : UnionBase<T>
    {

    }

    public abstract class UnionBase
    {
        internal protected abstract string UnionTagString { get; set; }
        internal protected void SetUnionTagStringSafe(string value)
        {
            try
            {
                UnionTagString = value;
            } catch(Exception exc)
            {
                //
            }
        }
    }

    public abstract class UnionBase<T> : UnionBase
    {
        private string _unionTagString;

        internal UnionBase()
        {
            if (typeof(T) == typeof(string))
            {
                //
            } else if (typeof(T).IsEnum)
            {
                //
            } else
            {
                throw new ArgumentException("Type of <T> must be string or enum");
            }
        }

        internal bool IsEnum => typeof(T).IsEnum;

        public T UnionTag
        {
            get {
                if (!IsEnum)
                {
                    return (T)(object)_unionTagString;
                } else
                {
                    Enum.TryParse(typeof(T), _unionTagString, out var result);
                    return (T)result;
                }
            }
            set {
                UnionTagString = value.ToString();
            }
        }

        override internal protected string UnionTagString
        {
            get => _unionTagString;
            set
            {
                if (!IsEnum)
                {
                    _unionTagString = value;
                } else
                {
                    if (Enum.TryParse(typeof(T), value, out var result))
                    {
                        _unionTagString = value;
                    } else
                    {
                        throw new ArgumentOutOfRangeException(value);
                    }
                }
            }
        }
    }
}