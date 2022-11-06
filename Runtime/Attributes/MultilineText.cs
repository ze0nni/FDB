using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MultilineText :  Attribute
    {
        public int MinLines;
        public int MaxLines;
        public string Condition;
    }
}