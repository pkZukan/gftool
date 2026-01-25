using System;
using System.IO;
using GFTool.Renderer.Scene.GraphicsObjects;

namespace TrinityModelViewer.Export
{
    internal static class EditedMaterialMetadataExporter
    {
        public static void ExportEditedTrmmt(string sourceTrmmtPath, Model model, string outputTrmmtPath)
        {
            if (string.IsNullOrWhiteSpace(sourceTrmmtPath)) throw new ArgumentException("Missing source TRMMT path.", nameof(sourceTrmmtPath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(outputTrmmtPath)) throw new ArgumentException("Missing output TRMMT path.", nameof(outputTrmmtPath));
            if (!File.Exists(sourceTrmmtPath)) throw new FileNotFoundException("Source TRMMT not found.", sourceTrmmtPath);

            TrmmtBinaryPatcher.ExportEditedTrmmtPreserveAllFields(sourceTrmmtPath, model, outputTrmmtPath);
        }
    }
}
