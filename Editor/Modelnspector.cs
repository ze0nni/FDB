using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace FDB.Editor
{
    public partial class ModelInspector<T> : EditorWindow
    {
        public const int MenuSize = 25;
        
        bool _isDirty;

        InputState _input;

        int _pageIndex;
        Dictionary<object, int> _expandedItems = new Dictionary<object, int>();

        public void SetDirty()
        {
            _isDirty = true;
            GUI.changed = true;
            _state?.Resolver?.SetDirty();
        }

        void OnGUI()
        {
            InitStatic();
            Invalidate();

            if (_state.Model == null)
            {
                PushGuiColor(Color.red);

                GUILayout.Label("Model not loaded");

                PopGuiColor();
            } else
            {
                OnToolbarGui();

                if (_pageStates == null || _pageStates.Length == 0)
                {
                    GUILayout.Label("No indexes", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                } else
                {
                    if (_pageIndex < 0 || _pageIndex >= _pageStates.Length)
                    {
                        _pageIndex = 0;
                    }
                    var page = _pageStates[_pageIndex];

                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.SetNextControlName("SearchFilter");
                        page.Filter = EditorGUILayout.TextField(page.Filter ?? string.Empty, GUILayout.ExpandWidth(true));

                        var e = Event.current;
                        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && e.modifiers == EventModifiers.Control)
                        {
                            GUI.FocusControl("SearchFilter");
                            GUI.changed = true;
                        }
                    }

                    using (var scroll = new GUILayout.ScrollViewScope(page.Position,
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true)))
                    {
                        var model = page.ResolveModel(_state.Model);
                        var changed = OnTableGui(0, page.Headers, page.IndexType, model, page.Filter);
                        page.Position = scroll.scrollPosition;

                        if (changed)
                        {
                            SetDirty();
                        }
                    }
                    using (new GUILayout.VerticalScope())
                    {
                        var color = GUI.color;
                        GUI.color = Color.yellow;

                        foreach (var w in _state.Resolver.GetIndex(page.ModelType).Warnings)
                        {
                            GUILayout.Label(w);
                        }

                        GUI.color = color;
                    }

                    var newPageIndex = GUILayout.Toolbar(_pageIndex, _pageNames);
                    if (_pageIndex != newPageIndex)
                    {
                        _pageIndex = newPageIndex;
                        GUI.FocusControl(null);
                        GUI.changed = true;
                    }
                }
            }
            OnActionsGui();
        }

        void OnToolbarGui()
        {
            using (new GUILayout.HorizontalScope())
            {

                if (GuiButton("Save", _isDirty))
                {
                    Invoke("Save", () => SaveModel());
                }
                GuiButton("Undo", false);
                GuiButton("Redo", false);

                PushGuiColor(Color.red);
                if (GuiButton("Reset", true))
                {
                    Invoke("Load", () =>
                    {
                        LoadModel();
                        _isDirty = false;
                    });
                }
                PopGuiColor();
            }
        }

        bool OnTableGui(int left, HeaderState[] headers, Type type, object model, string filter)
        {
            var changed = false;
            OnHeadersGui(left, headers);
            changed |= OnItemsGui(left, headers, type, model, filter);
            OnPageActionsGui(left, headers, model);
            return changed;
        }
        
        void OnHeadersGui(int left, HeaderState[] headers)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(left + MenuSize);

                var e = Event.current;

                foreach (var header in headers)
                {
                    GUILayout.Box(header.Title, GUILayout.Width(header.Width));
                    var labelRect = GUILayoutUtility.GetLastRect();
                    var resizeRect = new Rect(labelRect.right - 5, labelRect.y, 10, labelRect.height);
                    EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

                    int id = GUIUtility.GetControlID(FocusType.Passive);

                    if (e.type == EventType.Repaint)
                        header.Left = (int)labelRect.left;

                    switch (_input.Type)
                    {
                        case InputState.Target.Free:
                            {
                                if (e.GetTypeForControl(id) == EventType.MouseDown && resizeRect.Contains(e.mousePosition))
                                {
                                    _input = new InputState
                                    {
                                        Type = InputState.Target.ResizeHeader,
                                        ResizePath = header.Path,
                                        ResizeStartWidth = header.Width,
                                        ResizeStartX = e.mousePosition.x,
                                    };
                                    e.Use();
                                }
                            }
                            break;
                        case InputState.Target.ResizeHeader:
                            {
                                if (_input.ResizePath == header.Path)
                                {
                                    switch (e.GetTypeForControl(id)) {
                                        case EventType.MouseDrag:
                                            {
                                                var delta = e.mousePosition.x - _input.ResizeStartX;
                                                header.Width = (int)Math.Max(20, _input.ResizeStartWidth + delta);
                                                GUI.changed = true;
                                                e.Use();
                                            }
                                            break;
                                        case EventType.MouseUp:
                                            {
                                                _input = default;
                                                e.Use();
                                            }
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }

        bool OnItemsGui(int left, HeaderState[] headers, Type itemType, object collection, string filter)
        {
            var changed = false;

            var itemIndex = 0;


            switch (collection)
            {
                case Index index:
                    foreach (var i in index.All())
                    {
                        if (!string.IsNullOrEmpty(filter) && !Inspector.ApplyFilter(i, filter))
                        {
                            continue;
                        }

                        changed |= OnModelGui(left, headers, i, index, itemIndex);
                        changed |= OnExpandedGui(left, headers, i);
                        itemIndex++;
                    }
                    if (changed)
                    {
                        index.SetDirty();
                    }
                    break;
                case IEnumerable list
                    when list.GetType().IsGenericType
                    && list.GetType().GetGenericTypeDefinition() == typeof(List<>):
                    {
                        if (itemType.IsEnum
                            || (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Ref<>))
                            || itemType == typeof(bool)
                            || itemType == typeof(int)
                            || itemType == typeof(float)
                            || itemType == typeof(string))
                        {
                            var indexParamas = new object[1];
                            var countProp = list.GetType().GetProperty("Count");
                            var itemProp = list.GetType().GetProperty("Item");

                            var count = (int)countProp.GetValue(list);
                            for (itemIndex = 0; itemIndex < count; itemIndex++)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(left);

                                    OnIndexMenuGUI(list, itemIndex);

                                    indexParamas[0] = itemIndex;
                                    var value = itemProp.GetValue(list, indexParamas);
                                    var newValue = Inspector.Field(_state.Resolver, headers[0], value);
                                    if (!newValue.Equals(value))
                                    {
                                        itemProp.SetValue(list, newValue, indexParamas);
                                        changed |= true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var i in list)
                            {
                                changed |= OnModelGui(left, headers, i, list, itemIndex);
                                itemIndex++;
                            }
                        }
                    }
                    break;
                default:
                    GUILayout.Label(collection.GetType().ToString());
                    break;
            }            
            return changed;
        }

        void ToggleExpandedState(object item, int index)
        {
            if (_expandedItems.TryGetValue(item, out var storedIndex) && storedIndex == index)
            {
                _expandedItems.Remove(item);
            }
            else
            {
                _expandedItems[item] = index;
            }

            GUI.changed = true;
        }

        bool OnExpandedGui(int left, HeaderState[] headers, object item)
        {
            var changed = false;
            if (_expandedItems.TryGetValue(item, out var expandIndex))
            {
                var header = headers[expandIndex];
                switch (header)
                {
                    case ListHeaderState listHeader:
                        {
                            OnHeadersGui(left + header.Left, listHeader.Headers);
                            var list = listHeader.Field.GetValue(item);
                            changed |= OnItemsGui(
                                left + header.Left,
                                listHeader.Headers,
                                listHeader.ItemType, list, null);
                            OnPageActionsGui(left + header.Left, listHeader.Headers, list);
                        }
                        break;

                    default:
                        {
                            GUILayout.Label($"Item {header.GetType().Name} can't be expand");
                        }
                        break;
                }
            }

            return changed;
        }

        bool OnModelGui(int left, HeaderState[] headers, object item, object collection, int collectionIndex)
        {
            var changed = false;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(left);

                OnIndexMenuGUI(collection, collectionIndex);
                for (var i = 0; i < headers.Length; i++)
                {
                    var h = headers[i];
                    changed |= OnFieldGui(h, i, item);
                }
            }

            return changed;
        }

        void OnIndexMenuGUI(object collection, int itemIndex)
        {
            GUILayout.Label("...", GUILayout.Width(MenuSize));
            var menuRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(menuRect, MouseCursor.SplitResizeUpDown);

            var e = Event.current;
            if (e.type != EventType.ContextClick || !menuRect.Contains(e.mousePosition))
            {
                return;
            }
            var menu = new GenericMenu();

            var collectionType = collection.GetType();

            switch (collection)
            {
                case Index index:
                    {
                        menu.AddItem(new GUIContent("Insert above"), false, () => { });
                        menu.AddItem(new GUIContent("Insert belove"), false, () => { });
                        menu.AddItem(new GUIContent("Remove"), false, () => { });
                    }
                    break;

                case IEnumerable _
                    when
                    collectionType.IsGenericType
                    && collectionType.GetGenericTypeDefinition() == typeof(List<>):
                    {

                    }
                    break;
            }            

            menu.ShowAsContext();

            GUI.changed = true;
        }

        bool OnFieldGui(HeaderState header, int headerIndex, object item)
        {
            var changed = false;

            switch (header) {
                case FieldHeaderState fieldHeader:
                    var value = fieldHeader.Field.GetValue(item);
                    var newValue = Inspector.Field(_state.Resolver, fieldHeader, value);
                    if (!newValue.Equals(value))
                    {
                        fieldHeader.Field.SetValue(item, newValue);
                        changed |= true;
                    }
                    break;

                case ListHeaderState listHeader:
                    if (GUILayout.Button("[...]", GUILayout.Width(header.Width)))
                    {
                        ToggleExpandedState(item, headerIndex);
                    }
                    break;                
            }
            return changed;
        }

        void OnPageActionsGui(int left, HeaderState[] headers, object collection)
        {
            if (headers.Length == 0)
            {
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(left + MenuSize);

                if (GUILayout.Button("+", GUILayout.Width(headers[0].Width)))
                {
                    var collectionType = collection.GetType();

                    Invoke("Add item", () =>
                    {
                        switch (collection)
                        {
                            case Index index:
                                {
                                    var modelType = collectionType.GetGenericArguments()[0];
                                    index.Add(_state.Resolver.Instantate(modelType));
                                }
                                break;


                            case IEnumerable _
                                when collectionType.IsGenericType
                                && collectionType.GetGenericTypeDefinition() == typeof(List<>):
                                {
                                    var modelType = collectionType.GetGenericArguments()[0];
                                    var model = _state.Resolver.Instantate(modelType);
                                    var add = collectionType.GetMethod("Add");
                                    add.Invoke(collection, new object[] { model });
                                    Debug.Log(collection);
                                }
                                break;
                        }
                        SetDirty();
                    });
                }
            }
        }
    }
}
