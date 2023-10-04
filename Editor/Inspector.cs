using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB.Editor
{
    public static partial class Inspector
    {
        public static bool ApplyFilter(object item, string filter)
        {
            if (item == null)
            {
                return "null".Contains(filter, StringComparison.CurrentCultureIgnoreCase);
            }
            foreach (var field in item.GetType().GetFields())
            {
                var value = field.GetValue(item);
                if ((value?.ToString() ?? "null").Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        internal static string ToString(object obj)
        {
            switch (obj)
            {
                case bool _:
                case int _:
                case float _:
                case string _:
                    return obj.ToString();
                case Kind kind:
                    return kind.Value;
            }
            return obj.ToString();
        }
    }
}