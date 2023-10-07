using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    enum RowType
    {
        Header,
        Row,
        CollectionActions,
        Aggregate,
        GroupText,
        NewGroup
    }

    struct RowRender
    {
        public RowType Type;
        public Rect Rect;

        public Header[] Headers;
        public object Config;
        public IList Collection;
        public Type CollectionItemType;
        public bool PrimitiveCollection;
        public int CollectionIndex;
        public bool IsFiltered;

        public string Text;

        public override int GetHashCode()
        {
            switch (Type) {
                case RowType.Header:
                    return Headers.GetHashCode();
                case RowType.Row when !PrimitiveCollection:
                    return Config.GetHashCode();
                case RowType.Row when PrimitiveCollection:
                    return HashCode.Combine(Collection, CollectionIndex);
                case RowType.CollectionActions:
                    return Collection.GetHashCode();
                case RowType.GroupText:
                    return Text.GetHashCode();
            }

            Debug.LogWarning($"No hash code for {Type}");
            return base.GetHashCode();
        }
    }


    partial class PageRender
    {
        float _contentWidth;
        float _contentHeight;
        public Rect Content => new Rect(0, 0, _contentWidth, _contentHeight);

        private float _indent;
        private Stack<float> _indents = new Stack<float>();
        private List<RowRender> _rows = new List<RowRender>();

        public void Render(in PageContext context, object config, Header[] headers, string filter, Aggregator aggregator)
        {
            _contentWidth = 0;
            _contentHeight = 0;
            _indent = 0;
            _indents.Clear();
            _rows.Clear();
            switch (config)
            {
                case Index index:
                    InvalidateFilter(index, filter);
                    RenderCollection(in context, index, index.ConfigType, false, headers, filter, aggregator);
                    break;
            }
        }

        void RenderHeaders(in PageContext context, Header[] headers)
        {
            var width = 0f;
            foreach (var h in headers)
            {
                if (h.Separate)
                {
                    width += GUIConst.HeaderSeparator;
                }
                width += h.Width + GUIConst.HeaderSpace;
            }
            _rows.Add(new RowRender
            {
                Type = RowType.Header,
                Headers = headers,
                Rect = AppendRect(width, GUIConst.HeaderHeight)
            });
        }

        void RenderCollection(
            in PageContext context,
            IList collection,
            Type itemType,
            bool primitive,
            Header[] headers,
            string filter,
            Aggregator aggregator)
        {
            var isFiltered = !string.IsNullOrEmpty(filter);

            aggregator.Clear();
            var collectionIndexCounter = 0;
            foreach (var config in collection)
            {
                var collectionIndex = collectionIndexCounter++;
                var separate = false;
                if (!isFiltered)
                {
                    aggregator.Add(config, out separate);
                }
                if (separate)
                {
                    RenderGroupText(aggregator.Fetch(false), headers[0].Width);
                    _rows.Add(new RowRender
                    {
                        Type = RowType.NewGroup,
                        Rect = AppendRect(0, GUIConst.NewGroupHeight)
                    });
                }
                if (isFiltered && !FilterConfig(config, headers)) {
                    continue;
                }
                RenderRow(in context, headers, config, collection, itemType, primitive, collectionIndex, isFiltered);
            }
            if (!isFiltered)
            {
                RenderGroupText(aggregator.Fetch(true), headers[0].Width);


                BeginIndend(GUIConst.RowActionsColumnWidth);
                _rows.Add(new RowRender
                {
                    Type = RowType.CollectionActions,
                    Collection = collection,
                    CollectionItemType = itemType,
                    Rect = AppendRect(headers[0].Width, GUIConst.RowFieldHeight)
                });
                EndIndent();
            }
        }

        void RenderGroupText(string[] texts, float minWidth)
        {
            if (texts == null || texts.Length == 0)
            {
                return;
            }

            BeginIndend(GUIConst.RowActionsColumnWidth);
            foreach (var text in texts)
            {
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }
                var size = FDBEditorStyles.GroupTextLabel.CalcSize(new GUIContent(text));
                _rows.Add(new RowRender
                {
                    Type = RowType.GroupText,
                    Text = text,
                    Rect = AppendRect(Math.Max(size.x, minWidth), size.y)
                });
            }
            EndIndent();
        }

        Rect AppendRect(float width, float height)
        {
            var rect = new Rect(_indent, _contentHeight, width, height);

            _contentWidth = Math.Max(_contentWidth, rect.xMax);
            _contentHeight = Math.Max(_contentHeight, rect.yMax);

            return rect;
        }

        void BeginIndend(float left)
        {
            _indents.Push(left);
            _indent = _indents.Sum();
        }

        void EndIndent()
        {
            _indents.Pop();
            _indent = _indents.Sum();
        }

        bool RectFrom(Vector2 pos, out RowRender row)
        {
            foreach (var r in _rows)
            {
                if (r.Rect.Contains(pos))
                {
                    row = r;
                    return true;
                }
            }
            row = default;
            return false;
        }

        void RenderRow(
            in PageContext context,
            Header[] headers,
            object config,
            IList collection,
            Type collectionItemType,
            bool primitiveCollection,
            int collectionIndex,
            bool isFiltered)
        {
            var height = GUIConst.RowFieldHeight;
            foreach (var h in headers)
            {
                height = Math.Max(height, h.GetFieldHeight(in context, config));
            }

            _rows.Add(new RowRender
            {
                Type = RowType.Row,
                Rect = AppendRect(GUIConst.MeasureHeadersWidth(headers) + GUIConst.AfterRowSpace, height + GUIConst.RowPadding * 2),
                Headers = headers,
                Config = config,
                Collection = collection,
                CollectionItemType = collectionItemType,
                PrimitiveCollection = primitiveCollection,
                CollectionIndex = collectionIndex,
                IsFiltered = isFiltered,
            });

            if (!primitiveCollection && context.Inspector.TryGetExpandedHeader(config, headers, out var expandedHeader, out var expandedHeaderLeft))
            {
                BeginIndend(expandedHeaderLeft);
                switch (expandedHeader)
                {
                    case ListHeader listHeader:
                        var list = (IList)listHeader.Get(config, null);
                        RenderHeaders(in context, listHeader.Headers);
                        RenderCollection(in context, list, listHeader.ItemType, listHeader.Primitive, listHeader.Headers, null, listHeader.Aggregator);
                        break;
                }
                EndIndent();
            }
        }

        public void OnGUI(in PageContext context, Rect viewRect)
        {
            var rowIndex = 0;
            foreach (var row in _rows)
            {
                if (viewRect.Overlaps(row.Rect))
                {
                    var rowId = GUIUtility.GetControlID(row.GetHashCode(), FocusType.Passive, row.Rect);
                    switch (row.Type) {
                        case RowType.Header:
                            OnHeadersGUI(context.Inspector, row.Rect, row.Headers);
                            break;
                        case RowType.Row:
                            OnRowBackground(in context, row.Rect, row.Collection, row.CollectionIndex, rowIndex++);
                            OnRowGUI(in context, rowId, row.Rect, row.Config, row.Collection, row.CollectionItemType, row.PrimitiveCollection, row.CollectionIndex, row.Headers, row.IsFiltered);
                            break;
                        case RowType.CollectionActions:
                            OnCollectionActions(in context, row.Rect, row.Collection, row.CollectionItemType);
                            break;
                        case RowType.GroupText:
                            OnRowBackground(in context, row.Rect, row.Collection, row.CollectionIndex, rowIndex++);
                            OnGroupText(in context, row.Rect, row.Text);
                            break;
                        default:
                            Debug.LogWarning(row.Type.ToString());
                            break;
                    }
                }
            }
        }

        void OnRowBackground(in PageContext context, Rect rect, IList collection, int collectionIndex, int rowIndex)
        {
            var style = 
                context.Inspector.OnInput<InputDragRow>(out var drageRow) && drageRow.Match(collection, collectionIndex)
                    ? FDBEditorStyles.HoverRowStyle
                :rowIndex % 2 == 0
                    ? FDBEditorStyles.EvenRowStyle
                    : FDBEditorStyles.OddRowStyle;

            GUI.Box(rect, "", style);
        }

        void OnRowGUI(in PageContext context, int rowId, Rect rowRect, object config, IList collection, Type collectionItemType, bool primitiveCollection, int collectionIndex, Header[] headers, bool isFiltered)
        {
            var rect = rowRect;
            rect.y += GUIConst.RowPadding;
            rect.height -= GUIConst.RowPadding * 2;

            var left = rect.x;
            var top = rect.y;
            var height = rect.height;

            OnRowActionsGUI(in context,
                rowId,
                new Rect(left, top, GUIConst.RowActionsColumnWidth, height),
                config,
                collection,
                collectionItemType, 
                collectionIndex,
                isFiltered);

            left += GUIConst.RowActionsColumnWidth;

            foreach (var h in headers)
            {
                if (h.Separate)
                {
                    left += GUIConst.HeaderSeparator;
                }

                var fieldRect = new Rect(left, top, h.Width, height);

                if (primitiveCollection)
                {
                    h.OnGUI(in context, fieldRect, collection, collectionIndex);
                }
                else
                {
                    h.OnGUI(in context, fieldRect, config, null);
                }

                left += h.Width + GUIConst.HeaderSpace;
            }
        }

        void OnRowActionsGUI(in PageContext context, int rowId, Rect rect, object config, IList collection, Type collectionItemType, int collectionIndex, bool isFiltered)
        {
            var iconSize = GUIConst.RowFieldHeight;
            var iconRect = new Rect(rect.x, rect.y, iconSize, iconSize);
            GUI.Label(iconRect, FDBEditorIcons.RowAction);
            var labelRect = new Rect(rect.x + iconSize, rect.y, rect.width - iconSize - GUIConst.HeaderSpace, iconSize);
            if (context.Inspector.OnInput<InputDragRow>(out var dragRow) && dragRow.Match(collection, collectionIndex))
            {
                GUI.Label(labelRect, $"{collectionIndex}<", FDBEditorStyles.RightAlignLabel);
            } else
            {
                GUI.Label(labelRect, collectionIndex.ToString(), FDBEditorStyles.RightAlignLabel);
            }

            if (!isFiltered)
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
            }

            var e = Event.current;
            var t = e.GetTypeForControl(rowId);
            switch (t)
            {
                case EventType.MouseDown:
                    if (!isFiltered && rect.Contains(e.mousePosition))
                    {
                        if (e.button == 0)
                        {
                            GUIUtility.hotControl = rowId;
                            context.Inspector.SetInput(new InputDragRow(collection, collectionIndex));
                            e.Use();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (dragRow != null && dragRow.Match(collection, collectionIndex))
                    {
                        context.Inspector.ResetInput();
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (dragRow != null && dragRow.Match(collection, collectionIndex))
                    {
                        if (RectFrom(e.mousePosition, out var targetRow) 
                            && targetRow.Type == RowType.Row
                            && targetRow.Collection == collection
                            && Math.Abs(targetRow.CollectionIndex - collectionIndex) == 1)
                        {
                            collection[collectionIndex] = targetRow.Config;
                            collection[targetRow.CollectionIndex] = config;
                            context.Inspector.SetInput(new InputDragRow(collection, targetRow.CollectionIndex));
                            context.MakeDirty();
                        }
                        GUIUtility.hotControl = rowId;
                        e.Use();
                    }
                    break;
                case EventType.ContextClick:
                    if (rect.Contains(e.mousePosition))
                    {
                        ShowRowActionsMenu(context, rect, config, collection, collectionItemType, collectionIndex, isFiltered);
                        e.Use();
                    }
                    break;
            }
        }

        void ShowRowActionsMenu(PageContext context, Rect rect, object config, IList collection, Type collectionItemType, int collectionIndex, bool isFiltered)
        {
            var menu = new GenericMenu();

            void swap(int i0, int i1)
            {
                var o0 = collection[i0];
                var o1 = collection[i1];
                collection[i0] = o1;
                collection[i1] = o0;
            }

            if (!isFiltered)
            {
                menu.AddItem(new GUIContent("Move up"), false, () =>
                {
                    swap(collectionIndex, collectionIndex - 1);
                    context.MakeDirty();
                });
                menu.AddItem(new GUIContent("Move down"), false, () =>
                {
                    swap(collectionIndex, collectionIndex + 1);
                    context.MakeDirty();
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Insert above"), false, () =>
                {
                    collection.Insert(collectionIndex, DBResolver.Instantate(collectionItemType, true));
                    context.MakeDirty();
                });
                menu.AddItem(new GUIContent("Insert belove"), false, () =>
                {
                    collection.Insert(collectionIndex + 1, DBResolver.Instantate(collectionItemType, true));
                    context.MakeDirty();
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () => {
                collection.RemoveAt(collectionIndex);
                context.MakeDirty();
            });

            menu.DropDown(rect);
        }

        private void OnCollectionActions(in PageContext context, Rect rect, IList collection, Type itemType)
        {
            if (GUI.Button(rect, "+"))
            {
                collection.Add(DBResolver.Instantate(itemType, true));
                context.MakeDirty();
            }
        }

        private void OnGroupText(in PageContext context, Rect rect, string text)
        {
            GUI.Label(rect, text, FDBEditorStyles.GroupTextLabel);
            var e = Event.current;
            if (e.type == EventType.ContextClick && rect.Contains(e.mousePosition))
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Copy"), false, () =>
                {
                    GUIUtility.systemCopyBuffer = text;
                });

                menu.DropDown(rect);
                e.Use();
            }
        }

        public static void OnHeadersGUI(IInspector inspector, Rect rect, Header[] headers)
        {
            int headersId = GUIUtility.GetControlID(headers.GetHashCode(), FocusType.Passive);

            var left = rect.x + GUIConst.RowActionsColumnWidth;
            foreach (var header in headers)
            {
                if (header.Separate)
                {
                    left += GUIConst.HeaderSeparator;
                }

                var headerRect = new Rect(left, rect.y, header.Width, rect.height);
                GUI.Label(headerRect, header.Title, FDBEditorStyles.HeaderStyle);

                left += GUIConst.HeaderSpace + header.Width;

                var resizeRect = new Rect(left - GUIConst.HeaderSpace, rect.y, GUIConst.HeaderSpace * 3, rect.height);
                HandleHeaderResizeEvent(inspector, headersId, header, resizeRect);
            }
        }

        public static void HandleHeaderResizeEvent(IInspector inspector, int headersId, Header header, Rect resize)
        {
            EditorGUIUtility.AddCursorRect(resize, MouseCursor.ResizeHorizontal);

            var e = Event.current;
            if (inspector.InputState == null)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && resize.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = headersId;
                    inspector.SetInput(new InputResizeHeader(header, e.mousePosition));
                }
            } else if (inspector.OnInput<InputResizeHeader>(out var resizeHeader) && resizeHeader.Header == header)
            {
                var t = e.GetTypeForControl(headersId);
                switch (t)
                {
                    case EventType.MouseUp:
                        GUIUtility.hotControl = 0;
                        inspector.ResetInput();
                        e.Use();
                        break;
                    case EventType.MouseDrag:
                        GUIUtility.hotControl = headersId;
                        var newWidth = Math.Max(
                            GUIConst.HeaderMinWidth,
                            resizeHeader.StartWidth + e.mousePosition.x - resizeHeader.StartMouse.x);
                        header.Width = (int)newWidth;
                        inspector.Repaint();
                        e.Use();
                        break;
                }
            }
        }
    }
}
