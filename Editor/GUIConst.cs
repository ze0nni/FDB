using UnityEditor;

namespace FDB.Editor
{
    public static class GUIConst
    {
        public const float RowActionsColumnWidth = 64f;
        public const float AfterRowSpace = 24f;
        public const float HeaderHeight = 24f;
        public const float HeaderMinWidth = 30f;
        public const float HeaderSpace = 4f;
        public const float HeaderSeparator = 20f;
        public const float NewGroupHeight = 16f;

        public static float RowFieldHeight => EditorGUIUtility.singleLineHeight + 2;
        public const float RowPadding = 2f;
        public static float FieldViewButtonWidth => EditorGUIUtility.singleLineHeight * 2;

        public static float MeasureHeadersWidth(HeaderState[] headers)
        {
            var width = RowActionsColumnWidth;
            foreach (var h in headers)
            {
                width += h.Width;
                if (h.Separate)
                {
                    width += GUIConst.HeaderSeparator;
                }
            }
            if (headers.Length > 1)
            {
                width += (headers.Length - 1) * HeaderSpace;
            }
            return width;
        }
    }
}
