using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
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
        public bool ExpandWidth;

        public virtual float GetFieldHeight(in PageContext context, object config)
        {
            return GUIConst.RowFieldHeight;
        }

        public abstract object Get(object config, int? collectionIndex);
        public abstract void Set(object config, int? collectionIndex, object value);

        public void OnGUI(in PageContext context, Rect rect, object config, int? collectionIndex)
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

    public abstract class FieldHeaderState : HeaderState
    {
        public readonly FieldInfo Field;

        public FieldHeaderState(string path, FieldInfo field) : base(path, field?.GetCustomAttributes().ToArray(), field?.Name ?? "Item", null)
        {
            Field = field;
        }

        public override object Get(object config, int? collectionIndex)
        {
            return Field.GetValue(config);
        }

        public override void Set(object config, int? collectionIndex, object value)
        {
            Field.SetValue(config, value);
            GUI.changed = true;
        }
    }

    public sealed class AssetReferenceFieldHeaderState : FieldHeaderState
    {
        public readonly Type AssetType;
        public AssetReferenceFieldHeaderState(string path, FieldInfo field, Type assetType) : base(path, field) {
            AssetType = assetType;
        }
    }

    public sealed class ColorFieldHeaderState : FieldHeaderState
    {
        public ColorFieldHeaderState(string path, FieldInfo field) : base(path, field) { }
    }

    public sealed class AnimationCurveFieldHeaderState : FieldHeaderState
    {
        public AnimationCurveFieldHeaderState(string path, FieldInfo field) : base(path, field) { }
    }

    public sealed class UnityObjectFieldHeaderState : FieldHeaderState
    {
        public UnityObjectFieldHeaderState(string path, FieldInfo field) : base(path, field)
        {
        }
    }

    public sealed class ListHeaderState : HeaderState
    {
        public readonly FieldInfo Field;
        public readonly Type ItemType;
        public readonly bool Primitive;
        public readonly Aggregator Aggregator;

        public ListHeaderState(string path, Type ownerType, FieldInfo field, Type itemType, bool primitive, HeaderState[] headers)
            : base(path, null, field.Name, headers)
        {
            Field = field;
            ItemType = itemType;
            Primitive = primitive;
            Aggregator = new Aggregator(ownerType, field, itemType);
        }

        public override object Get(object config, int? collectionIndex)
        {
            throw new NotImplementedException();
        }

        public override void Set(object config, int? collectionIndex, object value)
        {
            throw new NotImplementedException();
        }
    }
}
