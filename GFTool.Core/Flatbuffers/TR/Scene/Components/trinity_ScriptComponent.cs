using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_ScriptComponent
    {
        [FlatBufferItem(0)]
        public string FilePath { get; set; }

        [FlatBufferItem(1)]
        public string PackageName { get; set; }

        [FlatBufferItem(2)]
        public bool IsParallelized { get; set; }

        [FlatBufferItem(3)]
        public float Priority { get; set; }

        [FlatBufferItem(4)]
        public bool IsStatic { get; set; }

        [FlatBufferItem(5)]
        public string ResName { get; set; }
    }
}
