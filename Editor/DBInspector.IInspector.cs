namespace FDB.Editor {

    public interface IInspector
    {
        void ToggleExpandedState(object config, HeaderState header);
        bool TryGetExpandedHeader(object config, HeaderState[] headers, out HeaderState header, out float headerLeft);
    }

    public partial class DBInspector<T> : IInspector
    {
        public void ToggleExpandedState(object config, HeaderState header)
        {
            if (!DBResolver.GetGUID(config, out var guid))
            {
                return;
            }

            if (_expandedFields.TryGetValue(guid, out var storedField) && storedField == header.Title)
            {
                _expandedFields.Remove(guid);
                _expandedOrder.Remove(guid);
            }
            else
            {
                _expandedFields[guid] = header.Title;
                _expandedOrder.Remove(guid);
                _expandedOrder.Add(guid);
            }

            while (_expandedOrder.Count > MaxExpandedHistory)
            {
                _expandedFields.Remove(_expandedOrder[0]);
                _expandedOrder.RemoveAt(0);
            }

            _needRepaint = true;
        }

        public bool TryGetExpandedHeader(object config, HeaderState[] headers, out HeaderState header, out float headerLeft)
        {
            headerLeft = 0;
            if (!DBResolver.GetGUID(config, out var guid)
                || !_expandedFields.TryGetValue(guid, out var field))
            {
                header = null;
                return false;
            }

            foreach (var h in headers)
            {
                if (h.Separate)
                {
                    headerLeft += GUIConst.HeaderSeparator;
                }
                if (h.Title == field)
                {
                    header = h;
                    return true;
                }
                headerLeft += h.Width + GUIConst.HeaderSpace;
            }

            header = null;
            return false;
        }
    }
}