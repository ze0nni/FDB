using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    internal static class EditorIcons
    {
        public static Texture2D ConflictIcon { get; private set; }
        public static Texture2D ErrorIcon { get; private set; }

        static EditorIcons()
        {
            ConflictIcon = GetTexture("fdb-conflict-icon.png");
            ErrorIcon = GetTexture("fdb-error-icon.png");
        }

        static Texture2D GetTexture(string path) => EditorGUIUtility.FindTexture("Packages/com.pixelrebels.fdb/Editor/Icons/" + path);
    }
}