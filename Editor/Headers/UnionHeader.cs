using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public class UnionHeader : FieldHeader
    {
        public static bool TryGetUnionType(Type fieldType, out Type baseUnionType, out Type unionTagType)
        {
            var type = fieldType;
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UnionBase<>))
                {
                    baseUnionType = type;
                    unionTagType = type.GetGenericArguments()[0];
                    return true;
                }
                type = type.BaseType;
            }
            baseUnionType = default;
            unionTagType = default;
            return false;
        }

        public readonly string[] _tagsNames;
        public readonly Dictionary<string, Header> _headers;

        public UnionHeader(
            string path,
            FieldInfo field,
            Type unionBaseType,
            Type unionTagType,
            Header[] headers) : base(field.FieldType, path, field)
        {
            _tagsNames = headers.Select(h => h.Title).ToArray();
            _headers = headers.ToDictionary(h => h.Title);
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var union = (UnionBase)rawValue;
            var tagPopupSize = EditorStyles.popup.CalcSize(new GUIContent(union.UnionTagString));
            var tagPopupRect = new Rect(lineRect.x, lineRect.y, tagPopupSize.x, lineRect.height);

            var tagIndex = Array.IndexOf(_tagsNames, union.UnionTagString);
            var newTagIndex = EditorGUI.Popup(tagPopupRect, tagIndex, _tagsNames);
            if (tagIndex != newTagIndex)
            {
                union.UnionTagString = _tagsNames[newTagIndex];
                GUI.changed = true;
            }

            if (union.UnionTagString != null && _headers.TryGetValue(union.UnionTagString, out var valueHeader))
            {
                var valueWidth = rect.width - tagPopupRect.width - GUIConst.FieldsSpace;

                var valueRect = rect;
                valueRect.x += tagPopupRect.width + GUIConst.FieldsSpace;
                valueRect.width = valueWidth;

                var valueLineRect = lineRect;
                valueLineRect.x += tagPopupRect.width + GUIConst.FieldsSpace;
                valueLineRect.width = valueWidth;

                var value = valueHeader.Get(union, null);
                if (value == null)
                {
                    value = DBResolver.Instantate(valueHeader.HeaderType, true);
                    valueHeader.Set(union, null, value);
                    GUI.changed = true;
                }

                valueHeader.OnGUI(in context, valueRect, valueLineRect, union, null, value);
            }
        }

        public override bool Filter(object config, string filter)
        {
            throw new NotImplementedException();
        }
    }
}
