using System;
using System.Collections.Generic;
using UnityEngine;

namespace FDB
{
    public class FuryDBEntryAsset : ScriptableObject
    {
        public List<UnityEngine.Object> Dependency = new List<UnityEngine.Object>();

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
                    var type = e.GetType();
                    _resourcesByType.TryGetValue(type, out var n);
                    _resourcesByType[type] = n + 1;
                }
            }
            return _resourcesByType;
        }
#endif
    }
}