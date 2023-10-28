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

        [SerializeField] internal bool _displayGenerated;

        [SerializeField] internal List<PersistantPageState> _persistantPageStates = new List<PersistantPageState>();
        internal Dictionary<string, PersistantPageState> _persistantPageStatesMap = new Dictionary<string, PersistantPageState>();
        [SerializeField] List<PersistantExpendedField> _persistantExpendedFields = new List<PersistantExpendedField>();
        Dictionary<string, string> _expandedFields = new Dictionary<string, string>();
        List<string> _expandedOrder = new List<string>();
        const int MaxExpandedHistory = 128;

        void InitPersistanceData()
        {
            if (_allPageNames == null)
            {
                return;
            }
            _persistantPageStates = _persistantPageStates ?? new List<PersistantPageState>();
            _persistantExpendedFields = _persistantExpendedFields ?? new List<PersistantExpendedField>();

            var stateByName = _persistantPageStates.ToDictionary(x => x.Name, x => x);
            _persistantPageStates = _allPageNames.Select(name =>
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

            _persistantPageStatesMap = _persistantPageStates.ToDictionary(x => x.Name);

            _expandedOrder = _persistantExpendedFields
                .Select(x => x.GUID)
                .ToList();
            _expandedFields = _persistantExpendedFields
                .ToDictionary(x => x.GUID, x => x.Field);

            UpdateVisiblePages();
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

        string[] _visiblePagesNames;
        HashSet<string> _visiblePagesMap;

        internal void UpdateVisiblePages()
        {
            _visiblePagesNames = _persistantPageStates
                .Where(s => !s.Hidden)
                .Where(s => _displayGenerated || !EditorDB<T>.Resolver.IsGeneratedEntry(s.Name))
                .Select(s => s.Name)
                .ToArray();

            _visiblePagesMap = new HashSet<string>(_visiblePagesNames);

            _needRepaint = true;
        }

        int PageIndex
        {
            get {
                _pageIndex = _allPageNames.Length == 0 ? -1 : Mathf.Clamp(_pageIndex, 0, _allPageNames.Length - 1);
                if (!_visiblePagesMap.Contains(_allPageNames[_pageIndex]))
                {
                    var firstVisible = _visiblePagesNames.FirstOrDefault();
                    if (firstVisible == null)
                    {
                        _pageIndex = -1;
                    } else
                    {
                        _pageIndex = Array.IndexOf(_allPageNames, firstVisible);
                    }
                }
                return _pageIndex;
            }
            set {
                _pageIndex = Mathf.Clamp(value, 0, _allPageNames.Length - 1);
                _selectedPageName = _allPageNames[_pageIndex];
            }
        }

        int VisiblePageIndex
        {
            get
            {
                if (PageIndex == -1)
                {
                    return -1;
                }
                return Array.IndexOf(_visiblePagesNames, _allPageNames[PageIndex]);
            }
            set
            {
                if (value == -1)
                {
                    PageIndex = -1;
                }
                else
                {
                    PageIndex = Array.IndexOf(_allPageNames, _visiblePagesNames[value]);
                }
            }
        }
    }

    [Serializable]
    class PersistantPageState
    {
        public string Name;
        public string Filter;
        public Vector2 Position;
        public bool Hidden;
    }

    [Serializable]
    class PersistantExpendedField
    {
        public string GUID;
        public string Field;
    }
}
