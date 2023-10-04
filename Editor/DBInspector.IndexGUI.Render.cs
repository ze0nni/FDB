using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    enum RowType
    {
        Header,
        Row,
        Aggregate,
    }

    struct RowRender
    {
        public RowType Type;
        public Rect Rect;

        public HeaderState[] Headers;
        public object Config;
    }


    class PageRender
    {
        float _contentWidth;
        float _contentHeight;
        public Rect Content => new Rect(0, 0, _contentWidth, _contentHeight);

        private int _indent;
        private int _left;
        private List<RowRender> _rows = new List<RowRender>();

        public void Render(in PageContext context, HeaderState[] headers, object entry)
        {
            _contentWidth = 0;
            _contentHeight = 0;
            _rows.Clear();
            switch (entry)
            {
                case Index index:
                    RenderIndex(in context, index, headers);
                    break;
            }
        }

        void RenderIndex(in PageContext context, Index index, HeaderState[] headers)
        {
            foreach (var config in index.All())
            {
                AppendRow(in context, headers, config);
            }
        }

        Rect AppendRect(float width, float height)
        {
            var rect = new Rect(_left, _contentHeight, width, height);

            _contentWidth = Math.Max(_contentWidth, rect.xMax);
            _contentHeight = Math.Max(_contentHeight, rect.yMax);

            return rect;
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
        }

        public void OnGUI(in PageContext context, Rect viewRect)
        {
            foreach (var row in _rows)
            {
                if (viewRect.Overlaps(row.Rect))
                {
                    var rowId = GUIUtility.GetControlID(row.Config.GetHashCode(), FocusType.Passive, row.Rect);
                    OnRowRenderGUI(in context, in row);
                }
            }
        }

        void OnRowRenderGUI(in PageContext context, in RowRender row)
        {
            switch (row.Type)
            {
                case RowType.Row:
                    OnRowGUI(in context, in row);
                    break;
                case RowType.Header:
                    OnHeadersGUI(context.Input, row.Rect, row.Headers);
                    break;
                case RowType.Aggregate:
                    break;
                default:
                    Debug.LogWarning(row.Type.ToString());
                    break;
            }
        }

        void OnRowGUI(in PageContext context, in RowRender row)
        {
            var left = row.Rect.x;
            var top = row.Rect.y;
            var height = row.Rect.height;
            var config = row.Config;

            {
                //TODO actions
                left += GUIConst.ActionsColumnWidth;
            }

            foreach (var h in row.Headers)
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
