using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FDB
{
    public interface Index : IEnumerable, IList
    {
        Type ConfigType { get; }

        IEnumerable All();
        void SetDirty();
        void Invalidate();

        bool IsDuplicateKind(string kind);
        IReadOnlyList<string> Warnings { get; }
        
        bool TryGet(string kind, out object config);
        void Swap(int i0, int i1);
    }

    public sealed class Index<T> : Index, IEnumerable<T>
        where T : class
    {
        readonly List<T> _list = new List<T>();
        readonly Dictionary<string, T> _map = new Dictionary<string, T>();
        readonly HashSet<T> _configs = new HashSet<T>();

        readonly List<string> _warnings = new List<string>();
        readonly HashSet<string> _duplicates = new HashSet<string>();

        public Type ConfigType => typeof(T);

        [Obsolete("foreach (config in index)")]
        public IReadOnlyList<T> All()
        {
            return _list;
        }

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
            if (_map.Count > 0)
                return;
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
        }

        void Index.SetDirty()
        {
            _map.Clear();
            _configs.Clear();
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
            return _configs.Contains(config);
        }

        IEnumerable Index.All()
        {
            return _list;
        }

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();

        object IList.this[int index]
        {
            get => _list[index];
            set {
                _list[index] = (T)value;
                ((Index)this).SetDirty();
            }
        }

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => false;

        int IList.Add(object item)
        {
            item = DBResolver.WrapObj(item);
            _list.Add((T)item);
            ((Index)this).SetDirty();
            return _list.Count;
        }

        void IList.Clear()
        {
            _list.Clear();
            ((Index)this).SetDirty();
        }

        void IList.Insert(int index, object item)
        {
            _list.Insert(index, (T)item);
            ((Index)this).SetDirty();
        }

        int IList.IndexOf(object value) => _list.IndexOf((T)value);

        void IList.Remove(object value)
        {
            if (_list.Remove((T)value))
            {
                ((Index)this).SetDirty();
            }
        }

        void IList.RemoveAt(int index)
        {
            _list.RemoveAt(index);
            ((Index)this).SetDirty();
        }

        void Index.Swap(int i0, int i1)
        {
            var t = _list[i0];
            _list[i0] = _list[i1];
            _list[i1] = t;

        }
    }

    public static class IndexExtentions
    {
        public static IEnumerable<string> GetKinds(this Index index)
        {
            var itemType = index.GetType().GetGenericArguments()[0];
            var kindField = itemType.GetField("Kind");

            return index.All().Cast<object>().Select(x => ((Kind)kindField.GetValue(x)).Value);
        }
    }
}
