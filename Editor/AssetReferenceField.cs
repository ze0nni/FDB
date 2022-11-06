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
        static GUIStyle GuiTextField = new GUIStyle("textField");

        internal static AssetReference Field(AssetReference inputValue, params GUILayoutOption[] options)
        {
            var title = inputValue == null || inputValue.editorAsset == null ? $"({nameof(AssetReference)})" : inputValue.editorAsset.name;

            var id = GUIUtility.GetControlID(FocusType.Passive);
            GUILayout.Box(title, GuiTextField, options);
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
                                if (entry == null)
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
                                if (entry != null)
                                {
                                    GUI.changed = true;
                                    return new AssetReference(guid);
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