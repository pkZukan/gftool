using GFToolCore.Models.GFLX;
using GFToolCore.Structures.GFLX;
using GFToolCore.Utils;

namespace GFToolCore.Serializers.GFLX
{
    public static class BSEQSerializer
    {
        public static Sequence Deserialize(BinaryReader br)
        {

            var header = br.ReadBytes(BSEQHeader.SIZE).ToStruct<BSEQHeader>();

            var seq = new Sequence()
            {
                frames = (int)header.FrameCount
            };

            Dictionary<UInt64, uint> commandSizes = new Dictionary<UInt64, uint>();

            for (int i = 0; i < header.HashSizeCount; i++)
            {
                var hashsize = br.ReadBytes(BSEQHashSizeEntry.SIZE).ToStruct<BSEQHashSizeEntry>();
                commandSizes[hashsize.Hash] = hashsize.Size;
            }

            List<BSEQCommandEntry> commandEntries = new List<BSEQCommandEntry>();
            while (br.PeekUInt32() != 0xFFFFFFFF)
            {
                var startFrame = br.ReadUInt32();
                var endFrame = br.ReadUInt32();
                var groupNo = br.ReadUInt32();

                var groupOptions = new BSEQGroupOption[header.GroupOptionCount];
                for (int i = 0; i < groupOptions.Length; i++)
                {
                    groupOptions[i] = br.ReadBytes(BSEQGroupOption.SIZE).ToStruct<BSEQGroupOption>();
                }

                var commandHash = br.ReadUInt64();
                var commandBuffer = br.ReadBytes((int)commandSizes[commandHash]);
                commandEntries.Add(new BSEQCommandEntry()
                {
                    StartFrame = startFrame,
                    EndFrame = endFrame,
                    GroupNo = groupNo,
                    GroupOptions = groupOptions,
                    Hash = commandHash,
                    Buffer = commandBuffer,
                });
            }

            seq.Groups = new Dictionary<uint, SequenceGroup>();

            foreach (var entry in commandEntries)
            {
                if (!seq.Groups.ContainsKey(entry.GroupNo))
                {
                    seq.Groups[entry.GroupNo] = new SequenceGroup() { Commands = new List<SequenceCommand>() };
                }

                var command = new SequenceCommand()
                {
                    Name = entry.Hash.ToString(),
                    StartFrame = (int)entry.StartFrame,
                    EndFrame = (int)entry.EndFrame,
                    Arguments = BSEQCommands.Parse(entry.Hash, entry.Buffer),
                    Options = new SequenceCommandOptions[entry.GroupOptions.Length]
                };

                for (int i = 0; i < entry.GroupOptions.Length; i++)
                {
                    command.Options[i] = new SequenceCommandOptions()
                    {
                        Name = entry.GroupOptions[i].Hash.ToString(),
                        Value = entry.GroupOptions[i].Value,
                    };
                }

                seq.Groups[entry.GroupNo].Commands.Add(command);
            }

            return seq;
        }

        public static void Serialize(BinaryWriter bw, Sequence sequence)
        {

        }
    }
}
