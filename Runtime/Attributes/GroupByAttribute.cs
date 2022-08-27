using System;

namespace FDB
{
    public sealed class GroupByAttribute : Attribute
    {
        public readonly string Field;

        public GroupByAttribute(string field)
        {
            Field = field;
        }
    }
}