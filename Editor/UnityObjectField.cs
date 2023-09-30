using System;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public class UnityObjectField
    {
        internal static UnityEngine.Object Field(UnityEngine.Object inputValue, Type assetType, GUILayoutOption layoutWidth, Action onChanged)
        {
            var title =
                inputValue != null
                    ? inputValue.name
                : assetType == typeof(object)
                    ? $"({nameof(Type)})"
                    : $"({assetType.Name})";
            var icon =
                inputValue == null
                    ? FDBEditorIcons.DefaultAssetIcon
                    : AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(inputValue));

            var id = GUIUtility.GetControlID(FocusType.Keyboard);

            var fieldRect = GUILayoutUtility.GetRect(new GUIContent(), "label", layoutWidth);

            var originIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(Vector2.one * 14);
            GUI.Label(fieldRect, new GUIContent(title, icon), EditorStyles.objectFieldThumb);
            EditorGUIUtility.SetIconSize(originIconSize);

            if (ChooseUnityObjectWindow.TrySelect(id, out var selected))
            {
                GUI.changed = true;
                return selected;
            }

            var e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (fieldRect.Contains(e.mousePosition))
                    {
                        if (e.button == 0)
                        {
                            PopupWindow.Show(
                                fieldRect,
                                new ChooseUnityObjectWindow(id, inputValue, assetType, onChanged));
                            e.Use();
                        } else
                        {
                            EditorGUIUtility.PingObject(inputValue);
                        }
                    }
                    break;
                case EventType.DragUpdated:
                    {
                        if (fieldRect.Contains(e.mousePosition))
                        {
                            var obj = DragAndDrop.objectReferences[0];
                            if (GetAssignableObject(obj, assetType, out _))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            } else
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
                        var obj = DragAndDrop.objectReferences[0];
                        if (GetAssignableObject(obj, assetType, out var dropResult))
                        {
                            GUI.changed = true;
                            return dropResult;
                        }
                        e.Use();
                    }
                    break;
            }

            return inputValue;
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
