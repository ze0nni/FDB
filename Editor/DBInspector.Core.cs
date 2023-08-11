using Newtonsoft.Json;
using FDB;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace FDB.Editor
{
    public partial class DBInspector<T>
    {
        public UndoModel Undo = new UndoModel();
        public readonly Queue<(string Name, Action Task)> Tasks = new Queue<(string, Action)>();

        void Invoke(string name, Action task)
        {
            Tasks.Enqueue((name, task));

            GUI.changed = true;
        }

        void OnActionsGui()
        {
            if (Tasks.TryDequeue(out var task))
            {
                try
                {
                    task.Task.Invoke();
                    GUI.changed = true;
                }
                catch (Exception exc)
                {
                    Debug.LogException(exc);
                }
            }
        }
    }
}
