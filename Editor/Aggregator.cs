using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public class Aggregator
    {
        readonly List<(string Name, MethodInfo Func, Type InitialType)> _aggregators = new List<(string, MethodInfo, Type)>();
        readonly List<object> _history = new List<object>();
        readonly List<object> _group = new List<object>();

        readonly FieldInfo groupByField;
        readonly Regex groupByRegex;
        readonly int groupByRegexGroup;

        public Aggregator(Type ownerType, FieldInfo field, Type itemType)
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
            foreach (var attr in field.GetCustomAttributes<GroupByAttribute>())
            {
                groupByField = itemType.GetField(attr.Field);
                if (groupByField == null)
                {
                    Debug.LogWarning($"Field {attr.Field} not found in {itemType}");
                }
                if (attr.Regex != null)
                {
                    groupByRegex = new Regex(attr.Regex);
                }
                groupByRegexGroup = attr.RegexGroup;
            }
        }

        public void Clear()
        {            
            _history.Clear();
        }

        public void Add(object model, out bool separate)
        {
            var prev = _history.LastOrDefault();
            if (prev == null || groupByField == null)
            {
                separate = false;
            }
            else
            {
                string group0 = Inspector.ToString(groupByField.GetValue(prev));
                string group1 = Inspector.ToString(groupByField.GetValue(model));
                if (groupByRegex != null)
                {
                    group0 = groupByRegex.Match(group0).Groups[groupByRegexGroup].Value;
                    group1 = groupByRegex.Match(group1).Groups[groupByRegexGroup].Value;
                }
                separate = group0 != group1;
            }

            if (separate)
            {
                _group.Clear();
                _group.AddRange(_history);
                _history.Clear();
            }

            _history.Add(model);
        }

        public void OnGUI(float left, bool end, Func<float, IDisposable> tableLineScope)
        {
            if (end)
            {
                _group.AddRange(_history);
                _history.Clear();
            }
            if (_group.Count == 0)
            {
                return;
            }
            if (_aggregators.Count == 0)
            {
                return;
            }

            using (tableLineScope.Invoke(left))
            {
                foreach (var a in _aggregators)
                {
                    string result;
                    if (a.InitialType == null)
                    {
                        result = $"{a.Name} = {Compute(_group, a.Func, a.InitialType)}";
                    }
                    else
                    {
                        result = $"{Compute(_group, a.Func, a.InitialType)}";
                    }
                    GUILayout.Label(result);
                    var rect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                    {
                        var menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Copy"), false, () =>
                        {
                            GUIUtility.systemCopyBuffer = result;
                        });

                        menu.ShowAsContext();
                    }
                }
            }

            _group.Clear();
        }

        static readonly object[] _pair = new object[2];
        private static object Compute(List<object> history, MethodInfo func, Type initialType)
        {
            if (initialType == null)
            {
                if (history.Count == 0)
                {
                    return "Nil";
                }
                var acc = history[0];
                for (var i = 1; i < history.Count; i++)
                {
                    _pair[0] = acc;
                    _pair[1] = history[i];
                    acc = func.Invoke(null, _pair);
                }
                return acc;
            } else
            {
                var acc = Activator.CreateInstance(initialType);
                for (var i = 0; i < history.Count; i++)
                {
                    _pair[0] = acc;
                    _pair[1] = history[i];
                    acc = func.Invoke(null, _pair);
                }
                return acc;
            }
        }
    }
}
