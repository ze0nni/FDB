using System;

namespace FDB
{
    public partial class DBConverter
    {
        private readonly Type _dbType;
        private readonly DBResolver _resolver;

        public DBConverter(Type dbType, DBResolver resolver)
        {
            _dbType = dbType;
            _resolver = resolver;
        }
    }
}