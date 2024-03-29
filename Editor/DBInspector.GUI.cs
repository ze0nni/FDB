﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{    
    public partial class DBInspector<T>
    {
        readonly Stack<bool> _guiEnabledStack = new Stack<bool>();
        readonly Stack<Color> _guiColorStack = new Stack<Color>();

        void PushGuiColor(Color newColor)
        {
            _guiColorStack.Push(GUI.color);
            GUI.color = newColor;
        }

        void PopGuiColor()
        {
            GUI.color = _guiColorStack.Pop();
        }

        void PushGuiEnabled(bool newValue)
        {
            _guiEnabledStack.Push(GUI.enabled);
            GUI.enabled = newValue;
        }

        void PopGuiEnabled()
        {
            GUI.enabled = _guiEnabledStack.Pop();
        }

        bool GuiButton(string text, bool enabled, GUIStyle style = null)
        {
            PushGuiEnabled(enabled);
            var result = GUILayout.Button(text, style ?? "button", GUILayout.ExpandWidth(false));
            PopGuiEnabled();
            return result;
        }
    }
}
