using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Core.Flatbuffers.TR.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityUikitEditor
{
    public class TRUiViewProperties
    {
        private static string UikitSwitchPanel(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitSwitchPanel)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitSwitchItem(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitSwitchItem)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitSwitch(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitSwitch)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitShortcut(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitShortcut)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitScrollPanel(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitScrollPanel)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitOptionGuide(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitOptionGuide)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitGridPanel(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitGridPanel)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitGauge(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitGauge)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitCursor(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitCursor)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitButton(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitButton)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        private static string UikitBody(object objData)
        {
            StringBuilder sb = new StringBuilder();

            var data = (UikitBody)objData;
            sb.AppendLine("Name: " + data.Name);

            return sb.ToString();
        }

        public static string GetProperties(string sceneComponent, object objData)
        {
            string ret = string.Empty;

            switch (sceneComponent)
            {
                case "UikitBody": ret = UikitBody(objData); break;
                case "UikitButton": ret = UikitButton(objData); break;
                case "UikitCursor": ret = UikitCursor(objData); break;
                case "UikitGauge": ret = UikitGauge(objData); break;
                case "UikitGridPanel": ret = UikitGridPanel(objData); break;
                case "UikitOptionGuide": ret = UikitOptionGuide(objData); break;
                case "UikitScrollPanel": ret = UikitScrollPanel(objData); break;
                case "UikitShortcut": ret = UikitShortcut(objData); break;
                case "UikitSwitch": ret = UikitSwitch(objData); break;
                case "UikitSwitchItem": ret = UikitSwitchItem(objData); break;
                case "UikitSwitchPanel": ret = UikitSwitchPanel(objData); break;
            }

            return ret;
        }
    }
}
