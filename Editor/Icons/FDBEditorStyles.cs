using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    internal static class FDBEditorStyles
    {
        public static GUIStyle OddRowStyle { get; private set; }
        public static GUIStyle EvenRowStyle { get; private set; }
        public static GUIStyle HoverRowStyle { get; private set; }
        public static GUIStyle HeaderStyle { get; private set; }
        public static GUIStyle WordWrapTextArea { get; private set; }
        public static GUIStyle RichTextLabel { get; private set; }
        public static GUIStyle RightAlignLabel { get; private set; }
        public static GUIStyle GroupTextLabel { get; private set; }

        static FDBEditorStyles()
        {
            var padding = new RectOffset(0, 0, 3, 3);
            OddRowStyle = new GUIStyle
            {
                padding = padding,
                normal = new GUIStyleState
                {
                    background = FDBEditorIcons.RowOdd,
                }
            };
            EvenRowStyle = new GUIStyle
            {
                padding = padding,
                normal = new GUIStyleState
                {
                    background = FDBEditorIcons.RowEven
                }
            };
            HoverRowStyle = new GUIStyle
            {
                padding = padding,
                normal = new GUIStyleState
                {
                    background = FDBEditorIcons.RowHover
                }
            };
            HeaderStyle = new GUIStyle
            {
                padding = new RectOffset(1, 1, 4, 4),
                margin = new RectOffset(4, 4, 0, 0),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                border = new RectOffset(4, 4, 4, 4),
                normal = new GUIStyleState
                {
                    textColor = Color.white,
                    background = FDBEditorIcons.HeaderBackground
                },
            };
            WordWrapTextArea = new GUIStyle(EditorStyles.textArea);
            WordWrapTextArea.wordWrap = true;

            RichTextLabel = new GUIStyle(EditorStyles.label);
            RichTextLabel.richText = true;

            RightAlignLabel = new GUIStyle("label");
            RightAlignLabel.alignment = TextAnchor.MiddleRight;

            GroupTextLabel = new GUIStyle("label");
            GroupTextLabel.padding = new RectOffset(2, 2, 2, 2);
        }
    }
}