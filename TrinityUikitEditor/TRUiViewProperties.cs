using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Core.Flatbuffers.TR.UI.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TrinityUikitEditor
{
    public class TRUiViewProperties
    {
        public static string GetProperties(string sceneComponent, object objData)
        {
            if (objData == null) return string.Empty;

            var sb = new StringBuilder();
            var type = objData.GetType();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(objData);

                if (value == null)
                {
                    sb.AppendLine($"{prop.Name}: null");
                }
                else if (value is IEnumerable enumerable && !(value is string))
                {
                    sb.AppendLine($"{prop.Name}:");
                    foreach (var item in enumerable)
                    {
                        sb.AppendLine($"\t{item}");
                    }
                }
                else if (IsComplexType(value))
                {
                    sb.AppendLine($"{prop.Name}: {FormatComplexValue(value)}");
                }
                else
                {
                    sb.AppendLine($"{prop.Name}: {value}");
                }
            }

            return sb.ToString();
        }

        private static bool IsComplexType(object value)
        {
            if (value == null) return false;

            var type = value.GetType();

            if (type.Namespace?.StartsWith("Generated.") == true)
                return false;

            return !type.IsPrimitive
                && !type.IsEnum
                && type != typeof(string)
                && type != typeof(decimal);
        }

        private static string FormatComplexValue(object value)
        {
            if (value == null) return "null";

            var type = value.GetType();

            if (type.Namespace?.StartsWith("Generated.") == true)
                return value.ToString();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToArray();

            if (properties.Length == 0)
                return value.ToString();

            if (properties.Length <= 4)
            {
                var parts = properties.Select(p => $"{p.Name}={p.GetValue(value)}");
                return $"{{{string.Join(", ", parts)}}}";
            }

            var sb = new StringBuilder();
            sb.AppendLine("{");
            foreach (var prop in properties)
            {
                sb.AppendLine($"\t\t{prop.Name}: {prop.GetValue(value)}");
            }
            sb.Append("\t}");
            return sb.ToString();
        }
    }
}
