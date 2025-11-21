using GFTool.Core.Flatbuffers.TR.Scene.Components;
using System.Text;

namespace TrinitySceneView
{
    public static class TRSceneProperties
    {
        private static string CameraEntity(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (trinity_CameraEntity)objData;
            sb.AppendLine("Name: " + data.Name);
            sb.AppendLine("Target: " + data.TargetName);

            return sb.ToString();
        }

        private static string SceneObject(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (trinity_SceneObject)objData;
            sb.AppendLine("Name: " + data.Name);
            if (data.AttachJointName != string.Empty)
                sb.AppendLine("Attach joint name: " + data.AttachJointName);
            sb.AppendLine(string.Format("Tags: ({0})", data.TagList.Length));

            foreach (var tag in data.TagList)
            {
                sb.AppendLine(string.Format("  {0}" + Environment.NewLine, tag == string.Empty ? "(Blank)" : tag));
            }

            if (data.Layers.Length > 0)
            {
                sb.AppendLine(string.Format("Layers: ({0})", data.Layers.Length));
                foreach (var layer in data.Layers)
                {
                    sb.AppendLine(string.Format("  {0}" + Environment.NewLine, layer.Name));
                }
            }

            return sb.ToString();
        }

        private static string OverrideSensorData(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (trinity_OverrideSensorData)objData;
            sb.AppendLine("Realizing Dist: " + data.RealizingDistance);
            sb.AppendLine("Unrealizing Dist: " + data.UnrealizingDistance);
            sb.AppendLine("Loading Dist: " + data.LoadingDistance);
            sb.AppendLine("Unloading Dist: " + data.UnloadingDistance);

            return sb.ToString();
        }

        private static string ScriptComponent(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (trinity_ScriptComponent)objData;
            sb.AppendLine("File: " + data.FilePath);
            sb.AppendLine("Package: " + data.PackageName);
            sb.AppendLine("Is static: " + (data.IsStatic ? "True" : "False"));

            return sb.ToString();
        }

        private static string TextComponent(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (pe_TextComponent)objData;
            sb.AppendLine("File: " + data.FilePath);

            return sb.ToString();
        }

        private static string InputEventTriggerComponent(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (pe_InputEventTriggerComponent)objData;
            sb.AppendLine("Input name: " + data.InputName);
            sb.AppendLine("Resource name: " + data.ResourceName);

            return sb.ToString();
        }

        public static string GetProperties(string sceneComponent, object objData)
        { 
            string ret = string.Empty;

            switch (sceneComponent)
            {
                case "trinity_CameraEntity": ret = CameraEntity(objData); break;
                case "trinity_SceneObject": ret = SceneObject(objData); break;
                case "trinity_OverrideSensorData": ret = OverrideSensorData(objData); break;
                case "trinity_ScriptComponent": ret = ScriptComponent(objData); break;
                case "pe_TextComponent": ret = TextComponent(objData); break;
                case "pe_InputEventTriggerComponent": ret = InputEventTriggerComponent(objData); break;

            }

            return ret;
        }
    }
}
