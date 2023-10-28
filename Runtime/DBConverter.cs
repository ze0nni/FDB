using System;

namespace FDB
{
    public partial class DBConverter
    {
        public delegate UnityEngine.Object UnityResolverDelegate(string guid, Type type);

        private readonly Type _dbType;
        private readonly DBResolver _resolver;
        private UnityResolverDelegate _unityObjectsResolver;

        public DBConverter(
            bool isPlayMode,
            Type dbType,
            DBResolver resolver,
            UnityResolverDelegate unityObjectsResolver
            )
        {
            _dbType = dbType;
            _resolver = resolver;
            _unityObjectsResolver = unityObjectsResolver;
        }
    }
}