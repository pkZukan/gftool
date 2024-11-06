using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene
{
    [FlatBufferTable]
    public class SceneChunk
    {
        [FlatBufferItem(0)]
        public string Type { get; set; }

        [FlatBufferItem(1)]
        public byte[] Data { get; set; }

        [FlatBufferItem(2)]
        public SceneChunk[] Children { get; set; }
    }

    [FlatBufferTable]
    public class TRSCN
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public string TimerName { get; set; }

        [FlatBufferItem(2)]
        public string ScriptPath { get; set; }

        [FlatBufferItem(3)]
        public string ScriptPackage { get; set; }

        [FlatBufferItem(4)]
        public SceneChunk[] Chunks { get; set; }

        [FlatBufferItem(5)]
        public string[] SubScenes { get; set; }

        [FlatBufferItem(6)]
        public bool unk_5 { get; set; }

        [FlatBufferItem(7)]
        public bool unk_6 { get; set; }
    }
}
