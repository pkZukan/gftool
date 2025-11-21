using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer
{
    public class Transform
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public Transform()
        {
            Position = new Vector3();
            Rotation = new Quaternion();
            Scale = new Vector3();
        }
    }
}
