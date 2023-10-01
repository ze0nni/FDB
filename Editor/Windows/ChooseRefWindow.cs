using System;
using System.Collections.Generic;
using System.Linq;

namespace FDB.Editor
{
    public class ChooseRefWindow : BaseChooseWindow<string> 
    {
        public ChooseRefWindow(
            string selected,
            IEnumerable<string> sources,
            Action<string> onSelect
            ) : base(
                selected,
                (new string[] { null }).Concat(sources),
                onSelect)
        {
        }

        protected override bool Filter(string item, string text)
        {
            return item == null || item.Contains(text);
        }

        protected override string ItemText(string item)
        {
            return item == null ? "null" : item;
        }
    }
}
