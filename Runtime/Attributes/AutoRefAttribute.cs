using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class AutoRefAttribute : Attribute
    {
        public string Prefix;
        public string Suffix;
        public string Field;

        static StringBuilder _sb = new StringBuilder();

        public string GetKind(object owner)
        {
            if (Prefix != null)
            {
                var field = FieldToString(owner);
                if (field == null)
                {
                    return null;
                }

                _sb.Clear();
                _sb.Append(Prefix);
                _sb.Append(field);
                if (Suffix != null)
                {
                    _sb.Append(Suffix);
                }
                return _sb.ToString();
            }
            return null;
        }

        private string FieldToString(object owner)
        {
            var fieldName = Field ?? "Kind";
            var field = owner.GetType().GetField(fieldName);
            if (field == null)
            {
                Debug.LogWarning($"Object {owner.GetType()} has no field {fieldName}");
                return null;
            }
            var value = field.GetValue(owner);
            switch (value)
            {
                case null:
                    return null;
                case Kind kind:
                    return kind.Value;
                case string str:
                    return str.Length == 0 ? null : str;
                default:
                    return value.ToString();
            }
        }

        public int GetInsertIndex(object owner, Index index)
        {
            var kinds = index.GetKinds().ToArray();
            if (Prefix != null)
            {
                var lastIndex = -1;
                var i = 0;
                foreach (var kind in kinds)
                {
                    if (kind.StartsWith(Prefix))
                    {
                        lastIndex = i + 1;
                    }
                    i++;
                }
                if (lastIndex != -1)
                    return lastIndex;
            }

            return kinds.Length;
        }
    }
}
