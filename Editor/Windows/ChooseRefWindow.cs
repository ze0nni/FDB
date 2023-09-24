using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    class NestLevel0 { }
    class NestLevel1 { }
    class NestLevel2 { }
    class NestLevel3 { }
    class NestLevel4 { }
    class NestLevel5 { }
    class NestLevel6 { }
    class NestLevel7 { }

    public class ChooseRefWindow<TNestLevel> : PopupWindowContent
    {
        static int _controlId;
        static bool _done = false;
        static Ref _currentField;
        static float _width;
        static (float Time, Ref Field) _clickTime;
        static Rect _hoveredRect;
        static Action _makeDirty;

        public static Ref Field(
            object owner,
            DBResolver resolver,
            Type modelType,
            AutoRefAttribute autoRef,
            Ref currentField,
            float width,
            GUILayoutOption layoutWidth,
            int nestLevel,
            Action makeDirty
        ) {
            _makeDirty = makeDirty;

            int id;
            var content = new GUIContent();
            var fieldRect = GUILayoutUtility.GetRect(content, "label", layoutWidth);
            
            id = GUIUtility.GetControlID(FocusType.Passive);

            if (Event.current.type == EventType.Repaint && fieldRect.Contains(Event.current.mousePosition))
            {
                _hoveredRect = fieldRect;
            }

            var refRect = fieldRect;
            var viewRect = new Rect();
            if (autoRef != null || currentField.Config != null)
            {
                var viewWidth = EditorGUIUtility.singleLineHeight * 3;
                refRect.width -= viewWidth;
                if (refRect.width > 0)
                {
                    viewRect = new Rect(refRect.xMax, refRect.y, viewWidth, refRect.height);
                } else
                {
                    refRect.width = width;
                }
            }

            var originIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(Vector2.one * 14);
            if (GUI.Button(refRect,new GUIContent(currentField.Kind.Value, FDBEditorIcons.LinkIcon), EditorStyles.objectField))
            {
                _controlId = id;
                PopupWindow.Show(_hoveredRect, new ChooseRefWindow<TNestLevel>(resolver, modelType, currentField, width));
            }
            EditorGUIUtility.SetIconSize(originIconSize);

            if ((autoRef != null || currentField.Config != null))
            {
                if (GUI.Button(viewRect, new GUIContent("View", FDBEditorIcons.ViewIcon)))
                {
                    _controlId = id;
                    PopupWindow.Show(_hoveredRect, new AutoRefWindow<TNestLevel>(
                        resolver,
                        owner,
                        modelType,
                        autoRef,
                        currentField,
                        width,
                        nestLevel,
                        makeDirty,
                        UpdateRef(_controlId)));
                }
            }
            

            if (_done && _controlId == id)
            {
                _done = false;
                GUI.changed = true;
                return _currentField;
            }

            return currentField;
        }

        private static Action<Ref> UpdateRef(int controlId)
        {
            return newRef =>
            {
                if (_controlId != controlId)
                {
                    return;
                }
                _currentField = newRef;
                _done = true;
                GUI.changed = true;
                _makeDirty?.Invoke();
            };
        }

        private ChooseRefWindow(DBResolver resolver, Type modelType, Ref currentField, float width)
        {
            _resolver = resolver;
            _modelType = modelType;
            _kindField = modelType.GetField("Kind");
            _currentField = currentField;
            _width = width;
            _filter = "";
            _scrollPos = Vector2.zero;
        }

        DBResolver _resolver;
        Type _modelType;
        FieldInfo _kindField;
        string _filter;
        Vector2 _scrollPos;

        public override Vector2 GetWindowSize()
        {
            var size = base.GetWindowSize();
            size.x = _width;
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            if (_resolver == null)
            {
                return;
            }

            var index = _resolver.GetIndex(_modelType);
            if (index == null)
            {
                EditorGUILayout.HelpBox($"{_modelType.Name} not in Index", MessageType.Error);
                return;
            }

            GUI.SetNextControlName("SearchFilter");
            _filter = GUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));

            using (var scroll = new GUILayout.ScrollViewScope(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                foreach (var model in index.All())
                {
                    if (!String.IsNullOrEmpty(_filter) && !Inspector.ApplyFilter(model, _filter))
                    {
                        continue;
                    }
                    var kind = (Kind)_kindField.GetValue(model);

                    var color = GUI.color;

                    if (_currentField.Kind.Equals(kind))
                    {
                        GUI.color = Color.blue;
                    }

                    GUILayout.Label(kind.Value);
                    var labelRect = GUILayoutUtility.GetLastRect();
                    EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

                    if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                    {
                        _currentField = (Ref)Activator.CreateInstance(_currentField.GetType(), _resolver, model);
                        GUI.changed = true;
                        if (_clickTime.Field != null && _clickTime.Field.Config == _currentField.Config && Time.realtimeSinceStartup - _clickTime.Time < 0.3f)
                        {
                            _done = true;
                            GUI.changed = true;
                            this.editorWindow.Close();
                        } else
                        {
                            _clickTime = (Time.realtimeSinceStartup, _currentField);
                        }
                    }
                    GUI.color = color;
                }

                _scrollPos = scroll.scrollPosition;
            }

            if (GUILayout.Button("Clear"))
            {
                _done = true;
                _currentField = null;
                GUI.changed = true;
                this.editorWindow.Close();
            }
            if (GUILayout.Button("Ok"))
            {
                _done = true;
                GUI.changed = true;
                this.editorWindow.Close();
            }
        }
    }
}