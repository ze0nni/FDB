using System;

namespace FDB.Editor
{
    public class PageState
    {
        public string Title;
        public Type IndexType;
        public Type ModelType;
        public Func<object, object> ResolveModel;

        public Aggregator Aggregator;

        public HeaderState[] Headers;
        public bool IsPaintedOnce;
    }
}