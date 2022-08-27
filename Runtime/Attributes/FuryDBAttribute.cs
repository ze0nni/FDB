using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FuryDBAttribute : Attribute
    {
        public readonly string Path;
        public FuryDBAttribute(string path)
        {
            Path = path;
        }
    }
}
