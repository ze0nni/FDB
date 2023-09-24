using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace FDB.Editor
{
    public partial class DBInspector<T> : EditorWindow
    {
        public const int MenuSize = 25;
        public const int GroupSpace = 25;

        InputState _input;

        long _dbVersion;
        bool _needRepaint;

        Action _makeDirty;
        public void MakeDirty()
        {
            _needRepaint = true;
            GUI.changed = true;
            EditorDB<T>.SetDirty();
        }

        void OnEnable()
        {
            _makeDirty = MakeDirty;
            InitStatic();
        }

        void OnDisable()
        {
            if (_autoSave && EditorDB<T>.IsDirty)
            {
                EditorDB<T>.Save();
            }
        }

        private void OnProjectChange()
        {
            if (_autoSave && EditorDB<T>.IsDirty)
            {
                EditorDB<T>.Save();
            }
        }

        private void Update()
        {
            var changed = false;
            if (_dbVersion != EditorDB<T>.Version)
            {
                _dbVersion = EditorDB<T>.Version;
                changed = true;
            }
            if (_needRepaint)
            {
                _needRepaint = false;
                changed = true;
            }

            if (changed)
            {
                Repaint();
            }
        }

        void OnGUI()
        {
            if (!OnValidateGUI())
            {
                return;
            }

            OnToolbarGui();

            if (_pageStates == null || _pageStates.Length == 0)
            {
                GUILayout.Label("No indexes", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            } else
            {
                var page = _pageStates[PageIndex];
                var pagePersist = _persistantPageStates[PageIndex];

                if (page.Errors.Count > 0)
                {
                    foreach (var w in page.Errors)
                    {
                        EditorGUILayout.HelpBox(w, MessageType.Error);
                    }
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    var pageId = GUIUtility.GetControlID(page.ModelType.GetHashCode(), FocusType.Passive);

                    using (var scroll = new GUILayout.ScrollViewScope(pagePersist.Position,
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true)))
                    {
                        var index = page.ResolveModel(EditorDB<T>.DB);
                        var changed = OnTableGui(0, page.Aggregator, page.Headers, page.IndexType, index, pagePersist.Filter);

                        if (page.IsPaintedOnce && _input.Type != InputState.Target.ResizeHeader)
                        {
                            pagePersist.Position = scroll.scrollPosition;
                        }

                        if (changed)
                        {
                            MakeDirty();
                        }
                    }
                    var hasWarnings =
                        EditorDB<T>.Resolver.Indexes.Any(i => i.Warnings.Count > 0)
                        || _pageStates.Any(s => s.Errors.Count > 0);
                    if (hasWarnings)
                    {
                        using (new GUILayout.VerticalScope(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight * 5)))
                        {
                            using (var scrollView = new GUILayout.ScrollViewScope(_warningsScrollPosition))
                            {
                                foreach (var s in _pageStates)
                                {
                                    foreach (var e in s.Errors)
                                    {
                                        EditorGUILayout.HelpBox(e, MessageType.Error);
                                    }
                                }

                                var color = GUI.color;
                                GUI.color = Color.yellow;
                                foreach (var index in EditorDB<T>.Resolver.Indexes)
                                {
                                    foreach (var w in index.Warnings)
                                    {
                                        GUILayout.Label($"[{index.ConfigType.Name}] {w}");
                                    }
                                }
                                GUI.color = color;
                                _warningsScrollPosition = scrollView.scrollPosition;
                            }
                        }
                    }
                }

                var pages = _pageStates
                    .Select(s =>
                    {
                        var index = EditorDB<T>.Resolver.GetIndex(s.ModelType);
                        return new GUIContent
                        {
                            text = s.Title,
                            image =
                                s.Errors.Count > 0 ? EditorIcons.ErrorIcon
                                : index.Warnings.Count > 0 ? EditorIcons.ConflictIcon
                                : null
                        };
                    }).ToArray();

                var newPageIndex = GUILayout.Toolbar(PageIndex, pages);
                if (PageIndex != newPageIndex)
                {
                    PageIndex = newPageIndex;
                    GUI.FocusControl(null);
                    GUI.changed = true;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    page.IsPaintedOnce = true;
                }
            }
            OnActionsGui();
        }

        void OnToolbarGui()
        {
            var e = Event.current;

            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GuiButton("Save", EditorDB<T>.IsDirty, EditorStyles.toolbarButton))
                {
                    Invoke("Save", () => EditorDB<T>.Save());
                }

                //if (GuiButton("Undo", Undo.CanUndo))
                //{
                //    Undo.Undo();
                //    SetDirty();
                //}
                //GuiButton("Redo", false);

                if (PageIndex == -1)
                {
                    GUILayout.FlexibleSpace();
                } else
                {
                    var pagePersist = _persistantPageStates[PageIndex];

                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.SetNextControlName("SearchFilter");
                        pagePersist.Filter = EditorGUILayout.TextField(
                            pagePersist.Filter ?? string.Empty,
                            EditorStyles.toolbarSearchField,
                            GUILayout.ExpandWidth(true));

                        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && e.modifiers == EventModifiers.Control)
                        {
                            GUI.FocusControl("SearchFilter");
                            GUI.changed = true;
                        }
                    }
                }

                _autoSave = GUILayout.Toggle(_autoSave, "Auto save", GUILayout.ExpandWidth(false));

                PushGuiColor(Color.red);
                if (GuiButton("Reload", true, EditorStyles.toolbarButton))
                {
                    Invoke("Reload", () =>
                    {
                        EditorDB<T>.Load();
                    });
                }
                PopGuiColor();
            }

            if (e.type == EventType.KeyDown
                && e.keyCode == KeyCode.Z
                && e.modifiers == EventModifiers.Control)
            {
                if (Undo.CanUndo)
                {
                    //_state.Undo.Undo();
                }
                e.Use();
            }
        }

        bool OnTableGui(int left, Aggregator aggregator, HeaderState[] headers, Type type, object model, string filter)
        {            
            var changed = false;
            OnHeadersGui(left, headers);
            changed |= OnItemsGui(left, aggregator, headers, type, model, filter);
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
                    if (header.Separate)
                    {
                        GUILayout.Space(GroupSpace);
                    }
                    GUILayout.Box(header.Title, GUILayout.Width(header.Width));
                    var labelRect = GUILayoutUtility.GetLastRect();
                    var resizeRect = new Rect(labelRect.xMax - 5, labelRect.y, 10, labelRect.height);
                    EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

                    int id = GUIUtility.GetControlID(FocusType.Passive);

                    if (e.type == EventType.Repaint)
                        header.Left = (int)labelRect.xMin;

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
                                if (e.type == EventType.ContextClick && labelRect.Contains(e.mousePosition))
                                {
                                    ShowHeaderContextMenu(header);
                                }
                            }
                            break;
                        case InputState.Target.ResizeHeader:
                            {
                                if (_input.ResizePath == header.Path)
                                {
                                    switch (e.GetTypeForControl(id)) {
                                        case EventType.MouseDrag:
                                        case EventType.DragUpdated:
                                            {
                                                var delta = e.mousePosition.x - _input.ResizeStartX;
                                                header.Width = (int)Math.Max(20, _input.ResizeStartWidth + delta);
                                                GUI.changed = true;
                                                e.Use();
                                            }
                                            break;
                                        case EventType.MouseUp:
                                        case EventType.MouseDown:
                                        case EventType.MouseLeaveWindow:
                                        case EventType.DragExited:
                                            {
                                                _input = default;
                                                GUI.changed = true;
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
        
        void OnAggregateGUI(int left, List<object> list, HeaderState[] headers)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(left);
                foreach (var header in headers)
                {
                    Debug.Log(header);
                }
            }
        }

        bool OnItemsGui(int left, Aggregator aggregator, HeaderState[] headers, Type itemType, object collection, string filter)
        {
            var changed = false;

            aggregator.Clear();

            var itemIndex = 0;

            switch (collection)
            {
                case Index index:
                    foreach (var i in index.All())
                    {
                        aggregator.Add(i, out var separate);
                        if (!string.IsNullOrEmpty(filter) && !Inspector.ApplyFilter(i, filter))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(filter) && separate)
                        {
                            aggregator.OnGUI(left + MenuSize);
                            GUILayout.Space(GroupSpace);
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
                                indexParamas[0] = itemIndex;
                                var value = itemProp.GetValue(list, indexParamas);

                                aggregator.Add(value, out var separate);

                                if (separate)
                                {
                                    aggregator.OnGUI(left + MenuSize);
                                    GUILayout.Space(GroupSpace);
                                }

                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(left);
                                    OnIndexMenuGUI(list, itemIndex);
                                    var newValue = Inspector.Field(EditorDB<T>.Resolver, headers[0], null, value, 0, _makeDirty);
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
                                aggregator.Add(i, out var separate);

                                if (separate)
                                {
                                    aggregator.OnGUI(left + MenuSize);
                                    GUILayout.Space(GroupSpace);
                                }

                                changed |= OnModelGui(left, headers, i, list, itemIndex);
                                changed |= OnExpandedGui(0, headers, i);
                                itemIndex++;
                            }
                        }
                    }
                    break;
                default:
                    GUILayout.Label(collection?.GetType().ToString() ?? "Null");
                    break;
            }

            aggregator.OnGUI(left + MenuSize);

            return changed;
        }

        void ToggleExpandedState(object item, HeaderState header)
        {
            if (!DBResolver.GetGUID(item, out var guid))
            {
                return;
            }

            if (_expandedFields.TryGetValue(guid, out var storedField) && storedField == header.Title)
            {
                _expandedFields.Remove(guid);
                _expandedOrder.Remove(guid);
            }
            else
            {
                _expandedFields[guid] = header.Title;
                _expandedOrder.Remove(guid);
                _expandedOrder.Add(guid);
            }

            while (_expandedOrder.Count > MaxExpandedHistory)
            {
                _expandedFields.Remove(_expandedOrder[0]);
                _expandedOrder.RemoveAt(0);
            }

            GUI.changed = true;
        }

        bool TryGetExpandedHeader(object item, HeaderState[] headers, out HeaderState header)
        {
            if (!DBResolver.GetGUID(item, out var guid)
                || !_expandedFields.TryGetValue(guid, out var field))
            {
                header = null;
                return false;
            }

            foreach (var h in headers)
            {
                if (h.Title == field)
                {
                    header = h;
                    return true;
                }
            }

            header = null;
            return false;
        }

        bool OnExpandedGui(int left, HeaderState[] headers, object item)
        {
            var changed = false;
            if (TryGetExpandedHeader(item, headers, out var header))
            {
                switch (header)
                {
                    case ListHeaderState listHeader:
                        {
                            OnHeadersGui(left + header.Left, listHeader.Headers);
                            var list = listHeader.Field.GetValue(item);
                            changed |= OnItemsGui(
                                left + header.Left,
                                listHeader.Aggregator,
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
                var id = GUIUtility.GetControlID(item.GetHashCode(), FocusType.Passive);
                GUILayout.Space(left);

                OnIndexMenuGUI(collection, collectionIndex);
                for (var i = 0; i < headers.Length; i++)
                {
                    var h = headers[i];
                    if (h.Separate)
                    {
                        GUILayout.Space(GroupSpace);
                    }
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
                        menu.AddItem(new GUIContent("Move up"), false, () => {
                            index.Swap(itemIndex, itemIndex - 1);
                            MakeDirty();
                        });
                        menu.AddItem(new GUIContent("Move down"), false, () => {
                            index.Swap(itemIndex, itemIndex + 1);
                            MakeDirty();
                        });
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Insert above"), false, () => {
                            var modelType = index.GetType().GetGenericArguments()[0];
                            index.Insert(itemIndex, DBResolver.Instantate(modelType, true));
                            MakeDirty();
                        });
                        menu.AddItem(new GUIContent("Insert belove"), false, () => {
                            var modelType = index.GetType().GetGenericArguments()[0];
                            index.Insert(itemIndex + 1, DBResolver.Instantate(modelType, true));
                            MakeDirty();
                        });
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete"), false, () => {
                            index.Remove(itemIndex);
                            MakeDirty();
                        });
                    }
                    break;

                case IEnumerable list
                    when
                    collectionType.IsGenericType
                    && collectionType.GetGenericTypeDefinition() == typeof(List<>):
                    {
                        menu.AddItem(new GUIContent("Move up"), false, () => {
                            ListActions.Swap(list, itemIndex, itemIndex - 1);
                            MakeDirty();
                        });
                        menu.AddItem(new GUIContent("Move down"), false, () => {
                            ListActions.Swap(list, itemIndex, itemIndex + 1);
                            MakeDirty();
                        });
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Insert above"), false, () => {
                            var modelType = list.GetType().GetGenericArguments()[0];
                            ListActions.Insert(list, itemIndex, DBResolver.Instantate(modelType, true));
                            MakeDirty();
                        });
                        menu.AddItem(new GUIContent("Insert belove"), false, () => {
                            var modelType = list.GetType().GetGenericArguments()[0];
                            ListActions.Insert(list, itemIndex + 1, DBResolver.Instantate(modelType, true));
                            MakeDirty();
                        });
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete"), false, () => {
                            ListActions.RemoveAt(list, itemIndex);
                        });
                    }
                    break;
            }            

            menu.ShowAsContext();

            GUI.changed = true;
        }

        bool OnFieldGui(HeaderState header, int headerIndex, object owner)
        {
            var changed = false;

            switch (header) {
                case FieldHeaderState fieldHeader:

                    EditorGUI.BeginChangeCheck();

                    var value = fieldHeader.Field.GetValue(owner);
                    var newValue = Inspector.Field(EditorDB<T>.Resolver, fieldHeader, owner, value, 0, _makeDirty);
                    var fieldId = GUIUtility.GetControlID(headerIndex, FocusType.Passive);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.Push(fieldId, owner);
                        fieldHeader.Field.SetValue(owner, newValue);
                        changed |= true;
                    }
                    break;

                case ListHeaderState listHeader:
                    if (GUILayout.Button($"[...]", GUILayout.Width(header.Width)))
                    {
                        ToggleExpandedState(owner, header);
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
                                    index.Add(DBResolver.Instantate(modelType, true));
                                }
                                break;


                            case IEnumerable _
                                when collectionType.IsGenericType
                                && collectionType.GetGenericTypeDefinition() == typeof(List<>):
                                {
                                    var modelType = collectionType.GetGenericArguments()[0];
                                    var model = DBResolver.Instantate(modelType, true);
                                    var add = collectionType.GetMethod("Add");
                                    add.Invoke(collection, new object[] { model });
                                }
                                break;
                        }
                        MakeDirty();
                    });
                }
            }
        }

        void ShowHeaderContextMenu(HeaderState header)
        {
            var menu = new GenericMenu();
            var persistanceState = _persistantPageStates[PageIndex];

            menu.AddItem(new GUIContent("Reset size"), false, () => {
                var delta = 150 - header.Width;
                header.Width = 150;
                persistanceState.Position.x += delta;
                GUI.changed = true;
            });
            menu.AddItem(new GUIContent("Expand"), false, () => {
                var delta = header.Width;
                header.Width *= 2;
                persistanceState.Position.x += delta;
                GUI.changed = true;
            });

            menu.ShowAsContext();
        }
    }
}
