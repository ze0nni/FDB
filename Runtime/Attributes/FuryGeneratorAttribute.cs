using System;

namespace FDB
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class FuryGeneratorAttribute : Attribute
    {
        public string CsPath;
        public Type GeneratorType;

        public FuryGeneratorAttribute(string csPath, Type generatorType)
        {
            CsPath = csPath;
            GeneratorType = generatorType;
        }
    }
}