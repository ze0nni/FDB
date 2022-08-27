using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GroupByAttribute : Attribute
    {
        public readonly string Field;

        public GroupByAttribute(string field)
        {
            Field = field;
        }
    }
}