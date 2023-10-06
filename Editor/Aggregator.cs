using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        readonly List<string> _results = new List<string>();

        public Aggregator(Type ownerType, FieldInfo field, Type itemType)
        {
            
            foreach (var attr in field.GetCustomAttributes<AggregateAttribute>())
            {
                var method = itemType.GetMethod(attr.AggregateFuncName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (method == null)
                {
                    method = ownerType.GetMethod(attr.AggregateFuncName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                }
                if (method == null)
                {
                    Debug.LogWarning($"Aggregate method {attr.AggregateFuncName} not found in {ownerType} and {itemType}");
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

        public void Add(object config, out bool separate)
        {
            var prev = _history.LastOrDefault();
            if (prev == null || groupByField == null)
            {
                separate = false;
            }
            else
            {
                string group0 = Inspector.ToString(groupByField.GetValue(prev));
                string group1 = Inspector.ToString(groupByField.GetValue(config));
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

            _history.Add(config);
        }

        public string[] Fetch(bool end)
        {
            if (end)
            {
                _group.AddRange(_history);
                _history.Clear();
            }
            if (_group.Count == 0)
            {
                return null;
            }
            if (_aggregators.Count == 0)
            {
                return null;
            }

            _results.Clear();
            foreach (var a in _aggregators)
            {
                if (a.InitialType == null)
                {
                    _results.Add($"{a.Name} = {Compute(_group, a.Func, a.InitialType)}");
                }
                else
                {
                    _results.Add($"{Compute(_group, a.Func, a.InitialType)}");
                }
            }

            _group.Clear();

            return _results.ToArray();
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
