using System;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public readonly struct PageContext
    {
        public readonly IInspector Inspector;
        public readonly object DB;
        public readonly DBResolver Resolver;
        public readonly Action Repaint;
        public readonly Action MakeDirty;
        public readonly int WindowLevel;
        public PageContext(
            IInspector inspector,
            object db,
            DBResolver resolver,
            Action repaint,
            Action makeDirty)
        {
            Inspector = inspector;
            DB = db;
            Resolver = resolver;
            Repaint = repaint;
            MakeDirty = makeDirty;
            WindowLevel = 0;
        }

        PageContext(
            IInspector inspector,
            object db,
            DBResolver resolver,
            Action repaint,
            Action makeDirty,
            int windowLevel)
        {
            Inspector = inspector;
            DB = db;
            Resolver = resolver;
            Repaint = repaint;
            MakeDirty = makeDirty;
            WindowLevel = windowLevel;
        }

        public PageContext Nested(Action repaint)
        {
            var baseRepaint = Repaint;
            return new PageContext(
                Inspector,
                DB,
                Resolver,
                () =>
                {
                    repaint();
                    baseRepaint();
                },
                MakeDirty,
                WindowLevel + 1);
        }
    }

    public partial class DBInspector<T>
    {
        void OnPageGUI(PageState state, PersistantPageState pagePers)
        {
            var pageId = GUIUtility.GetControlID(state.ModelType.GetHashCode(), FocusType.Passive);
            var pageRect = GUILayoutUtility.GetRect(new GUIContent(), "label", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            var headerWidth = GUIConst.MeasureHeadersWidth(state.Headers);
            var headerRect = new Rect(pageRect.x, pageRect.y, headerWidth, GUIConst.HeaderHeight);
            headerRect.x -= pagePers.Position.x;
            PageRender.OnHeadersGUI(this, headerRect, state.Headers);

            var db = EditorDB<T>.DB;
            var resolver = EditorDB<T>.Resolver;
            var index = (Index)state.ResolveModel(db);
            var context = new PageContext(this, db, resolver, this.Repaint, EditorDB<T>.SetDirty);

            Render.Render(in context, index, state.Headers, pagePers.Filter, state.Aggregator);

            var contentRect = Render.Content;
            if (GetFixedContentRect(out var fixedContentRect))
            {
                contentRect = fixedContentRect;
            }

            var scrollRect = new Rect(pageRect.x, headerRect.yMax, pageRect.width, pageRect.height - headerRect.height);
            using (var scrollView = new GUI.ScrollViewScope(scrollRect, pagePers.Position, contentRect))
            {
                var viewRect = new Rect(pagePers.Position, scrollRect.size);
                Render.OnGUI(in context, viewRect);
                pagePers.Position = scrollView.scrollPosition;
            }
        }
        private static PageRender Render = new PageRender();
    }
}