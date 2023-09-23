using System;

namespace FDB.Components
{
    [Serializable]
    public class TextValue<TDB, TConfig>
    {
        public bool Translate = true;
        public string Value = "Text";
    }
}
