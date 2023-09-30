#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace FDB
{
    public sealed partial class DBResolver
    {
        public const string __GUID = "__GUID";
        public static string EmptyGUID = new Guid().ToString();

        static Dictionary<Type, FieldInfo> _GUIDFiledCache = new Dictionary<Type, FieldInfo>();
        static HashSet<Type> _warnAboutGUIDField = new HashSet<Type>();

        static bool GetGUIDField(object obj, out FieldInfo guidField)
        {
            var type = obj.GetType();
            if (!_GUIDFiledCache.TryGetValue(type, out guidField))
            {
                guidField = type.GetField(__GUID);
                _GUIDFiledCache.Add(type, guidField);
            }
            return guidField != null;
        }

        static public bool GetGUID(object obj, out string guid)
        {
            GetGUIDField(obj, out var guidField);
            if (guidField == null && _warnAboutGUIDField.Add(obj.GetType()))
            {
                Debug.LogWarning($"Type {obj.GetType().Name} has no field {__GUID}");
            }
            if (guidField == null)
            {
                guid = default;
                return false;
            }
            guid = (string)guidField.GetValue(obj);
            return guid != null;
        }

        static ModuleBuilder _moduleBuilder;
        static Dictionary<Type, Type> _internalTypes = new Dictionary<Type, Type>();
        static ModuleBuilder GetModuleBulder()
        {
            if (_moduleBuilder == null)
            {
                var assemblyName = new AssemblyName($"FDB.Internal");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var module = assemblyBuilder.DefineDynamicModule("FDB.Internal");
                _moduleBuilder = module;
            }
            return _moduleBuilder;
        }

        internal static Type Wrap(Type originType)
        {
            if (_internalTypes.TryGetValue(originType, out var internalType))
            {
                return internalType;
            }
            if (DBResolver.IsSupportedUnityType(originType))
            {
                _internalTypes.Add(originType, originType);
                return originType;
            }
            if (!originType.IsClass)
            {
                _internalTypes.Add(originType, originType);
                return originType;
            }
            if (originType.GetField(__GUID) != null)
            {
                _internalTypes.Add(originType, originType);
                return originType;
            }

            var module = GetModuleBulder();

            var typeBuilder = module
                .DefineType($"{originType.FullName}`FBDInternal",
                TypeAttributes.Class,
                originType);

            typeBuilder.DefineField(__GUID,
                typeof(string),
                FieldAttributes.Public);


            internalType = typeBuilder.CreateType();
            _internalTypes.Add(originType, internalType);

            return internalType;
        }

        internal static object WrapObj(object origin)
        {
            var originType = origin.GetType();
            var wrapType = Wrap(originType);
            if (originType == wrapType)
            {
                return origin;
            }
            var wrap = DBResolver.Instantate(wrapType, false);

            foreach (var f in originType.GetFields())
            {
                f.SetValue(wrap, f.GetValue(origin));
            }

            return wrap;
        }

        internal static bool Invalidate(object obj)
        {
            if (!GetGUIDField(obj, out var guidField))
            {
                return false;
            }
            var guid = (string)guidField.GetValue(obj);
            if (guid != null && guid.Length == EmptyGUID.Length)
            {
                return false;
            }
            guidField.SetValue(obj, Guid.NewGuid().ToString());
            return true;
        }
    }
}
#else
namespace FDB
{
    public sealed partial class DBResolver
    {
        public const string __GUID = "--GUID";

        static public bool GetGUID(object obj, out string guid)
        {
            guid = null;
            return false;
        }

        internal static System.Type Wrap(System.Type originType) => originType;

        internal static object WrapObj(object origin) => origin;

        internal static bool Invalidate(object obj)
        {
            return false;
        }
    }
}
#endif
