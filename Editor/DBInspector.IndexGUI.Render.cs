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
    }

    struct RowRender
    {
        public RowType Type;
        public Rect Rect;

        public HeaderState[] Headers;
        public object Config;
        public IList Collection;
        public Type CollectionItemType;

        public override int GetHashCode()
        {
            switch (Type) {
                case RowType.Header:
                    return Headers.GetHashCode();
                case RowType.Row:
                    return Config.GetHashCode();
                case RowType.CollectionActions:
                    return Collection.GetHashCode();
            }

            Debug.LogWarning($"No hash code for {Type}");
            return base.GetHashCode();
        }
    }


    class PageRender
    {
        float _contentWidth;
        float _contentHeight;
        public Rect Content => new Rect(0, 0, _contentWidth, _contentHeight);

        private float _indent;
        private Stack<float> _indents = new Stack<float>();
        private List<RowRender> _rows = new List<RowRender>();

        public void Render(in PageContext context, HeaderState[] headers, object entry)
        {
            _contentWidth = 0;
            _contentHeight = 0;
            _indent = 0;
            _indents.Clear();
            _rows.Clear();
            switch (entry)
            {
                case Index index:
                    RenderCollection(in context, index, index.ConfigType, headers);
                    break;
            }
        }

        void RenderHeaders(in PageContext context, HeaderState[] headers)
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

        void RenderCollection(in PageContext context, IList collection, Type itemType, HeaderState[] headers)
        {
            foreach (var config in collection)
            {
                AppendRow(in context, headers, config);
            }

            BeginIndend(GUIConst.ActionsColumnWidth);
            _rows.Add(new RowRender
            {
                Type = RowType.CollectionActions,
                Collection = collection,
                CollectionItemType = itemType,
                Rect = AppendRect(headers[0].Width, GUIConst.RowFieldHeight)
            });
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

        void AppendRow(in PageContext context, HeaderState[] headers, object config)
        {
            var height = GUIConst.RowFieldHeight;
            foreach (var h in headers)
            {
                height = Math.Max(height, h.GetFieldHeight(in context, config));
            }

            _rows.Add(new RowRender
            {
                Type = RowType.Row,
                Rect = AppendRect(GUIConst.MeasureHeadersWidth(headers), height),
                Headers = headers,
                Config = config
            });

            if (context.Inspector.TryGetExpandedHeader(config, headers, out var expandedHeader, out var expandedHeaderLeft))
            {
                BeginIndend(expandedHeaderLeft);
                switch (expandedHeader)
                {
                    case ListHeaderState listHeader:
                        var list = (IList)listHeader.Get(config, null);
                        if (!listHeader.Primitive)
                        {
                            RenderHeaders(in context, listHeader.Headers);
                            RenderCollection(in context, list, listHeader.ItemType, listHeader.Headers);
                        } else
                        {
                            
                        }
                        break;
                }
                EndIndent();
            }
        }

        public void OnGUI(in PageContext context, Rect viewRect)
        {
            foreach (var row in _rows)
            {
                if (viewRect.Overlaps(row.Rect))
                {
                    var rowId = GUIUtility.GetControlID(row.GetHashCode(), FocusType.Passive, row.Rect);
                    switch (row.Type) {
                        case RowType.Header:
                            OnHeadersGUI(context.Input, row.Rect, row.Headers);
                            break;
                        case RowType.Row:
                            OnRowGUI(in context, row.Rect, row.Config, row.Headers);
                            break;
                        case RowType.CollectionActions:
                            OnCollectionActions(in context, row.Rect, row.Collection, row.CollectionItemType);
                            break;
                        default:
                            Debug.LogWarning(row.Type.ToString());
                            break;
                    }
                }
            }
        }

        void OnRowGUI(in PageContext context, Rect rect, object config, HeaderState[] headers)
        {
            var left = rect.x;
            var top = rect.y;
            var height = rect.height;

            {
                //TODO actions
                left += GUIConst.ActionsColumnWidth;
            }

            foreach (var h in headers)
            {
                if (h.Separate)
                {
                    left += GUIConst.HeaderSeparator;
                }

                var fieldRect = new Rect(left, top, h.Width, height);

                h.OnGUI(in context, fieldRect, config, null);

                left += h.Width + GUIConst.HeaderSpace;
            }
        }

        public void OnCollectionActions(in PageContext context, Rect rect, IList collection, Type itemType)
        {
            if (GUI.Button(rect, "+"))
            {
                collection.Add(DBResolver.Instantate(itemType, true));
                context.MakeDirty();
            }
        }

        public static void OnHeadersGUI(IInput input, Rect rect, HeaderState[] headers)
        {
            int headersId = GUIUtility.GetControlID(headers.GetHashCode(), FocusType.Passive);

            var left = rect.x + GUIConst.ActionsColumnWidth;
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
                HandleHeaderResizeEvent(input, headersId, header, resizeRect);
            }
        }

        public static void HandleHeaderResizeEvent(IInput input, int headersId, HeaderState header, Rect resize)
        {
            EditorGUIUtility.AddCursorRect(resize, MouseCursor.ResizeHorizontal);

            var e = Event.current;
            if (input.State == null)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && resize.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = headersId;
                    input.Set(new InputResizeHeader(header, e.mousePosition));
                }
            } else if (input.Resolve<InputResizeHeader>(out var resizeHeader) && resizeHeader.Header == header)
            {
                var t = e.GetTypeForControl(headersId);
                switch (t)
                {
                    case EventType.MouseUp:
                        GUIUtility.hotControl = 0;
                        input.Reset();
                        e.Use();
                        break;
                    case EventType.MouseDrag:
                        GUIUtility.hotControl = headersId;
                        var newWidth = Math.Max(
                            GUIConst.HeaderMinWidth,
                            resizeHeader.StartWidth + e.mousePosition.x - resizeHeader.StartMouse.x);
                        header.Width = (int)newWidth;
                        input.Repaint();
                        e.Use();
                        break;
                }
            }
        }
    }
}
