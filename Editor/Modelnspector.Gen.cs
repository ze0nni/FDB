using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FDB.Editor
{
    public partial class ModelInspector<T> : EditorWindow
    {
        public static void GenerateCs(string path, T db)
        {
            var sb = new StringBuilder();

            var dbType = typeof(T);

            sb.AppendLine("using FDB;");
            sb.AppendLine($"namespace {dbType.Namespace}");
            sb.AppendLine("{");

            {
                sb.AppendLine($"\tpublic static class Kinds");
                sb.AppendLine("\t{");
                foreach (var field in dbType.GetFields())
                {
                    var index = field.GetValue(db) as Index;                    
                    if (index == null)
                    {
                        continue;
                    }
                    sb.AppendLine($"\t\tpublic static class {field.Name}");
                    sb.AppendLine("\t\t{");
                    var modelType = index.GetType().GetGenericArguments()[0];
                    var kindField = modelType.GetField("Kind");
                    foreach (var model in index.All())
                    {
                        var kind = (Kind)kindField.GetValue(model);
                        sb.AppendLine($"\t\t\tpublic static Kind<{modelType.Name}> {kind.Value} = new Kind<{modelType.Name}>(\"{kind.Value}\");");
                    }
                    sb.AppendLine("\t\t}");
                    sb.AppendLine($"\t\tpublic static Kind<{modelType.Name}>[] {field.Name}All = new Kind<{modelType.Name}>[]");
                    sb.AppendLine("\t\t{");
                    foreach (var model in index.All())
                    {
                        var kind = (Kind)kindField.GetValue(model);
                        sb.AppendLine($"\t\t\tnew Kind<{modelType.Name}>(\"{kind.Value}\"),");
                    }
                    sb.AppendLine("\t\t};");

                }
                sb.AppendLine("\t}");
            }

            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString());
        }
    }
}