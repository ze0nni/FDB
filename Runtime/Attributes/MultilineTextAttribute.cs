using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MultilineTextAttribute :  Attribute
    {
        public int MinLines;
        public int MaxLines;
        public string Condition;
    }
}