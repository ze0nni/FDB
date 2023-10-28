using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FDB.Editor
{
    public partial class EditorDB<T>
    {
        public  static IEnumerable<(string, IFuryGenerator<T>)> ResolveGenerators()
        {
            var patches = new HashSet<string>();

            patches.Add(MetaData.CsGenPath);
            yield return (MetaData.CsGenPath, new DefaultGenerator());

            foreach (var ga in typeof(T)
                .GetCustomAttributes(typeof(FuryGeneratorAttribute), false)
                .Cast<FuryGeneratorAttribute>())
            {
                var generator = default(IFuryGenerator<T>);
                try
                {
                    if (!typeof(IFuryGenerator<T>).IsAssignableFrom(ga.GeneratorType))
                    {
                        throw new SchemaException($"Generator {ga.GeneratorType} must implements IFuryGenerator<{typeof(T)}>");
                    }
                    generator = (IFuryGenerator<T>)Activator.CreateInstance(ga.GeneratorType);
                } catch
                {
                    Debug.LogError($"Error when create generator {ga.GeneratorType} => {ga.CsPath}");
                    throw;
                }
                if (!patches.Add(ga.CsPath))
                {
                    throw new SchemaException($"Duplicates FuryGenerator's path {ga.CsPath}");
                }
                yield return (ga.CsPath, generator);
            }
        }

        private class DefaultGenerator : IFuryGenerator<T>
        {
            public void Execute(IndentStringBuilder sb, T db)
            {
                var dbType = typeof(T);

                sb.AppendLine("using FDB;");
                if (!string.IsNullOrEmpty(dbType.Namespace))
                {
                    sb.AppendLine($"namespace {dbType.Namespace}");
                    sb.AppendLine("{");
                    sb.BeginIndent();
                }

                {
                    sb.AppendLine($"public partial class {dbType.Name}");
                    sb.AppendLine("{");
                    sb.BeginIndent();
                    {
                        sb.AppendLine($"public static partial class Kinds ");
                        sb.AppendLine("{");
                        sb.BeginIndent();
                        foreach (var field in dbType.GetFields())
                        {
                            var index = field.GetValue(db) as Index;
                            if (index == null)
                            {
                                continue;
                            }
                            sb.AppendLine($"public static partial class {field.Name}");
                            sb.AppendLine("{");
                            sb.BeginIndent();
                            var modelType = index.GetType().GetGenericArguments()[0];
                            var kindField = modelType.GetField("Kind");
                            foreach (var config in index)
                            {
                                var kind = (Kind)kindField.GetValue(config);
                                if (!kind.CanExport)
                                {
                                    sb.AppendLine($"// Skip kind '{kind.Value}'");
                                }
                                else if (index.IsDuplicateKind(kind.Value))
                                {
                                    sb.AppendLine($"// Skip duplicate '{kind.Value}'");
                                    Debug.LogWarning($"Skip duplicate kind='{kind.Value}' of {modelType.Name}");
                                }
                                else
                                {
                                    sb.AppendLine($"public static Kind<{modelType.Name}> {kind.Value} = new Kind<{modelType.Name}>(\"{kind.Value}\");");
                                }
                            }
                            sb.EndIndent();
                            sb.AppendLine("}");

                        }
                        sb.EndIndent();
                        sb.AppendLine("}");
                    }
                    sb.EndIndent();
                    sb.AppendLine("}");
                }

                if (!string.IsNullOrEmpty(dbType.Namespace))
                {
                    sb.EndIndent();
                    sb.AppendLine("}");
                }
            }
        }
    }
}