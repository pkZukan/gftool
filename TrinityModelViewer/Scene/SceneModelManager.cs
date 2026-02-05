using GFTool.Renderer.Scene.GraphicsObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Trinity.Core.Assets;

namespace TrinityModelViewer.Scene
{
    internal sealed class SceneModelManager
    {
        private readonly Dictionary<Model, string> modelSourcePaths = new Dictionary<Model, string>();
        private readonly List<string> loadedModelPaths = new List<string>();
        private readonly HashSet<Model> hiddenModels = new HashSet<Model>();
        private readonly List<IAssetProvider> activeAssetProviders = new List<IAssetProvider>();

        public IReadOnlyDictionary<Model, string> ModelSourcePaths => modelSourcePaths;
        public IReadOnlyList<string> LoadedModelPaths => loadedModelPaths;

        public bool TryGetModelSourcePath(Model model, out string path) => modelSourcePaths.TryGetValue(model, out path!);

        public void SetModelSourcePath(Model model, string path)
        {
            if (model == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            modelSourcePaths[model] = path;
        }

        public void RemoveModelSourcePath(Model model)
        {
            if (model == null)
            {
                return;
            }

            modelSourcePaths.Remove(model);
        }

        public void AddLoadedModelPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            loadedModelPaths.Add(path);
        }

        public void ClearLoadedModelPaths() => loadedModelPaths.Clear();

        public bool IsHidden(Model model) => model != null && hiddenModels.Contains(model);

        public void SetHidden(Model model, bool hidden)
        {
            if (model == null)
            {
                return;
            }

            if (hidden)
            {
                hiddenModels.Add(model);
            }
            else
            {
                hiddenModels.Remove(model);
            }
        }

        public void ClearHidden() => hiddenModels.Clear();

        public void RegisterAssetProvider(IAssetProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            if (!activeAssetProviders.Contains(provider))
            {
                activeAssetProviders.Add(provider);
            }
        }

        public void DisposeAssetProviders()
        {
            foreach (var provider in activeAssetProviders.ToList())
            {
                try { provider.Dispose(); } catch { }
            }
            activeAssetProviders.Clear();
        }

        public void ClearSceneTracking()
        {
            modelSourcePaths.Clear();
            hiddenModels.Clear();
            loadedModelPaths.Clear();
        }
    }
}
