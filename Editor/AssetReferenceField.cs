using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB.Editor
{
    public static class AssetReferenceField
    {
        static int? resetId;

        internal static AssetReference Field(
            AssetReference inputValue,
            Type assetType,
            params GUILayoutOption[] options)
        {
            var title = 
                inputValue != null && inputValue.editorAsset != null
                    ? inputValue.editorAsset.name
                : assetType == typeof(object)
                    ? $"({nameof(AssetReference)})"
                    : $"({assetType.Name})";
            var icon =
                inputValue == null || inputValue.editorAsset == null
                    ? FDBEditorIcons.DefaultAssetIcon
                    : AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(inputValue.editorAsset));

            var id = GUIUtility.GetControlID(FocusType.Passive);

            var originIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(Vector2.one * 14);
            GUILayout.Label(new GUIContent(title, icon), EditorStyles.objectFieldThumb, options);
            EditorGUIUtility.SetIconSize(originIconSize);

            var rect = GUILayoutUtility.GetLastRect();

            if (resetId == id)
            {
                resetId = null;
                GUI.changed = true;
                return null;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            var e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.DragUpdated:
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            if (DragAndDrop.objectReferences.Length != 1)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            } else
                            {
                                var obj = DragAndDrop.objectReferences[0];
                                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _);
                                var entry = settings.FindAssetEntry(guid);
                                if (obj == null || entry == null || !assetType.IsAssignableFrom(obj.GetType()))
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                                } else
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                                }
                            }
                            e.Use();
                        }
                    }
                    break;

                case EventType.DragPerform:
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            if (DragAndDrop.objectReferences.Length == 1)
                            {
                                var obj = DragAndDrop.objectReferences[0];
                                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _);
                                var entry = settings.FindAssetEntry(guid);
                                if (entry != null && assetType == typeof(object))
                                {
                                    GUI.changed = true;
                                    return new AssetReference(guid);
                                } else
                                {
                                    GUI.changed = true;
                                    var assetReferenceType = typeof(AssetReferenceT<>).MakeGenericType(assetType);
                                    return (AssetReference)Activator.CreateInstance(assetReferenceType, new object[] { guid });
                                }
                            }
                            e.Use();
                        }
                    }
                    break;

                case EventType.MouseDown:
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            EditorGUIUtility.PingObject(inputValue?.editorAsset);
                            e.Use();
                        }
                    }
                    break;

                case EventType.ContextClick:
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            var menu = new GenericMenu();

                            menu.AddItem(new GUIContent("Reset"), false, () =>
                            {
                                resetId = id;
                                GUI.changed = true;
                            });

                            menu.ShowAsContext();
                            GUI.changed = true;
                            e.Use();
                        }
                    }
                    break;
            }

            return inputValue;
        }
    }
}