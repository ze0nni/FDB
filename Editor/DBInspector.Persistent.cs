using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FDB.Editor
{
    public partial class DBInspector<T> : ISerializationCallbackReceiver
    {
        [SerializeField] string _selectedPageName;
        int _pageIndex;

        [SerializeField] List<PersistantPageState> _persistantPageStates;

        public void OnAfterDeserialize()
        {
            var stateByName = _persistantPageStates.ToDictionary(x => x.Name, x => x);
            _persistantPageStates = _pageNames.Select(name =>
            {
                if (stateByName.TryGetValue(name, out var state))
                {
                    return state;
                }
                return new PersistantPageState
                {
                    Name = name
                };
            }).ToList();
        }

        public void OnBeforeSerialize()
        {
            
        }

        int PageIndex
        {
            get => _pageIndex;
            set {
                _pageIndex = Mathf.Clamp(value, 0, _pageNames.Length);
                _selectedPageName = _pageNames[_pageIndex];
            }
        }
    }

    [Serializable]
    class PersistantPageState
    {
        public string Name;
        public string Filter;
        public Vector2 Position;
    }
}
