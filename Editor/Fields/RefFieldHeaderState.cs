using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class RefFieldHeaderState : FieldHeaderState
    {
        public readonly Type ConfigType;
        public readonly Type RefType;
        public readonly AutoRefAttribute AutoRef;
        public RefFieldHeaderState(string path, FieldInfo field) : base(path, field)
        {
            ConfigType = field.FieldType.GetGenericArguments()[0];
            RefType = typeof(Ref<>).MakeGenericType(ConfigType);
            AutoRef = field.GetCustomAttribute<AutoRefAttribute>();
        }

        public RefFieldHeaderState(string path, Type modelType) : base(path, null)
        {
            ConfigType = modelType;
            RefType = typeof(Ref<>).MakeGenericType(ConfigType);
        }

        Ref NewRef(DBResolver resolver, object target)
        {
            return (Ref)Activator.CreateInstance(RefType, resolver, target);
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var r = (Ref)rawValue;

            var showViewButton =
                lineRect.width > GUIConst.FieldViewButtonWidth &&
                (AutoRef != null || r.Config != null);

            var fieldRect = lineRect;
            if (showViewButton)
            {
                fieldRect.width -= GUIConst.FieldViewButtonWidth;
            }

            var id = GUIUtility.GetControlID(config.GetHashCode(), FocusType.Passive);

            if (GUI.Button(
                fieldRect,
                new GUIContent(r.Kind.Value, FDBEditorIcons.LinkIcon),
                EditorStyles.objectFieldThumb))
            {
                var index = context.Resolver.GetIndex(ConfigType);
                var resolver = context.Resolver;
                var repaint = context.Repaint;
                var makeDirty = context.MakeDirty;
                PopupWindow.Show(
                    fieldRect,
                    new ChooseRefWindow(
                        r.Kind.Value,
                        index.GetKinds(),
                        str =>
                        {
                            object refConfig = null;
                            if (str != null)
                            {
                                index.TryGet(str, out refConfig);
                            }
                            Set(config, collectionIndex, NewRef(resolver, refConfig));
                            makeDirty();
                            repaint();
                        }));
            }

            if (showViewButton)
            {
                var viewRect = new Rect(fieldRect.xMax, fieldRect.y, GUIConst.FieldViewButtonWidth, fieldRect.height);
                if (GUI.Button(viewRect, new GUIContent("", FDBEditorIcons.ViewIcon)))
                {
                    var index = context.Resolver.GetIndex(ConfigType);
                    var resolver = context.Resolver;
                    var makeDirty = context.MakeDirty;
                    PopupWindow.Show(
                        fieldRect,
                        AutoRefWindow.New(
                            context,
                            config,
                            ConfigType,
                            AutoRef,
                            r,
                            str =>
                            {
                                object refConfig = null;
                                if (str != null)
                                {
                                    index.TryGet(str, out refConfig);
                                }
                                Set(config, collectionIndex, NewRef(resolver, refConfig));
                                makeDirty();
                            }
                            ));
                }
            }
        }
    }
}