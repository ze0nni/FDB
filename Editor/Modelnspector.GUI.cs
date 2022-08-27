using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{    
    public partial class ModelInspector<T>
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

        bool GuiButton(string text, bool enabled)
        {
            PushGuiEnabled(enabled);
            var result = GUILayout.Button(text);
            PopGuiEnabled();
            return result;
        }        
    }
}
