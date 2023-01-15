using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FDB
{
    public interface Index
    {
        IEnumerable All();
        void SetDirty();

        bool IsDuplicateKind(string kind);
        IReadOnlyList<string> Warnings { get; }
        
        bool TryGet(string kind, out object model);
        void Add(object item);
        void Insert(int index, object item);
        void Swap(int i0, int i1);
        void Remove(int index);
    }

    public sealed class Index<T> : Index where T : class
    {
        readonly List<T> _list = new List<T>();
        readonly Dictionary<string, T> _map = new Dictionary<string, T>();

        readonly List<string> _warnings = new List<string>();
        readonly HashSet<string> _duplicates = new HashSet<string>();

        public IReadOnlyList<T> All()
        {
            return _list;
        }

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
        public T this[Kind<T> kind] => _map[kind.Value];

        void Invalidate()
        {
            if (_map.Count > 0)
                return;
            _warnings.Clear();
            _duplicates.Clear();

            var kindField = typeof(T).GetField("Kind");

            foreach (var i in _list)
            {
                var kind = kindField.GetValue(i) as Kind;

                if (_map.ContainsKey(kind.Value))
                {
                    _warnings.Add($"Duplicate kind={kind.Value}");
                    _duplicates.Add(kind.Value);
                }
                _map[kind.Value] = i;
            }
        }

        void Index.SetDirty()
        {
            _map.Clear();
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

        IEnumerable Index.All()
        {
            return _list;
        }

        void Index.Add(object item)
        {
            _list.Add((T)item);
            ((Index)this).SetDirty();
        }

        void Index.Insert(int index, object item)
        {
            _list.Insert(index, (T)item);
            ((Index)this).SetDirty();
        }

        void Index.Swap(int i0, int i1)
        {
            var t = _list[i0];
            _list[i0] = _list[i1];
            _list[i1] = t;

        }

        void Index.Remove(int index)
        {
            _list.RemoveAt(index);
            ((Index)this).SetDirty();
        }
    }
}
