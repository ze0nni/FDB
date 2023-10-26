using System;
using System.Collections;
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
            UnionValidator.Validate(field.FieldType, unionTagType);

            _tagsNames = headers.Select(h => h.Title).ToArray();
            _headers = headers.ToDictionary(h => h.Title);
        }

        public override bool GetExpandedList(object config, int? collectionIndex, out IList listOut, out ListHeader listHeaderOut)
        {
            var union = (UnionBase)Get(config, null);
            if (union.UnionTagString != null && _headers.TryGetValue(union.UnionTagString, out var header) && header is ListHeader listHeader)
            {
                listOut = (IList)listHeader.Get(union, null);
                listHeaderOut = listHeader;
                return true;
            }
            listOut = default;
            listHeaderOut = default;
            return false;
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var union = (UnionBase)rawValue;
            var tagPopupSize = EditorStyles.popup.CalcSize(new GUIContent(union.UnionTagString));
            var tagPopupRect = new Rect(lineRect.x, lineRect.y, Math.Min(lineRect.width, tagPopupSize.x), lineRect.height);

            var tagIndex = Array.IndexOf(_tagsNames, union.UnionTagString);
            var newTagIndex = EditorGUI.Popup(tagPopupRect, tagIndex, _tagsNames);
            if (tagIndex != newTagIndex)
            {
                union.UnionTagString = _tagsNames[newTagIndex];
                GUI.changed = true;
            }

            if (union.UnionTagString == null)
                return;
            if (!_headers.TryGetValue(union.UnionTagString, out var valueHeader))
                return;

            var valueWidth = Mathf.Max(0, rect.width - tagPopupRect.width - GUIConst.FieldsSpace);

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

            if (valueHeader is ListHeader)
            {
                if (GUI.Button(valueLineRect, "[...]"))
                {
                    context.Inspector.ToggleExpandedState(config, this);
                }
            }
            else
            {
                valueHeader.OnGUI(in context, valueRect, valueLineRect, union, null, value);
            }
        }

        public override bool Filter(object config, string filter)
        {
            var union = (UnionBase)Get(config, null);
            if (union.UnionTagString != null && _headers.TryGetValue(union.UnionTagString, out var header))
            {
                return header.Filter(union, filter);
            }
            return false;
        }
    }
}
