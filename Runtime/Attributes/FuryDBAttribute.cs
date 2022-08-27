using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FuryDBAttribute : Attribute
    {
        public readonly string SourcePath;
        public readonly string CsPath;

        public FuryDBAttribute(string sourcePath, string csPath)
        {
            SourcePath = sourcePath;
            CsPath = csPath;
        }
    }
}
