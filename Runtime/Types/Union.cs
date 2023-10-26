using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

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

    internal static class UnionValidator
    {
        public static void Validate(Type unionType, Type tagType)
        {
            if (tagType == typeof(string))
            {
                // Ok
                return;
            }

            if (tagType.IsEnum)
            {
                var fields = unionType.GetFields().Select(f => f.Name);
                var tags = tagType.GetFields(BindingFlags.Static|BindingFlags.Public).Where(f => f.FieldType == tagType).Select(f => f.Name);
                var lost = fields.Where(x => !tags.Contains(x)).ToArray();
                var excess = tags.Where(x => !fields.Contains(x)).ToArray();
                if (lost.Length != 0 || excess.Length != 0)
                {
                    throw new SchemaException($"{unionType.Name}<{tagType.Name}> enum keys not matchs with union fields:\nLost: {string.Join(", ", lost) }\nExpress: { string.Join(", ", excess) }");
                }

                // Ok
                return;
            }

            throw new SchemaException($"Type of <T> must be string or enum but {unionType.Name}<{tagType.Name}>");
        }
    }
}