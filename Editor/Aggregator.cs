using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FDB.Editor
{
    public class Aggregator
    {
        readonly List<(string Name, MethodInfo Func, Type InitialType)> _aggregators = new List<(string, MethodInfo, Type)>();
        readonly List<object> _history = new List<object>();

        public Aggregator(Type ownerType, FieldInfo field)
        {
            
            foreach (var attr in field.GetCustomAttributes<AggregateAttribute>())
            {
                var method = ownerType.GetMethod(attr.AggregateFuncName, BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null)
                {
                    Debug.LogWarning($"Aggregate method {attr.AggregateFuncName} not found in {ownerType}");
                }
                else
                {
                    _aggregators.Add((attr.Name, method, attr.InitialType));
                }
            }
        }

        public void Clear()
        {            
            _history.Clear();
        }

        public void Add(object model, out bool separate)
        {
            //if (_aggregators.Count > 0)
            {
                _history.Add(model);
            }
            separate = false;
        }

        public void OnGUI(float left)
        {
            if (_history.Count == 0)
            {
                return;
            }
            if (_aggregators.Count == 0)
            {
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(left);
                foreach (var a in _aggregators)
                {
                    GUILayout.Label($"{a.Name} = {Compute(_history, a.Func, a.InitialType)}");
                }
            }

            _history.Clear();
        }

        readonly object[] _pair = new object[2];
        private object Compute(List<object> history, MethodInfo func, Type initialType)
        {
            if (initialType == null)
            {
                if (_history.Count == 0)
                {
                    return "Nil";
                }
                var acc = history[0];
                for (var i = 1; i < history.Count; i++)
                {
                    _pair[0] = acc;
                    _pair[1] = _history[i];
                    acc = func.Invoke(null, _pair);
                }
                return acc;
            } else
            {
                var acc = Activator.CreateInstance(initialType);
                for (var i = 0; i < history.Count; i++)
                {
                    _pair[0] = acc;
                    _pair[1] = _history[i];
                    acc = func.Invoke(null, _pair);
                }
                return acc;
            }
        }
    }
}
