using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class KindHeader : FieldHeader
    {
        public readonly Type ConfigType;
        public readonly Type KindType;
        public KindHeader(string path, FieldInfo field) : base(path, field)
        {
            ConfigType = field.FieldType.GetGenericArguments()[0];
            KindType = typeof(Kind<>).MakeGenericType(ConfigType);
        }

        public Kind NewKind(string value)
        {
            return (Kind)Activator.CreateInstance(KindType, value);
        }

        public override bool Filter(object config, string filter)
        {
            var kind = (Kind)Get(config, null);
            if (kind.Value == null)
            {
                return false;
            }
            return kind.Value.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var index = context.Resolver.GetIndex(ConfigType);
            if (index == null)
            {
                GUI.Label(lineRect, "Index not exists");
                return;
            }

            var kind = (Kind)rawValue;
            EditorGUI.BeginChangeCheck();

            var canExport = kind.CanExport;
            if (index.IsDuplicateKind(kind.Value))
            {
                GUI.color = Color.red;
            }
            else if (!canExport)
            {
                GUI.color = Color.gray;
            }
            var newValue = GUI.TextField(lineRect, kind.Value);
            GUI.color = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                Set(config, collectionIndex, NewKind(newValue));
            }

            if (!canExport)
            {
                GUI.Box(
                    new Rect(
                        lineRect.xMax - lineRect.height,
                        lineRect.y,
                        lineRect.height,
                        lineRect.height),
                    FDBEditorIcons.NotExportIcon);
            }
        }
    }
}