using Newtonsoft.Json;
using static GFTool.Structures.GFLX.BSEQCommands;

namespace GFTool.Models.GFLX
{
    public class SequenceCommandOptions
    {
        [JsonProperty("Name")]
        public string Name { get; set; } = String.Empty;
        [JsonProperty("Value")]
        public uint Value { get; set; } = 0;
    }
    public class SequenceCommand
    {
        [JsonProperty("StartFrame", Order = 0)]
        public int StartFrame;
        [JsonProperty("EndFrame", Order = 1)]
        public int EndFrame;
        [JsonProperty("Name", Order = 2)]
        public string Name { get; set; } = String.Empty;
        [JsonProperty("Options", Order = 3)]
        public SequenceCommandOptions[] Options { get; set; } = Array.Empty<SequenceCommandOptions>();
        [JsonProperty("Arguments", Order = 4)]
        public IBSEQArgReader? Arguments { get; set; } = null;
    }

    public class SequenceGroup
    {
        [JsonProperty("Commands")]
        public List<SequenceCommand> Commands { get; set; } = new List<SequenceCommand>();
    }
 
    public class Sequence
    {
        [JsonProperty("Frames")]
        public int frames;
        [JsonProperty("Groups")]
        public Dictionary<uint, SequenceGroup> Groups { get; set; } = new Dictionary<uint, SequenceGroup>();
    }
}
