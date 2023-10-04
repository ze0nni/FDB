using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public partial class DBInspector<T>
    {
        private int _pageRowsCounter = 0;

        bool OnIndexGui_BAK(int left, Aggregator aggregator, HeaderState[] headers, Type type, object model, string filter)
        {
            _pageRowsCounter = 0;

            var changed = false;
            ////OnHeadersGui(left, headers);
            //changed |= OnItemsGui_BAK(left, aggregator, headers, type, model, filter);
            //if (string.IsNullOrEmpty(filter))
            //{
            //    OnPageActionsGui_BAK(left, headers, model);
            //}
            return changed;
        }

        void OnAggregateGUI_BAK(int left, List<object> list, HeaderState[] headers)
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

        //bool OnItemsGui_BAK(int left, Aggregator aggregator, HeaderState[] headers, Type itemType, object collection, string filter)
        //{
        //    var changed = false;

        //    var itemIndex = 0;

        //    switch (collection)
        //    {
        //        case Index index:
        //            aggregator.Clear();
        //            foreach (var i in index.All())
        //            {
        //                aggregator.Add(i, out var separate);
        //                if (!string.IsNullOrEmpty(filter) && !Inspector.ApplyFilter(i, filter))
        //                {
        //                    continue;
        //                }

        //                if (string.IsNullOrEmpty(filter) && separate)
        //                {
        //                    aggregator.OnGUI(left + MenuSize, false, TableLineScope_BAK);
        //                    GUILayout.Space(GroupSpace);
        //                }

        //                changed |= OnModelGui_BAK(left, headers, i, index, itemIndex);
        //                changed |= OnExpandedGui_BAK(left, headers, i);
        //                itemIndex++;
        //            }
        //            aggregator.OnGUI(left + MenuSize, true, TableLineScope_BAK);
        //            if (changed)
        //            {
        //                index.SetDirty();
        //            }
        //            break;
        //        case IEnumerable list
        //            when list.GetType().IsGenericType
        //            && list.GetType().GetGenericTypeDefinition() == typeof(List<>):
        //            {
        //                if (itemType.IsEnum
        //                    || (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Ref<>))
        //                    || itemType == typeof(bool)
        //                    || itemType == typeof(int)
        //                    || itemType == typeof(float)
        //                    || itemType == typeof(string)
        //                    || DBResolver.IsSupportedUnityType(itemType))
        //                {
        //                    var indexParamas = new object[1];
        //                    var countProp = list.GetType().GetProperty("Count");
        //                    var itemProp = list.GetType().GetProperty("Item");

        //                    var count = (int)countProp.GetValue(list);

        //                    aggregator.Clear();
        //                    for (itemIndex = 0; itemIndex < count; itemIndex++)
        //                    {
        //                        indexParamas[0] = itemIndex;
        //                        var value = itemProp.GetValue(list, indexParamas);

        //                        aggregator.Add(value, out var separate);

        //                        if (separate)
        //                        {
        //                            aggregator.OnGUI(left + MenuSize, false, TableLineScope_BAK);
        //                            GUILayout.Space(GroupSpace);
        //                        }

        //                        using (new TableRowGUILayout(this, left))
        //                        {
        //                            OnIndexMenuGUI_BAK(list, itemIndex);
        //                            EditorGUI.BeginChangeCheck();
        //                            var newValue = Inspector.Field(EditorDB<T>.Resolver, headers[0], null, value, 0, _makeDirty);
        //                            if (EditorGUI.EndChangeCheck())
        //                            {
        //                                itemProp.SetValue(list, newValue, indexParamas);
        //                                changed |= true;
        //                            }
        //                        }
        //                    }
        //                    aggregator.OnGUI(left + MenuSize, true, TableLineScope_BAK);
        //                }
        //                else
        //                {
        //                    foreach (var i in list)
        //                    {
        //                        aggregator.Add(i, out var separate);

        //                        if (separate)
        //                        {
        //                            aggregator.OnGUI(left + MenuSize, false, TableLineScope_BAK);
        //                            GUILayout.Space(GroupSpace);
        //                        }

        //                        changed |= OnModelGui_BAK(left, headers, i, list, itemIndex);
        //                        changed |= OnExpandedGui_BAK(0, headers, i);
        //                        itemIndex++;
        //                    }
        //                    aggregator.OnGUI(left + MenuSize, true, TableLineScope_BAK);
        //                }
        //            }
        //            break;
        //        default:
        //            GUILayout.Label(collection?.GetType().ToString() ?? "Null");
        //            break;
        //    }

        //    return changed;
        //}

        void ToggleExpandedState_BAK(object item, HeaderState header)
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

        bool TryGetExpandedHeader_BAK(object item, HeaderState[] headers, out HeaderState header)
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

        bool OnExpandedGui_BAK(int left, HeaderState[] headers, object item)
        {
            var changed = false;
            if (TryGetExpandedHeader_BAK(item, headers, out var header))
            {
                switch (header)
                {
                    case ListHeaderState listHeader:
                        {
                            //OnHeadersGui(left + header.Left, listHeader.Headers);
                            var list = listHeader.Field.GetValue(item);
                            //changed |= OnItemsGui_BAK(
                            //    left + header.Left,
                            //    listHeader.Aggregator,
                            //    listHeader.Headers,
                            //    listHeader.ItemType, list, null);
                            OnPageActionsGui_BAK(left + header.Left, listHeader.Headers, list);
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

        bool OnModelGui_BAK(int left, HeaderState[] headers, object item, object collection, int collectionIndex)
        {
            var changed = false;

            using (new TableRowGUILayout(this, left))
            {
                var id = GUIUtility.GetControlID(item.GetHashCode(), FocusType.Passive);

                OnIndexMenuGUI_BAK(collection, collectionIndex);
                for (var i = 0; i < headers.Length; i++)
                {
                    var h = headers[i];
                    if (h.Separate)
                    {
                        GUILayout.Space(GroupSpace);
                    }
                    changed |= OnFieldGui_BAK(h, i, item);
                }
            }

            return changed;
        }

        void OnIndexMenuGUI_BAK(object collection, int itemIndex)
        {
            var originIconsSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(Vector2.one * 16);
            GUILayout.Label(FDBEditorIcons.RowAction, GUILayout.Width(MenuSize - 5));
            EditorGUIUtility.SetIconSize(originIconsSize);

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

        bool OnFieldGui_BAK(HeaderState header, int headerIndex, object owner)
        {
            var changed = false;

            //switch (header)
            //{
            //    case FieldHeaderState fieldHeader:

            //        EditorGUI.BeginChangeCheck();

            //        var value = fieldHeader.Field.GetValue(owner);
            //        var newValue = Inspector.Field(EditorDB<T>.Resolver, fieldHeader, owner, value, 0, _makeDirty);
            //        var fieldId = GUIUtility.GetControlID(headerIndex, FocusType.Passive);

            //        if (EditorGUI.EndChangeCheck())
            //        {
            //            Undo.Push(fieldId, owner);
            //            fieldHeader.Field.SetValue(owner, newValue);
            //            changed |= true;
            //        }
            //        break;

            //    case ListHeaderState listHeader:
            //        {
            //            DBResolver.GetGUID(owner, out var guid);
            //            _expandedFields.TryGetValue(guid, out var fieldName);
            //            var isExpanded = fieldName == header.Title;

            //            var list = (IList)listHeader.Field.GetValue(owner);
            //            if (GUILayout.Button(
            //                isExpanded
            //                    ? " [...]"
            //                    : $"[ {list.Count} : {listHeader.ItemType.Name} ]",
            //                GUILayout.Width(header.Width)))
            //            {
            //                ToggleExpandedState_BAK(owner, header);
            //            }
            //        }
            //        break;
            //}
            return changed;
        }

        void OnPageActionsGui_BAK(int left, HeaderState[] headers, object collection)
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

        void ShowHeaderContextMenu_BAK(HeaderState header)
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

        private IDisposable TableLineScope_BAK(float left)
        {
            return new TableRowGUILayout(this, left);
        }

        private struct TableRowGUILayout : IDisposable
        {
            readonly DBInspector<T> _inspector;
            readonly int _rowIndex;

            readonly GUILayout.HorizontalScope _scope;
            readonly GUILayout.HorizontalScope _innerScope;

            public TableRowGUILayout(DBInspector<T> inspector, float left)
            {
                _inspector = inspector;
                _rowIndex = _inspector._pageRowsCounter++;
                var style =
                    _rowIndex % 2 == 0
                    ? FDBEditorStyles.EvenRowStyle
                    : FDBEditorStyles.OddRowStyle;

                _scope = new GUILayout.HorizontalScope();
                GUILayout.Space(left);
                _innerScope = new GUILayout.HorizontalScope(style);
            }

            public void Dispose()
            {
                _innerScope.Dispose();
                _scope.Dispose();
            }
        }
    }
}
