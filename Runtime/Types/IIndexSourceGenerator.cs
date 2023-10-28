using System.Collections.Generic;
using System.Reflection;

namespace FDB
{
    public interface IIndexSourceGenerator<T>
    {
        void Setup(FieldInfo field);
        IEnumerable<T> Generate();
    }
}