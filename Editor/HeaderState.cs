using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FDB.Editor
{
    public abstract class HeaderState
    {
        public readonly string Path;
        public readonly string Title; 
        public readonly HeaderState[] Headers;

        public int Left;

        public HeaderState(string path, string title, HeaderState[] headers)
        {
            Path = path;
            Title = title;
            Headers = headers;
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
    }

    public abstract class FieldHeaderState : HeaderState
    {
        public readonly FieldInfo Field;

        public FieldHeaderState(string path, FieldInfo field) : base(path, field.Name, null)
        {
            Field = field;
        }
    }

    public sealed class KindFieldHeaderState : FieldHeaderState
    {
        public readonly Type ModelType;
        public KindFieldHeaderState(string path, FieldInfo field) : base(path, field) {
            ModelType = field.FieldType.GetGenericArguments()[0];
        }
    }

    public sealed class RefFieldHeaderState : FieldHeaderState
    {
        public readonly Type ModelType;
        public RefFieldHeaderState(string path, FieldInfo field) : base(path, field) {
            ModelType = field.FieldType.GetGenericArguments()[0];
        }
    }

    public sealed class BoolFieldHeaderState : FieldHeaderState
    {
        public BoolFieldHeaderState(string path, FieldInfo field) : base(path, field) { }
    }

    public sealed class IntFieldHeaderState : FieldHeaderState
    {
        public IntFieldHeaderState(string path, FieldInfo field) : base(path, field) { }
    }

    public sealed class FloatFieldHeaderState : FieldHeaderState
    {
        public FloatFieldHeaderState(string path, FieldInfo field) : base(path, field) { }
    }

    public sealed class StringFieldHeaderState : FieldHeaderState
    {
        public StringFieldHeaderState(string path, FieldInfo field) : base(path, field) { }
    }

    public sealed class EnumFieldHeaderState : FieldHeaderState
    {
        public readonly Array Values;
        public readonly string[] Names;

        public EnumFieldHeaderState(string path, FieldInfo field) : base(path, field)
        {
            Values = field.FieldType.GetEnumValues();
            Names = field.FieldType.GetEnumNames();
        }
    }

    public sealed class ListHeaderState : HeaderState
    {
        public readonly FieldInfo Field;
        public readonly Type ItemType;

        public ListHeaderState(string path, FieldInfo field, Type itemType)
            : base(path, field.Name, new[] { new ListItemHeaderState(path, itemType) })
        {
            Field = field;
            ItemType = itemType;
        }
    }

    public sealed class ListItemHeaderState : HeaderState
    {
        public readonly Type ItemType;

        public ListItemHeaderState(string path, Type itemType) : base(path, "List", null)
        {
            ItemType = itemType;
        }
    }
}
