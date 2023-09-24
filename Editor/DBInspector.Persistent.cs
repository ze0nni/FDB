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
        [SerializeField] Vector2 _warningsScrollPosition;

        [SerializeField] bool _autoSave = true;

        [SerializeField] List<PersistantPageState> _persistantPageStates = new List<PersistantPageState>();
        [SerializeField] List<PersistantExpendedField> _persistantExpendedFields = new List<PersistantExpendedField>();
        Dictionary<string, string> _expandedFields = new Dictionary<string, string>();
        List<string> _expandedOrder = new List<string>();
        const int MaxExpandedHistory = 128;

        void InitPersistanceData()
        {
            if (_pageNames == null)
            {
                return;
            }
            _persistantPageStates = _persistantPageStates ?? new List<PersistantPageState>();
            _persistantExpendedFields = _persistantExpendedFields ?? new List<PersistantExpendedField>();

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

            _expandedOrder = _persistantExpendedFields
                .Select(x => x.GUID)
                .ToList();
            _expandedFields = _persistantExpendedFields
                .ToDictionary(x => x.GUID, x => x.Field);
        }

        public void OnAfterDeserialize()
        {
            InitPersistanceData();
        }

        public void OnBeforeSerialize()
        {
            _persistantExpendedFields = _expandedOrder
                .Select(guid =>
                {
                    _expandedFields.TryGetValue(guid, out var field);
                    return (GUID: guid, Field: field);
                })
                .Where(x => x.Field != null)
                .Select(x => new PersistantExpendedField
                {
                    GUID = x.GUID,
                    Field = x.Field
                })
                .ToList();
        }

        int PageIndex
        {
            get => _pageNames.Length == 0 ? -1 : _pageIndex;
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

    [Serializable]
    class PersistantExpendedField
    {
        public string GUID;
        public string Field;
    }
}
