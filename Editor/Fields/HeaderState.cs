using System;
using UnityEngine;

namespace FDB.Editor
{
    public abstract partial class HeaderState
    {
        public readonly string Path;
        public readonly string Title;
        public readonly bool Separate;
        public readonly HeaderState[] Headers;

        public int Left;

        public HeaderState(string path, Attribute[] attr, string title, HeaderState[] headers)
        {
            Path = path;
            Title = title;
            Headers = headers;

            if (attr != null)
            {
                foreach (var a in attr)
                {
                    switch (a)
                    {
                        case SpaceAttribute _:
                            Separate = true;
                            break;
                    }
                }
            }
        }

        private int _width = -1;
        public int Width
        {
            get
            {
                if (_width == -1)
                {
                    _width = PlayerPrefs.GetInt(Path, 150);
                }
                return _width;
            }
            set
            {
                _width = value;
                PlayerPrefs.SetInt(Path, value);
            }
        }

        public virtual float GetFieldHeight(in PageContext context, object config)
        {
            return GUIConst.RowFieldHeight;
        }

        public abstract object Get(object config, int? collectionIndex);
        public abstract void Set(object config, int? collectionIndex, object value);

        public virtual void OnGUI(in PageContext context, Rect rect, object config, int? collectionIndex)
        {
            var lineRect = rect;
            lineRect.height = GUIConst.RowFieldHeight;
            OnGUI(in context, rect, lineRect, config, collectionIndex, Get(config, collectionIndex));
        }

        public virtual void OnGUI(
            in PageContext context,
            Rect rect,
            Rect lineRect,
            object config,
            int? collectionIndex,
            object rawValue)
        {
            GUI.Label(rect, GetType().Name);
        }
    }
}