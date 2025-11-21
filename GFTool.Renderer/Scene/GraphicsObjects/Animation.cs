using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.TR.Animation;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Animation
    {
        private PlayType PlayType;
        private uint FrameCount;
        private uint FrameRate;

        public Animation(TRANM tranim) 
        {
            PlayType = tranim.Info.PlayType;
            FrameCount = tranim.Info.FrameCount;
            FrameRate = tranim.Info.FrameRate;
        }
    }
}
