using System;
using System.Text.RegularExpressions;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GroupByAttribute : Attribute
    {
        public readonly string Field;
        public readonly String Regex;
        public readonly int RegexGroup;

        public GroupByAttribute(string field = null, string regex = null, int regexGroup = 1)
        {
            Field = field;
            Regex = regex;
            RegexGroup = regexGroup;
        }
    }
}