using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace FDB.Editor
{
    public partial class DBInspector<T> : EditorWindow
    {
        public const int MenuSize = 35;
        public const int GroupSpace = 25;

        long _dbVersion;
        bool _needRepaint;

        Action _makeDirty;
        public void MakeDirty()
        {
            _needRepaint = true;
            GUI.changed = true;
            EditorDB<T>.SetDirty();
        }

        void OnEnable()
        {
            _makeDirty = MakeDirty;
            InitStatic();
            InitPersistanceData();
        }

        void OnDisable()
        {
            if (_autoSave && EditorDB<T>.IsDirty)
            {
                EditorDB<T>.Save();
            }
        }

        private void OnProjectChange()
        {
            if (_autoSave && EditorDB<T>.IsDirty)
            {
                EditorDB<T>.Save();
            }
        }

        private void Update()
        {
            var changed = false;
            if (_dbVersion != EditorDB<T>.Version)
            {
                _dbVersion = EditorDB<T>.Version;
                changed = true;
            }
            if (_needRepaint)
            {
                _needRepaint = false;
                changed = true;
            }

            if (changed)
            {
                Repaint();
            }
        }

         void OnGUI()
        {
            if (!OnValidateGUI())
            {
                return;
            }

            OnToolbarGui();

            if (_pageStates == null || _pageStates.Length == 0)
            {
                EditorGUILayout.HelpBox("No indexes", MessageType.Info);
            } else
            {
                var page = _pageStates[PageIndex];
                var pagePersist = _persistantPageStates[PageIndex];

                if (page.Errors.Count > 0)
                {
                    foreach (var w in page.Errors)
                    {
                        EditorGUILayout.HelpBox(w, MessageType.Error);
                    }
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    OnPageGUI(page, pagePersist);
                    if (EditorGUI.EndChangeCheck())
                    {
                        MakeDirty();
                    }

                    var hasWarnings =
                        EditorDB<T>.Resolver.Indexes.Any(i => i.Warnings.Count > 0)
                        || _pageStates.Any(s => s.Errors.Count > 0);
                    if (hasWarnings)
                    {
                        using (new GUILayout.VerticalScope(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight * 5)))
                        {
                            using (var scrollView = new GUILayout.ScrollViewScope(_warningsScrollPosition))
                            {
                                foreach (var s in _pageStates)
                                {
                                    foreach (var e in s.Errors)
                                    {
                                        EditorGUILayout.HelpBox(e, MessageType.Error);
                                    }
                                }

                                var color = GUI.color;
                                GUI.color = Color.yellow;
                                foreach (var index in EditorDB<T>.Resolver.Indexes)
                                {
                                    foreach (var w in index.Warnings)
                                    {
                                        GUILayout.Label($"[{index.ConfigType.Name}] {w}");
                                    }
                                }
                                GUI.color = color;
                                _warningsScrollPosition = scrollView.scrollPosition;
                            }
                        }
                    }
                }

                var pages = _pageStates
                    .Select(s =>
                    {
                        var index = EditorDB<T>.Resolver.GetIndex(s.ModelType);
                        return new GUIContent
                        {
                            text = s.Title,
                            image =
                                s.Errors.Count > 0 ? FDBEditorIcons.ErrorIcon
                                : index.Warnings.Count > 0 ? FDBEditorIcons.ConflictIcon
                                : null
                        };
                    }).ToArray();

                var newPageIndex = GUILayout.Toolbar(PageIndex, pages);
                if (PageIndex != newPageIndex)
                {
                    PageIndex = newPageIndex;
                    ResetInput();
                    GUIUtility.hotControl = 0;
                    GUI.FocusControl(null);
                    GUI.changed = true;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    page.IsPaintedOnce = true;
                }
            }
            OnActionsGui();
        }

        void OnToolbarGui()
        {
            var e = Event.current;

            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GuiButton("Save", EditorDB<T>.IsDirty, EditorStyles.toolbarButton))
                {
                    Invoke("Save", () => EditorDB<T>.Save());
                }

                //if (GuiButton("Undo", Undo.CanUndo))
                //{
                //    Undo.Undo();
                //    SetDirty();
                //}
                //GuiButton("Redo", false);

                if (PageIndex == -1)
                {
                    GUILayout.FlexibleSpace();
                } else
                {
                    var pagePersist = _persistantPageStates[PageIndex];

                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.SetNextControlName("SearchFilter");
                        pagePersist.Filter = EditorGUILayout.TextField(
                            pagePersist.Filter ?? string.Empty,
                            EditorStyles.toolbarSearchField,
                            GUILayout.ExpandWidth(true));

                        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && e.modifiers == EventModifiers.Control)
                        {
                            GUI.FocusControl("SearchFilter");
                            GUI.changed = true;
                        }
                    }
                }

                _autoSave = GUILayout.Toggle(_autoSave, "Auto save", GUILayout.ExpandWidth(false));

                PushGuiColor(Color.red);
                if (GuiButton("Reload", true, EditorStyles.toolbarButton))
                {
                    Invoke("Reload", () =>
                    {
                        EditorDB<T>.Load();
                    });
                }
                PopGuiColor();
            }

            if (e.type == EventType.KeyDown
                && e.keyCode == KeyCode.Z
                && e.modifiers == EventModifiers.Control)
            {
                if (Undo.CanUndo)
                {
                    //_state.Undo.Undo();
                }
                e.Use();
            }
        }

        void OnPageGUI()
        {

        }
    }
}
