using System.Collections;
using UnityEngine;

namespace FDB.Components
{
    public abstract class TextComponentBase<TDB, TConfig> : MonoBehaviour where TConfig : class
    {
        [SerializeField] TextValue<TDB, TConfig> _text;
        object[] _args;

        object[] _arg1;
        object[] _arg2;
        object[] _arg3;

        public bool Translate => _text.Translate;

        public void SetText(string text, bool translate = false)
        {
            _text.Translate = translate;
            _text.Value = text;
            SetDirty();
        }

        public void SetText(Kind<TConfig> kind)
        {
            _text.Translate = true;
            _text.Value = kind.Value;
            SetDirty();
        }

        public void SetArgs()
        {
            _args = null;
            SetDirty();
        }

        public void SetArgs<A>(A a)
        {
            _arg1 = _arg1 ?? new object[1];
            _arg1[0] = a;

            _args = _arg1;
            SetDirty();
        }

        public void SetArgs<A, B>(A a, B b)
        {
            _arg2 = _arg2 ?? new object[2];
            _arg2[0] = a;
            _arg2[1] = b;

            _args = _arg2;
            SetDirty();
        }

        public void SetArgs<A, B, C>(A a, B b, C c)
        {
            _arg3 = _arg3 ?? new object[3];
            _arg3[0] = a;
            _arg3[1] = b;
            _arg3[2] = c;

            _args = _arg3;
            SetDirty();
        }

        public void SetArgs(params object[] args)
        {
            _args = args;
            SetDirty();
        }

        private void OnEnable()
        {
            StartRender();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _isDirty = false;
            SetDirty();
        }
#endif

        protected abstract Index<TConfig> Index { get; }
        protected abstract string GetText(TConfig config);
        protected abstract void Render(string text);

        private bool _isDirty = false;
        private IEnumerator _render;
        protected void SetDirty()
        {
            if (_isDirty)
            {
                return;
            }
            StartRender();
        }

        void StartRender()
        {
            _isDirty = true;
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            if (_render == null)
            {
                _render = new RenderTask(this);
            }
            _render.Reset();

            StartCoroutine(_render);
        }

        void Render()
        {
            _isDirty = false;

            Index<TConfig> index;
#if UNITY_EDITOR
            index = Application.isPlaying
                ? Index
                : (Index<TConfig>)FDB.Editor.EditorDB<TDB>.Resolver.GetIndex(typeof(TConfig));
#else
            index = Index;
#endif
            string format;
            if (_text.Translate)
            {
                index.TryGet(new Kind<TConfig>(_text.Value), out var config);
                format = config != null
                    ? GetText(config)
                    : _text.Value;
            }
            else
            {
                format = _text.Value;
            }

            var text = _args != null && _args.Length > 0
                ? string.Format(format, _args)
                : format;

            Render(text);
        }

        private class RenderTask : IEnumerator
        {
            readonly TextComponentBase<TDB, TConfig> _text;

            public RenderTask(TextComponentBase<TDB, TConfig> text)
            {
                _text = text;
            }

            int _index;

            public object Current => null;

            public bool MoveNext()
            {
                switch (_index++)
                {
                    case 0:
                        return true;
                    case 1:
                        _text.Render();
                        return false;
                    default:
                        return false;
                }
            }

            public void Reset()
            {
                _index = 0;
            }
        }
    }
}
