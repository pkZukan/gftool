using System.Text.Json.Serialization;
using static GFTool.Core.Structures.GFLX.BSEQCommands;

namespace GFTool.Core.Models.GFLX
{
    public class SequenceCommandOptions
    {
        public string Name { get; set; } = String.Empty;

        public uint Value { get; set; } = 0;
    }
    public class SequenceCommand
    {
        [JsonPropertyOrderAttribute(0)]
        public int StartFrame;
        [JsonPropertyOrderAttribute(1)]
        public int EndFrame;
        [JsonPropertyOrderAttribute(2)]
        public string Name { get; set; } = String.Empty;
        [JsonPropertyOrderAttribute(3)]
        public SequenceCommandOptions[] Options { get; set; } = Array.Empty<SequenceCommandOptions>();
        [JsonPropertyOrderAttribute(4)]
        public IBSEQArgReader? Arguments { get; set; } = null;
    }

    public class SequenceGroup
    {
        public List<SequenceCommand> Commands { get; set; } = new List<SequenceCommand>();
    }
 
    public class Sequence
    {
        public int frames;

        public Dictionary<uint, SequenceGroup> Groups { get; set; } = new Dictionary<uint, SequenceGroup>();
    }
}
