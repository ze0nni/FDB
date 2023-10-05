using System.Collections;
using UnityEngine;

namespace FDB.Editor {

    public interface IInspector
    {
        void Repaint();

        InputStateBase InputState { get; }
        bool OnInput<S>(out S state) where S : InputStateBase;
        void SetInput(InputStateBase state);
        void ResetInput();

        void ToggleExpandedState(object config, HeaderState header);
        bool TryGetExpandedHeader(object config, HeaderState[] headers, out HeaderState header, out float headerLeft);
    }

    public abstract class InputStateBase
    {
    }

    public sealed class InputResizeHeader : InputStateBase
    {
        public readonly HeaderState Header;
        public readonly float StartWidth;
        public readonly Vector2 StartMouse;

        public InputResizeHeader(HeaderState header, Vector2 startMouse)
        {
            Header = header;
            StartWidth = header.Width;
            StartMouse = startMouse;
        }
    }

    public sealed class InputDragRow : InputStateBase
    {
        public readonly object Config;
        public readonly IList Collection;
        public readonly int CollectionIndex;
        public InputDragRow(object config, IList collection, int collectionIndex)
        {
            Config = config;
            Collection = collection;
            CollectionIndex = collectionIndex;
        }
    }

    public partial class DBInspector<T> : IInspector
    {
        void IInspector.Repaint()
        {
            _needRepaint = true;
        }

        public InputStateBase InputState { get; private set; }
        Rect _fixedContentRect;

        public bool OnInput<S>(out S state) where S : InputStateBase
        {
            if (InputState != null && InputState.GetType() == typeof(S))
            {
                state = (S)InputState;
                return true;
            }
            state = default;
            return false;
        }
        public void SetInput(InputStateBase state)
        {
            InputState = state;
            _needRepaint = true;
            _fixedContentRect = Render.Content;
        }

        public void ResetInput()
        {
            SetInput(null);
        }

        public bool GetFixedContentRect(out Rect contentRect)
        {
            contentRect = _fixedContentRect;
            return InputState != null;
        }

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