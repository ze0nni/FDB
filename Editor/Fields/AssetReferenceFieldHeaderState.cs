using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FDB.Editor
{
    public sealed class AssetReferenceFieldHeaderState : FieldHeaderState
    {
        public readonly Type AssetType;
        public AssetReferenceFieldHeaderState(string path, Type assetType, FieldInfo field) : base(path, field)
        {
            AssetType = assetType;
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            var r = (AssetReference)rawValue;

            var group = r == null || r.editorAsset == null
                ? null
                : settings.FindAssetEntry(r.AssetGUID)?.parentGroup;

            var title =
                r != null && r.editorAsset != null
                    ? $"{r.editorAsset.name}/{group?.name}"
                    : $"({AssetType.Name})";

            var icon =
                r == null || r.editorAsset == null
                    ? FDBEditorIcons.DefaultAssetIcon
                    : AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(r.editorAsset));

            var showViewButton =
                lineRect.width > GUIConst.FieldViewButtonWidth &&
                (r != null && r.editorAsset != null);

            var fieldRect = lineRect;
            if (showViewButton)
            {
                fieldRect.width -= GUIConst.FieldViewButtonWidth;
            }

            GUI.Label(
                fieldRect,
                new GUIContent(title, icon),
                EditorStyles.objectFieldThumb);

            if (showViewButton)
            {
                var viewRect = new Rect(fieldRect.xMax, fieldRect.y, GUIConst.FieldViewButtonWidth, fieldRect.height);
                if (GUI.Button(viewRect, new GUIContent("", FDBEditorIcons.ViewIcon)))
                {
                    EditorGUIUtility.PingObject(r.editorAsset);
                }
            }

            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    {
                        if (fieldRect.Contains(e.mousePosition))
                        {
                            if (e.button == 0)
                            {
                                var repaint = context.Repaint;
                                var makeDirty = context.MakeDirty;
                                PopupWindow.Show(
                                    fieldRect,
                                    new ChooseUnityObjectWindow(
                                        r?.editorAsset,
                                        AssetType,
                                        newObj => {
                                            Set(config, collectionIndex, GetReference(newObj));
                                            repaint();
                                            makeDirty();
                                        }));
                            }
                            else if (e.button == 1 && r != null && r.editorAsset != null)
                            {
                                var menu = new GenericMenu();

                                foreach (var g in settings.groups)
                                {
                                    if (g.ReadOnly)
                                    {
                                        continue;
                                    }

                                    menu.AddItem(new GUIContent(g.name), false, () =>
                                    {
                                        var entry = settings.FindAssetEntry(r.AssetGUID);
                                        if (entry == null)
                                        {
                                            settings.CreateAssetReference(r.AssetGUID);
                                            entry = settings.FindAssetEntry(r.AssetGUID);
                                        }
                                        settings.MoveEntry(entry, g);
                                    });
                                }

                                menu.DropDown(fieldRect);
                            }
                            e.Use();
                        }
                    }
                    break;

                case EventType.DragUpdated:
                    {
                        if (fieldRect.Contains(e.mousePosition))
                        {
                            if (GetDropResult(false, out _))
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
                    {
                        if (fieldRect.Contains(e.mousePosition))
                        {
                            if (GetDropResult(true, out var newValue))
                            {
                                Set(config, collectionIndex, newValue);
                            }
                            e.Use();
                        }
                    }
                    break;
            }
        }

        private AssetReference GetReference(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return null;
            }
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _)) {
                return null;
            }
            if (!AssetType.IsAssignableFrom(obj.GetType()))
            {
                return null;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings.FindAssetEntry(guid) == null)
            {
                settings.CreateAssetReference(guid);
            }
            if (AssetType == typeof(AssetReference))
            {
                return new AssetReference(guid);
            }
            else
            {
                var assetReferenceType = typeof(AssetReferenceT<>).MakeGenericType(AssetType);
                return (AssetReference)Activator.CreateInstance(assetReferenceType, guid);
            }
        }

        private bool GetDropResult(bool createRef, out AssetReference result)
        {
            if (DragAndDrop.objectReferences.Length != 1)
            {
                result = default;
                return false;
            }
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            var obj = DragAndDrop.objectReferences[0];
            if (obj == null)
            {
                result = default;
                return false;
            }
            if (!AssetType.IsAssignableFrom(obj.GetType()))
            {
                result = default;
                return false;
            }

            if (!createRef)
            {
                result = default;
                return true;
            }

            result = GetReference(obj);
            return true;
        }
    }
}