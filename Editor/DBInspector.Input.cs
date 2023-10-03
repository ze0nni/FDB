using System;
using UnityEngine;

namespace FDB.Editor
{
    public interface IInput
    {
        InputStateBase State { get; }
        bool Resolve<S>(out S state) where S : InputStateBase;
        void Set(InputStateBase state);
        void Reset();
        void Repaint();
    }

    public abstract class InputStateBase
    {
        public virtual bool GetFixedPageContentSize(out Rect rect) { rect = default; return false; }

        internal virtual void Set(PageRender render) { }
    }

    public sealed class InputResizeHeader : InputStateBase
    {
        public readonly HeaderState Header;
        public readonly float StartWidth;
        public readonly Vector2 StartMouse;
        Rect _fixedPageContentSize;

        public InputResizeHeader(HeaderState header, Vector2 startMouse)
        {
            Header = header;
            StartWidth = header.Width;
            StartMouse = startMouse;
        }

        internal override void Set(PageRender render)
        {
            _fixedPageContentSize = render.Content;
        }

        public override bool GetFixedPageContentSize(out Rect rect)
        {
            rect = _fixedPageContentSize;
            return true;
        }
    }

    public partial class DBInspector<T> : IInput
    {
        public IInput Input => this;

        Type _inputStateType;
        InputStateBase _inputState;

        InputStateBase IInput.State => _inputState;

        bool IInput.Resolve<S>(out S state)
        {
            if (_inputStateType == typeof(S))
            {
                state = (S)_inputState;
                return true;
            }
            state = default;
            return false;
        }

        void IInput.Set(InputStateBase state)
        {
            _inputStateType = state.GetType();
            _inputState = state;
            _inputState.Set(Render);
        }

        void IInput.Reset()
        {
            _inputStateType = null;
            _inputState = null;
            _needRepaint = true;
        }

        void IInput.Repaint()
        {
            _needRepaint = true;
        }
    }
}
