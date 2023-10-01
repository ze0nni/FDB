using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MultilineTextAttribute :  Attribute
    {
        public int MinLines;
        public string Condition;
    }
}