using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    [CustomEditor(typeof(FuryDBAsset))]
    public class FuryDBAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = true;

            var asset = (FuryDBAsset)target;

            if (asset.Errors != null && asset.Errors.Count > 0)
            {
                foreach (var error in asset.Errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
                return;
            }
            using (new GUILayout.HorizontalScope())
            {
                var type = asset.DBType;
                GUILayout.Label($"DB Type: {(type == null ? "Null" : type.FullName)}");
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"Json size (bytes) ");
                GUILayout.TextField(asset.JsonData.Length.ToString());
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"Json version (md5)");
                GUILayout.TextField(asset.MD5);
            }

            GUILayout.Space(20);

            foreach (var entry in asset.Entries)
            {
                EditorGUILayout.HelpBox(entry.name, MessageType.None);
                var _resCount = entry.GetStatistics();
                foreach (var rType in _resCount)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(rType.Key.Name);
                        GUILayout.Label(rType.Value.ToString(), GUILayout.Width(200));
                    }
                }
            }
        }
    }

}