﻿using Newtonsoft.Json;
using FDB;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace FDB.Editor
{
    public partial class ModelInspector<T>
    {
        class State
        {
            public T Model;
            public DBResolver Resolver;
            public readonly Queue<(string Name, Action Task)> Tasks = new Queue<(string, Action)>();
            public readonly List<Tuple<string, bool, Exception>> TasksErrors = new List<Tuple<string, bool, Exception>>();
        }

        State _state;

        void Invalidate()
        {
            if (_state == null)
            {
                _state = new State();
                Invoke("Load model", () => LoadModel());                
            }

            if (_state.Model == null)
            {
                if (GUILayout.Button($"Load model from {MetaData().Path}"))
                {
                    Invoke("Load model", () => LoadModel());
                }
                if (GUILayout.Button($"New model at {MetaData().Path}"))
                {
                    Invoke("New model", () => InitNewModel());
                }
            }
        }        

        FuryDBAttribute MetaData()
        {
            foreach (var attr in Attribute.GetCustomAttributes(typeof(T)))
            {
				switch (attr)
                {
                    case FuryDBAttribute model:
                        return model;
                }
            }
            throw new ArgumentException($"Type {typeof(T).FullName} must have attribute {nameof(FuryDBAttribute)}");	
        }

        void InitNewModel()
        {
            var model = DBResolver.New<T>(out var resolver);
            _state.Model = model;
            _state.Resolver = resolver;
            SaveModel();
        }

		void LoadModel()
        {
            using (var reader = System.IO.File.OpenText(MetaData().Path))
            {
                var model = DBResolver.Load<T>(reader, out var resolver);
                _state.Model = model;
                _state.Resolver = resolver;
            }
        }

        void SaveModel()
        {
            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;

            using (var writer = System.IO.File.CreateText(MetaData().Path))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    serializer.Serialize(writer, _state.Model);
                }
            }
            _isDirty = false;
        }

        void Invoke(string name, Action task)
        {
            _state.Tasks.Enqueue((name, task));

            GUI.changed = true;
        }

        void OnActionsGui()
        {
            if (_state.Tasks.TryDequeue(out var task))
            {
                try
                {
                    task.Task.Invoke();
                    GUI.changed = true;
                } catch (Exception exc)
                {
                    _state.TasksErrors.Add(Tuple.Create(task.Name, false, exc));
                    Debug.LogException(exc);
                }
            }

            var itemToRemove = -1;
            for (var i = 0; i < _state.TasksErrors.Count; i++)
            {
                var error = _state.TasksErrors[i];

                using (new GUILayout.HorizontalScope())
                {

                    GUILayout.Label(error.Item1);
                    if (GUILayout.Button("Ok"))
                    {
                        itemToRemove = i;
                    }
                    GUILayout.Label(error.Item3.ToString(), GUILayout.ExpandWidth(true));
                }
            }
            if (itemToRemove != -1)
            {
                _state.TasksErrors.RemoveAt(itemToRemove);
                GUI.changed = true;
            }
        }
    }
}
