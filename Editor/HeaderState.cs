﻿using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FDB.Editor
{
    public abstract class HeaderState
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
    }

    public abstract class FieldHeaderState : HeaderState
    {
        public readonly FieldInfo Field;

        public FieldHeaderState(string path, FieldInfo field) : base(path, field?.GetCustomAttributes().ToArray(), field?.Name ?? "Item", null)
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

        public RefFieldHeaderState(string path, Type modelType) : base(path, null)
        {
            ModelType = modelType;
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

    public sealed class AssetReferenceFieldHeaderState : FieldHeaderState
    {
        public AssetReferenceFieldHeaderState(string path, FieldInfo field) : base(path, field) { }
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

        public EnumFieldHeaderState(string path, Type enumType) : base(path, null)
        {
            Values = enumType.GetEnumValues();
            Names = enumType.GetEnumNames();
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
    }
}
