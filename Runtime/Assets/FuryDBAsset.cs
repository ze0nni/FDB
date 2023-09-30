using System;
using System.Collections.Generic;
using UnityEngine;

namespace FDB
{
    public class FuryDBAsset : ScriptableObject
    {
        public byte[] JsonData;
        public string MD5;

        public List<string> Errors;
        public List<FuryDBEntryAsset> Entries;

        private Dictionary<string, UnityEngine.Object> _dependencyCahce;
        public UnityEngine.Object ResolveDependency(string guid, Type type)
        {
            if (_dependencyCahce == null)
            {
                _dependencyCahce = new Dictionary<string, UnityEngine.Object>();
                foreach (var e in Entries)
                {
                    foreach (var r in e.Dependency)
                    {
                        _dependencyCahce[r.GUID] = r.Object;
                    }
                }
            }
            _dependencyCahce.TryGetValue(guid, out var result);
            return result;
        }
    }
}
