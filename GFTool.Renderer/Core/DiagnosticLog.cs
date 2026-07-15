using System;
using System.IO;

namespace GFTool.Renderer.Core
{
    public static class DiagnosticLog
    {
        private static readonly object LogLock = new object();
        private static readonly string LogPath = System.IO.Path.Combine(AppContext.BaseDirectory, "testliog.txt");

        public static string FilePath => LogPath;

        public static void Reset(string reason)
        {
            try
            {
                lock (LogLock)
                {
                    File.WriteAllText(LogPath, string.Empty);
                    WriteUnlocked("=== Trinity Model Viewer diagnostic log ===");
                    WriteUnlocked($"Reset: {DateTime.Now:O}");
                    WriteUnlocked($"Reason: {reason}");
                    WriteUnlocked($"BaseDirectory: {AppContext.BaseDirectory}");
                    WriteUnlocked(string.Empty);
                }
            }
            catch
            {
                // Diagnostic logging must never stop the app from opening.
            }
        }

        public static void Write(string message)
        {
            try
            {
                lock (LogLock)
                {
                    WriteUnlocked(message);
                }
            }
            catch
            {
                // Diagnostic logging must never affect loading or rendering.
            }
        }

        public static void Section(string title)
        {
            Write(string.Empty);
            Write($"--- {title} ---");
        }

        public static void WriteException(string context, Exception ex)
        {
            Write($"{context}: {ex.GetType().Name}: {ex.Message}");
        }

        public static void WriteCapabilities()
        {
            Section("Project capabilities");
            Write("ModelViewer direct load:");
            Write("- .trmdl model root; references mesh/material/skeleton files next to the model data.");
            Write("- .trmsh mesh metadata; references .trmbf vertex/index buffers.");
            Write("- .trmtr legacy material flatbuffer and Gfx2 material flatbuffer path are both attempted.");
            Write("- .trskl skeleton; optional base skeleton merge is attempted for common character categories.");
            Write("- .bntx textures through NxMiddleware BnTxx decoder.");
            Write("- .tranm / .gfbanm animation flatbuffers through GfAnim parser.");
            Write("ModelViewer export:");
            Write("- Model export: glTF 2.0 JSON + .bin + decoded PNG textures + Blender material helper script.");
            Write("- Animation export: glTF 2.0 skeleton animation.");
            Write("- Texture export: decoded PNG or original BNTX copy.");
            Write("FileExplorer support visible in code:");
            Write("- .trpfd/.trpfs file descriptors and packed TRPAK/TRPFS data through ONEFILESerializer.");
            Write("- PackedArchive flatbuffers can be split/exported when Oodle is available.");
            Write("Important current limitation:");
            Write("- Renderer preview uses the first model UV set and repeats texture sampling for tiled UV ranges outside 0..1.");
            Write("- glTF exporter writes raw game UVs as TEXCOORD_0 and extra V-flipped helper UVs for Blender troubleshooting.");
            Write("- glTF standard materials cannot represent Trinity shader masks directly; exported material extras and the Blender helper keep those maps available.");
        }

        private static void WriteUnlocked(string message)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
    }
}
