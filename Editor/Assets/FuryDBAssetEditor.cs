using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    [CustomEditor(typeof(FuryDBAsset))]
    public class FuryDBAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (FuryDBAsset)target;

            if (asset.Errors != null && asset.Errors.Count > 0)
            {
                foreach (var error in asset.Errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
                return;
            }
            
            foreach (var entry in asset.Entries)
            {
                EditorGUILayout.HelpBox(entry.name, MessageType.None);
                var _resCount = entry.GetStatistics();
                foreach (var rType in _resCount)
                {
                    GUILayout.Label(rType.ToString());
                }
            }
        }
    }

}