using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace FDB
{
    public class IndexSource<T, G> : Index, IEnumerable<T>
        where T : class
        where G : IIndexSourceGenerator<T>
    {
        readonly bool _isPlayMode;
        readonly IIndexSourceGenerator<T> _generator;

        readonly List<T> _list = new List<T>();
        readonly Dictionary<string, T> _map = new Dictionary<string, T>();
        readonly HashSet<T> _configs = new HashSet<T>();

        readonly List<string> _warnings = new List<string>();
        readonly HashSet<string> _duplicates = new HashSet<string>();

        bool _isValid = false;

        public IndexSource(bool isPlayMode, FieldInfo field)
        {
            _isPlayMode = isPlayMode;
            if (!isPlayMode)
            {
                _generator = (IIndexSourceGenerator<T>)Activator.CreateInstance(typeof(G));
                _generator.Setup(field);
            }
        }

        public Type ConfigType => typeof(T);
        public bool Readonly => true;

        public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();

        public T Get(Kind<T> kind)
        {
            Invalidate();
            return _map[kind.Value];
        }

        public bool TryGet(Kind<T> kind, out T e)
        {
            Invalidate();
            if (kind.Value == null)
            {
                e = default;
                return false;
            }
            return _map.TryGetValue(kind.Value, out e);
        }

        public int Count => _list.Count;
        public T this[int index] => _list[index];
        public T this[Kind<T> kind] => Get(kind);

        public void Invalidate()
        {
            if (_isValid)
            {
                return;
            }

            if (!_isPlayMode)
            {
                _list.Clear();
                foreach (var i in _generator.Generate())
                {
                    _list.Add(i);
                }
            }

            _map.Clear();
            _configs.Clear();
            _warnings.Clear();
            _duplicates.Clear();

            var kindField = typeof(T).GetField("Kind");

            foreach (var config in _list)
            {
                var kind = kindField.GetValue(config) as Kind;

                if (_map.ContainsKey(kind.Value))
                {
                    _warnings.Add($"Duplicate kind='{kind.Value}'");
                    _duplicates.Add(kind.Value);
                }

                _map[kind.Value] = config;
                _configs.Add(config);
            }

            _isValid = true;
        }

        void Index.SetDirty()
        {
            throw new NotImplementedException();
        }

        IReadOnlyList<string> Index.Warnings => _warnings;

        bool Index.IsDuplicateKind(string kind)
        {
            Invalidate();
            return _duplicates.Contains(kind);
        }

        bool Index.TryGet(string kind, out object model)
        {
            Invalidate();

            var result = _map.TryGetValue(kind, out var outModel);
            model = outModel;
            return result;
        }

        bool IList.Contains(object config)
        {
            Invalidate();
            return _configs.Contains((T)config);
        }

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();

        object IList.this[int index]
        {
            get => _list[index];
            set
            {
                _list[index] = (T)value;
                ((Index)this).SetDirty();
            }
        }

        bool IList.IsFixedSize => true;
        bool IList.IsReadOnly => true;

        int IList.IndexOf(object value) => _list.IndexOf((T)value);

        int IList.Add(object item)
        {
            if (_isPlayMode)
            {
                _isValid = false;
                _list.Add((T)item);
                return _list.Count;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        void IList.Clear() => throw new NotImplementedException();
        void IList.Insert(int index, object item) => throw new NotImplementedException();
        void IList.Remove(object value) => throw new NotImplementedException();
        void IList.RemoveAt(int index) => throw new NotImplementedException();
        void Index.Swap(int i0, int i1) => throw new NotImplementedException();
    }
}
