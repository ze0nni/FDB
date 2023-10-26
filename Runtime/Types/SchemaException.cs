using System;
using UnityEngine;

namespace FDB
{
    public class SchemaException : Exception
    {
        public SchemaException(string message): base(message)
        {

        }
    }
}