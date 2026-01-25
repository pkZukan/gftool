using System;
using System.IO;

namespace TrinityModelViewer.Export
{
    internal static class EditedMaterialExporter
    {
        public static void ExportEditedTrmtr(string sourceTrmtrPath, GFTool.Renderer.Scene.GraphicsObjects.Model model, string outputTrmtrPath)
        {
            if (string.IsNullOrWhiteSpace(sourceTrmtrPath)) throw new ArgumentException("Missing source TRMTR path.", nameof(sourceTrmtrPath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(outputTrmtrPath)) throw new ArgumentException("Missing output TRMTR path.", nameof(outputTrmtrPath));
            if (!File.Exists(sourceTrmtrPath)) throw new FileNotFoundException("Source TRMTR not found.", sourceTrmtrPath);

            // Preserve all game-facing fields by patching the original FlatBuffer in-place.
            // This avoids lossy reserialization when we haven't modeled every field.
            TrmtrBinaryPatcher.ExportEditedTrmtrPreserveAllFields(sourceTrmtrPath, model, outputTrmtrPath);
        }
    }
}
