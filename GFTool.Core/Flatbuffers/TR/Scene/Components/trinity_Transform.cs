using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    // Scene files store SRT as a table (uoffset), not an inline struct.
    // Layout observed in example scenes:
    //  0: Scale (Vector3f)
    //  1: Rotate (Vector4f)  // quaternion-like (WXYZ in files we tested)
    //  2: Translate (Vector3f)
    [FlatBufferTable]
    public class trinity_Transform
    {
        [FlatBufferItem(0)]
        public Vector3f Scale { get; set; } = new Vector3f();

        [FlatBufferItem(1)]
        public Vector4f Rotate { get; set; } = new Vector4f();

        [FlatBufferItem(2)]
        public Vector3f Translate { get; set; } = new Vector3f();
    }
}
