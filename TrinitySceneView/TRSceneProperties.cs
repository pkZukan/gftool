using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using FlatSharp;
using GFTool.Renderer.Scene.GraphicsObjects;
using TrinitySceneView.Loading;
using OpenTK.Mathematics;

namespace TrinitySceneView;

public static class TRSceneProperties
{
    public static string GetProperties(SceneNode node, IEnumerable<Model>? renderedModels = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Name: {node.Name}");
        sb.AppendLine($"Type: {node.Type}");
        sb.AppendLine($"ID: 0x{node.Id:X}");
        sb.AppendLine($"Parent: {node.Parent?.Name ?? "(root)"}");
        sb.AppendLine($"Children: {node.Children.Count}");
        sb.AppendLine($"Payload: {node.PayloadSize:N0} bytes");
        sb.AppendLine($"Status: {node.Status}");
        if (node.Error != null) sb.AppendLine($"Error: {node.Error}");
        sb.AppendLine($"World position: {FormatVector(node.WorldTransform.Row3.Xyz)}");
        sb.AppendLine($"Local position: {FormatVector(node.LocalTransform.Row3.Xyz)}");
        sb.AppendLine($"World matrix:\n{FormatMatrix(node.WorldTransform)}");
        if (node.InstanceTransforms != null) sb.AppendLine($"Instances: {node.InstanceTransforms.Count:N0} (world transforms)");
        foreach (var note in node.Notes) sb.AppendLine($"Note: {note}");
        var models = renderedModels?.ToArray() ?? Array.Empty<Model>();
        if (models.Length > 0)
        {
            sb.AppendLine($"Rendered models in subtree: {models.Length}");
            foreach (var model in models.Take(20))
            {
                sb.AppendLine($"{model.Name}: {model.GetSubmeshNames().Count} submeshes, {model.GetMaterials().Count} materials, {model.Armature?.Bones.Count ?? 0} bones, {model.WorldTransforms.Count} placements");
                if (model.TryGetWorldBounds(out var min, out var max)) sb.AppendLine($"  Bounds: {FormatVector(min)} to {FormatVector(max)}");
            }
            if (models.Length > 20) sb.AppendLine($"... {models.Length - 20} more models");
        }
        return sb.ToString();
    }

    public static string GetAttributes(object? data)
    {
        if (data == null) return "No decoded payload. See Properties for the loading status.";
        var sb = new StringBuilder();
        Format(sb, data, "", 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
        if (sb.Length >= 60000) sb.AppendLine("... output limit reached");
        return sb.ToString();
    }

    private static void Format(StringBuilder sb, object? value, string label, int depth, HashSet<object> ancestors)
    {
        if (sb.Length > 60000) return;
        string indent = new(' ', depth * 2);
        if (value == null) { sb.AppendLine($"{indent}{label}null"); return; }
        var type = value.GetType();
        if (value is byte[] bytes)
        {
            sb.AppendLine($"{indent}{label}{bytes.Length:N0} bytes: {Convert.ToHexString(bytes.AsSpan(0, Math.Min(bytes.Length, 32)))}{(bytes.Length > 32 ? "..." : "")}");
            return;
        }
        if (type.IsPrimitive || type.IsEnum || value is string || value is decimal)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (text?.Length > 4096) text = text[..4096] + "... (text truncated)";
            sb.AppendLine($"{indent}{label}{text}");
            return;
        }
        if (depth >= 8) { sb.AppendLine($"{indent}{label}({type.Name}; depth limit)"); return; }
        if (!type.IsValueType && !ancestors.Add(value)) { sb.AppendLine($"{indent}{label}(recursive reference)"); return; }
        try
        {
            sb.AppendLine($"{indent}{label}{type.Name}");
            if (value is IFlatBufferUnion union)
            {
                sb.AppendLine($"{indent}  Discriminator: {union.Discriminator}");
                // Read only the active member: other Item properties throw.
                if (union.Discriminator != 0)
                    Format(sb, type.GetProperty($"Item{union.Discriminator}")?.GetValue(value), "Value: ", depth + 1, ancestors);
            }
            else if (value is IEnumerable items)
            {
                int index = 0;
                foreach (var item in items)
                {
                    if (index >= 64) { sb.AppendLine($"{indent}  ... remaining items omitted"); break; }
                    Format(sb, item, $"[{index++}]: ", depth + 1, ancestors);
                }
            }
            else
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
                {
                    try { Format(sb, property.GetValue(value), $"{property.Name}: ", depth + 1, ancestors); }
                    catch (Exception ex) { sb.AppendLine($"{indent}  {property.Name}: {ex.GetBaseException().Message}"); }
                }
            }
        }
        finally { if (!type.IsValueType) ancestors.Remove(value); }
    }

    private static string FormatVector(Vector3 vector) => $"({Number(vector.X)}, {Number(vector.Y)}, {Number(vector.Z)})";
    private static string Number(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    private static string FormatMatrix(Matrix4 matrix) => string.Join(Environment.NewLine, new[] { matrix.Row0, matrix.Row1, matrix.Row2, matrix.Row3 }
        .Select(r => $"  {Number(r.X)}, {Number(r.Y)}, {Number(r.Z)}, {Number(r.W)}"));
}
