using System;
using System.Collections.Generic;
using UnityEngine;

namespace FDB
{
    public class FuryDBEntryAsset : ScriptableObject
    {
        [Serializable]
        public class DependencyRecord
        {
            public string GUID;
            public UnityEngine.Object Object;
        }

        public List<DependencyRecord> Dependency = new List<DependencyRecord>();

#if UNITY_EDITOR
        Dictionary<Type, int> _resourcesByType;
        public Dictionary<Type, int> GetStatistics()
        {
            if (_resourcesByType == null)
            {
                _resourcesByType = new Dictionary<Type, int>();
                foreach (var e in Dependency)
                {
                    if (e == null)
                    {
                        continue;
                    }
                    var type = e.Object.GetType();
                    _resourcesByType.TryGetValue(type, out var n);
                    _resourcesByType[type] = n + 1;
                }
            }
            return _resourcesByType;
        }
#endif
    }
}