using FlatSharp;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Flatbuffers.TR.Model;
using PropVal = FlatSharp.FlatBufferUnion<GFTool.Core.Flatbuffers.TR.Scene.Components.PropInt, GFTool.Core.Flatbuffers.TR.Scene.Components.PropDec, GFTool.Core.Flatbuffers.TR.Scene.Components.PropStr, Trinity.Core.Flatbuffers.Utils.Vector2f, Trinity.Core.Flatbuffers.Utils.Vector3f, Trinity.Core.Flatbuffers.Utils.Vector4f, GFTool.Core.Flatbuffers.TR.Scene.Components.PropEnum, GFTool.Core.Flatbuffers.TR.Scene.Components.PropList, GFTool.Core.Flatbuffers.TR.Scene.Components.PropTable, GFTool.Core.Flatbuffers.TR.Scene.Components.PropUnion>;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class PropInt
    {
        [FlatBufferItem(0)] public ulong Data { get; set; }
        [FlatBufferItem(1)] public byte Size { get; set; }
    }

    [FlatBufferTable]
    public class PropDec
    {
        [FlatBufferItem(0)] public double Data { get; set; }
        [FlatBufferItem(1)] public byte Size { get; set; }
    }

    [FlatBufferTable]
    public class PropStr
    {
        [FlatBufferItem(0)] public string Data { get; set; }
    }

    [FlatBufferTable]
    public class PropEnum
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public int Val { get; set; }
    }

    [FlatBufferTable]
    public class PropUnion
    {
        [FlatBufferItem(0)] public PropEnum Type { get; set; }
        [FlatBufferItem(1)] public PropTable Val { get; set; }
    }

    [FlatBufferTable]
    public class PropField
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public PropVal? Value { get; set; }
    }

    [FlatBufferTable]
    public class PropTable
    {
        [FlatBufferItem(0)] public IList<PropField> Fields { get; set; }
    }

    [FlatBufferTable]
    public class PropList
    {
        [FlatBufferItem(0)] public IList<PropVal> Vals { get; set; }
    }

    [FlatBufferTable]
    public class trinity_PropertySheet
    {
        [FlatBufferItem(0)] public string property_name { get; set; }
        [FlatBufferItem(1)] public string property_template { get; set; }
        [FlatBufferItem(2)] public IList<PropTable> unk0 { get; set; }
    }

}
