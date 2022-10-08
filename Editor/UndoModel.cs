using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FDB.Editor
{
    public class UndoModel
    {
        readonly Stack<(int Field, Action Action)> _undo = new Stack<(int, Action)>();

        public UndoModel()
        {
        }

        public bool CanUndo => _undo.Count > 0;

        public void Push(int field, object model)
        {
            if (_undo.Count > 0 && _undo.Peek().Field != 0 && _undo.Peek().Field == field)
            {
                return;
            }            
            var state = new Dictionary<FieldInfo, object>();
            foreach (var f in model.GetType().GetFields())
            {
                state[f] = f.GetValue(model);
            }

            _undo.Push((field, () =>
            {
                foreach (var f in state)
                {
                    f.Key.SetValue(model, f.Value);
                }
            }
            ));
        }

        public void Undo()
        {
            _undo.Pop().Action.Invoke();
        }

        public void Push(Action action)
        {
            _undo.Push((0, action));
        }
    }
}