using System;
using System.Reflection;

namespace FDB.Editor
{
    public sealed class ListHeaderState : HeaderState
    {
        public readonly FieldInfo Field;
        public readonly Type ItemType;
        public readonly bool Primitive;
        public readonly Aggregator Aggregator;

        public ListHeaderState(string path, Type ownerType, FieldInfo field, Type itemType, bool primitive, HeaderState[] headers)
            : base(path, null, field.Name, headers)
        {
            Field = field;
            ItemType = itemType;
            Primitive = primitive;
            Aggregator = new Aggregator(ownerType, field, itemType);
        }

        public override object Get(object config, int? collectionIndex)
        {
            throw new NotImplementedException();
        }

        public override void Set(object config, int? collectionIndex, object value)
        {
            throw new NotImplementedException();
        }
    }
}
