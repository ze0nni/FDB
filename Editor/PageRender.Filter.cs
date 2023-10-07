using System.Collections.Generic;
using UnityEngine;

namespace FDB.Editor
{
    partial class PageRender
    {
        Index _currentFilterPage;
        string _currentFilter;
        readonly Dictionary<object, bool> _filterCache = new Dictionary<object, bool>();

        void InvalidateFilter(Index index, string filter)
        {
            if (_currentFilterPage != index || _currentFilter != filter)
            {
                _currentFilterPage = index;
                _currentFilter = filter;
                _filterCache.Clear();
            }
        }

        bool FilterConfig(object config, Header[] headers)
        {
            if (string.IsNullOrEmpty(_currentFilter))
            {
                return true;
            }
            var filter = _currentFilter;
            if (!_filterCache.TryGetValue(config, out var result))
            {
                foreach (var h in headers)
                {
                    if (h.Filter(config, filter))
                    {
                        result = true;
                        break;
                    };
                }
                _filterCache.Add(config, result);
            }
            return result;
        }
    }
}
