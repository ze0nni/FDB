using System.Collections.Generic;
using UnityEditor;

namespace FDB.Editor
{
    public class FuryDBSettings : SettingsProvider
    {
        public const string Path = "Project/FuryDB";

        [SettingsProvider]
        static SettingsProvider Create()
        {
            return new FuryDBSettings();
        }


        public FuryDBSettings() : base(
            Path,
            SettingsScope.Project, 
            new string[] { "furudb", "database", "data","db" })
        {
        }

        public override void OnGUI(string searchContext)
        {
            
        }
    }

}