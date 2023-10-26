using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FuryDBAttribute : Attribute
    {
        public readonly string SourcePath;
        public readonly string CsGenPath;

        public FuryDBAttribute(string sourcePath, string csGenPath)
        {
            SourcePath = sourcePath;
            CsGenPath = csGenPath;
        }
    }
}
