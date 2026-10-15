using System.Text.RegularExpressions;

namespace GFTool.Renderer.Core.Graphics;

public static class ShaderSource
{
    public static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, "Shaders");

    public static IReadOnlyList<string> ProgramNames => Directory.GetFiles(DirectoryPath, "*.glsl")
        .Select(Path.GetFileNameWithoutExtension).OfType<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();

    public static (string Vertex, string Fragment) GetProgram(string name)
    {

        string body = Expand(Path.Combine(DirectoryPath, name + ".glsl"));
        var version = Regex.Match(body, @"\A\s*#version[^\r\n]*(?:\r?\n|$)");
        if (!version.Success) throw new InvalidDataException($"Shader {name} must begin with #version.");
        string Stage(string stage) => body.Insert(version.Length, $"#define {stage}_STAGE\n");
        return (Stage("VERTEX"), Stage("FRAGMENT"));
    }

    public static string Expand(string path) => Expand(Path.GetFullPath(path), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static string Expand(string path, HashSet<string> active)
    {
        if (!active.Add(path)) throw new InvalidDataException($"Circular shader include: {path}");
        try
        {
            return Regex.Replace(File.ReadAllText(path), "(?m)^\\s*#include\\s+\"([^\"]+)\"\\s*$", match =>
                Expand(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, match.Groups[1].Value)), active));
        }
        finally { active.Remove(path); }
    }
}
