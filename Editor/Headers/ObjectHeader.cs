using System;
using System.Reflection;
using UnityEngine;

namespace FDB.Editor
{
    public class ObjectHeader : FieldHeader
    {
        readonly Header[] _headers;

        public ObjectHeader(Type headerType, string path, FieldInfo field, Header[] headers) : base(headerType, path, field)
        {
            _headers = headers;
        }

        public override bool Filter(object config, string filter)
        {
            return false;
        }

        public override bool GetExpandedObject(object config, out object obj, out Header[] headers)
        {
            obj = Get(config, null);
            headers = _headers;
            return obj != null;
        }

        public override void OnGUI(in PageContext context, Rect rect, Rect lineRect, object config, int? collectionIndex, object rawValue)
        {
            var e = GUI.enabled;
            GUI.enabled = true;
            if (GUI.Button(lineRect, "{...}"))
            {
                context.Inspector.ToggleExpandedState(config, this);
            }
            GUI.enabled = e;
        }
    }
}