using UnityEngine;

namespace FDB.Editor
{
    internal static class FDBEditorStyles
    {
        public static GUIStyle OddRowStyle { get; private set; }
        public static GUIStyle EvenRowStyle { get; private set; }

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
        }
    }
}