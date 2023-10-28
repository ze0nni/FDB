using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public class InspectorViewMenu<T> : PopupWindowContent
    {
        readonly DBInspector<T> _inspector;

        Vector2 _scrollPos;

        public InspectorViewMenu(DBInspector<T> inspector)
        {
            _inspector = inspector;
        }

        public override Vector2 GetWindowSize()
        {
            return base.GetWindowSize() * 1.5f;
        }

        public override void OnGUI(Rect rect)
        {
            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPos))
            {
                foreach (var page in _inspector._persistantPageStates)
                {
                    var isGenerated = EditorDB<T>.Resolver.IsGeneratedEntry(page.Name);
                    if (!_inspector._displayGenerated && isGenerated)
                    {
                        continue;
                    }

                    var visible = !page.Hidden;
                    var newVisible = GUILayout.Toggle(visible, !isGenerated ? page.Name : $"{page.Name} (Generated)");
                    if (visible != newVisible)
                    {
                        page.Hidden = !newVisible;
                        _inspector.UpdateVisiblePages();
                    }
                }
                _scrollPos = scrollView.scrollPosition;
            }

            GUILayout.Space(20);
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
            {
                if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    foreach (var s in _inspector._persistantPageStates)
                    {
                        s.Hidden = false;
                    }
                    _inspector.UpdateVisiblePages();
                }

                if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    foreach (var s in _inspector._persistantPageStates)
                    {
                        s.Hidden = true;
                    }
                    _inspector.UpdateVisiblePages();
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                _inspector._displayGenerated = GUILayout.Toggle(_inspector._displayGenerated, "Genereted");
                if (EditorGUI.EndChangeCheck())
                {
                    _inspector.UpdateVisiblePages();
                }
            }
        }
    }
}