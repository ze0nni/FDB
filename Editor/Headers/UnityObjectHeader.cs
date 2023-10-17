using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public sealed class UnityObjectHeader : FieldHeader
    {
        public readonly Type AssetType;

        public UnityObjectHeader(string path, Type assetType, FieldInfo field) : base(assetType, path, field)
        {
            AssetType = assetType;
        }

        public override bool Filter(object config, string filter)
        {
            var uobj = (UnityEngine.Object)Get(config, null);
            var name = uobj?.name;
            if (name == null)
            {
                return false;
            }
            return name.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var uobj = (UnityEngine.Object)rawValue;

            var title =
                uobj != null
                    ? uobj.name
                : AssetType == typeof(object)
                    ? $"({nameof(Type)})"
                    : $"({AssetType.Name})";
            var icon =
                uobj == null
                    ? FDBEditorIcons.DefaultAssetIcon
                    : AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(uobj));

            var showViewButton =
                lineRect.width > GUIConst.FieldViewButtonWidth &&
                uobj != null;

            var fieldRect = lineRect;
            if (showViewButton)
            {
                fieldRect.width -= GUIConst.FieldViewButtonWidth;
            }

            if (GUI.Button(fieldRect, new GUIContent(title, icon), EditorStyles.objectFieldThumb))
            {
                var repaint = context.Repaint;
                var makeDirty = context.MakeDirty;
                PopupWindow.Show(fieldRect,
                    new ChooseUnityObjectWindow(
                        uobj,
                        AssetType,
                        newObj =>
                        {
                            Set(config, collectionIndex, newObj);
                            makeDirty();
                            repaint();
                        }));
            }

            if (showViewButton)
            {
                var viewRect = new Rect(fieldRect.xMax, fieldRect.y, GUIConst.FieldViewButtonWidth, fieldRect.height);
                if (GUI.Button(viewRect, new GUIContent("", FDBEditorIcons.ViewIcon)))
                {
                    EditorGUIUtility.PingObject(uobj);
                }
            }

            var e = Event.current;
            switch (e.type)
            {
                case EventType.DragUpdated:
                    {
                        if (fieldRect.Contains(e.mousePosition))
                        {
                            var dobj = DragAndDrop.objectReferences[0];
                            if (GetAssignableObject(dobj, AssetType, out _))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            }
                            else
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            }
                            e.Use();
                        }
                    }
                    break;
                case EventType.DragPerform:
                    if (fieldRect.Contains(e.mousePosition))
                    {
                        var dobj = DragAndDrop.objectReferences[0];
                        if (GetAssignableObject(dobj, AssetType, out var dropResult))
                        {
                            Set(config, collectionIndex, dropResult);
                        }
                        e.Use();
                    }
                    break;
            }
        }

        private static bool GetAssignableObject(UnityEngine.Object input, Type assetType, out UnityEngine.Object result)
        {
            var go = input as GameObject;
            if (go != null && go.scene != null && go.scene.name != null)
            {
                result = default;
                return false;
            }
            var component = input as Component;
            if (component != null && component.gameObject.scene != null && component.gameObject.scene.name != null)
            {
                result = default;
                return false;
            }
            if (input.GetType().IsAssignableFrom(assetType))
            {
                result = input;
                return true;
            }
            if (go != null && typeof(Component).IsAssignableFrom(assetType) && go.TryGetComponent(assetType, out component))
            {
                result = component;
                return true;
            }

            result = default;
            return false;
        }
    }
}