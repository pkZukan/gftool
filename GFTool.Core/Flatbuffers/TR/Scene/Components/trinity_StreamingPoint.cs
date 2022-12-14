using FlatSharp.Attributes;
using System.Drawing;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class Object
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public byte[] NestedType { get; set; }
    }

    [FlatBufferTable]
    public class Point
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public Vec3f Position { get; set; }
    }

    [FlatBufferTable]
    public class Entry 
    {
        [FlatBufferItem(0)]
        public Point Point { get; set; }

        [FlatBufferItem(1)]
        public Object[] Objects { get; set; }
    }

    [FlatBufferTable]
    public class trinity_StreamingPoint
    {
        [FlatBufferItem(0)]
        public Entry[] Entries { get; set; }
    }
}
