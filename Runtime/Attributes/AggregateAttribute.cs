using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class AggregateAttribute : Attribute
    {
        public readonly string Name;
        public readonly String AggregateFuncName;
        public readonly Type InitialType;

        public AggregateAttribute(string name, string aggregateFuncName, Type initialType = null)
        {
            Name = name;
            AggregateFuncName = aggregateFuncName;
            InitialType = initialType;
        }

        public AggregateAttribute(string aggregateFuncName, Type initialType = null)
        {
            Name = aggregateFuncName;
            AggregateFuncName = aggregateFuncName;
        }
    }
}