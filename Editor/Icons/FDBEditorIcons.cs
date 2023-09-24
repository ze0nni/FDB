using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    internal static class FDBEditorIcons
    {
        public static Texture2D ConflictIcon { get; private set; }
        public static Texture2D ErrorIcon { get; private set; }
        public static Texture2D LinkIcon { get; private set; }
        public static Texture2D ViewIcon { get; private set; }
        public static Texture2D DefaultAssetIcon { get; private set; }
        public static Texture2D NotExportIcon { get; private set; }

        public static Texture2D RowOdd { get; private set; }
        public static Texture2D RowEven { get; private set; }
        public static Texture2D RowAction { get; private set; }

        public static Texture2D HeaderBackground { get; private set; }

        static FDBEditorIcons()
        {
            ConflictIcon = GetTexture("fdb-conflict-icon.png");
            ErrorIcon = GetTexture("fdb-error-icon.png");
            LinkIcon = GetTexture("fdb-link-icon.png");
            ViewIcon = GetTexture("fdb-view-icon.png");
            DefaultAssetIcon = GetTexture("fdb-default-asset-icon.png");
            NotExportIcon = GetTexture("fdb-not-export-icon.png");

            RowOdd = GetTexture("fdb-row-odd.png");
            RowEven = GetTexture("fdb-row-even.png");
            RowAction = GetTexture("fdb-row-action.png");

            HeaderBackground = GetTexture("fdb-header-backgroud.png");
        }

        static Texture2D GetTexture(string path) => EditorGUIUtility.FindTexture("Packages/com.pixelrebels.fdb/Editor/Icons/" + path);
    }
}