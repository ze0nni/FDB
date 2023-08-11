using System;
using UnityEngine;

namespace FDB.Editor
{
    public class PageState
    {
        public string Title;
        public Type IndexType;
        public Type ModelType;
        public Func<object, object> ResolveModel;

        public Vector2 Position;
        public string Filter;
        public Aggregator Aggregator;

        public HeaderState[] Headers;
    }
}