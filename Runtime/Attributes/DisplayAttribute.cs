using System;


namespace FDB
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisplayAttribute : Attribute
    {
        public string[] Fields;

        public DisplayAttribute(params string[] fields)
        {
            Fields = fields;
        }
    }
}
