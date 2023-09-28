using System.Collections.Generic;
using UnityEngine;

namespace FDB
{
    public class FuryDBAsset : ScriptableObject
    {
        public List<string> Errors;
        public List<FuryDBEntryAsset> Entries;
    }
}
