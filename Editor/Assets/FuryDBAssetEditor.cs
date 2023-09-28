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
        }
    }

}