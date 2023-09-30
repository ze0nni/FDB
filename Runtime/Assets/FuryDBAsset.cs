using System.Collections.Generic;
using UnityEngine;

namespace FDB
{
    public class FuryDBAsset : ScriptableObject
    {
        public string JsonData;
        public int JsonSize;
        public string MD5;

        public List<string> Errors;
        public List<FuryDBEntryAsset> Entries;
    }
}
