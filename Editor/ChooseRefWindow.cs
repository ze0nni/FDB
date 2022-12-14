using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public class ChooseRefWindow : PopupWindowContent
    {
        static int _controlId;
        static bool _done = false;
        static Ref _currentField;
        static (float Time, Ref Field) _clickTime;
        static Rect _hoveredRect;
        
        public static Ref Field(
            DBResolver resolver,
            Type modelType,
            Ref currentField,            
            float width
        ) {
            var id = GUIUtility.GetControlID(FocusType.Passive);

            if (GUILayout.Button(currentField.Kind.Value, GUILayout.Width(width)))
            {
                _controlId = id;
                PopupWindow.Show(_hoveredRect, new ChooseRefWindow(resolver, modelType, currentField));
            }

            var fieldRect = GUILayoutUtility.GetLastRect();            
            if (Event.current.type == EventType.Repaint && fieldRect.Contains(Event.current.mousePosition))
            {
                _hoveredRect = fieldRect;
            }

            if (_done && _controlId == id)
            {
                _done = false;
                GUI.changed = true;
                return _currentField;
            }
            
            return currentField;
        }

        private ChooseRefWindow(DBResolver resolver, Type modelType, Ref currentField)
        {
            _resolver = resolver;
            _modelType = modelType;
            _kindField = modelType.GetField("Kind");
            _currentField = currentField;
            _filter = "";
            _scrollPos = Vector2.zero;            
        }

        DBResolver _resolver;
        Type _modelType;
        FieldInfo _kindField;
        string _filter;
        Vector2 _scrollPos;

        public override void OnGUI(Rect rect)
        {
            if (_resolver == null)
            {
                return;
            }

            GUI.SetNextControlName("SearchFilter");
            _filter = GUILayout.TextField(_filter, GUILayout.ExpandWidth(true));

            using (var scroll = new GUILayout.ScrollViewScope(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                foreach (var model in _resolver.GetIndex(_modelType).All())
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
                        if (_clickTime.Field != null && _clickTime.Field.Model == _currentField.Model && Time.realtimeSinceStartup - _clickTime.Time < 0.3f)
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