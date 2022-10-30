using GFTool.Flatbuffers.TR.ResourceDictionary;
using GFTool.Utils;
using Newtonsoft.Json;
using GFTool.Serializers.GFLX;
using GFTool.Structures.GFLX;
using GFTool.Models;
using GFTool.Cache;

////GFPak exporting
//GFPakHashCache.Init();

//var gfpak = GFPakSerializer.Deserialize(new BinaryReader(File.Open("pm0201_37_00.gfpak", FileMode.Open, FileAccess.Read)));
//foreach (var folder in gfpak.folders)
//{
//    Console.WriteLine(folder.path);
//    foreach (var file in folder.files)
//    {
//        Console.WriteLine('\t' + file.path);
//        Console.WriteLine('\t' + file.name);
//    }
//}

//Flatbuffer Serialization
//var flatbuffer = FlatBufferConverter.DeserializeFrom<PokemonCatalog>("poke_resource_table.trpmcatalog");
//var jsonflatbuffer = JsonConvert.SerializeObject(flatbuffer, Formatting.Indented);
//File.WriteAllText("poke_resource_table.trpmcatalogj", jsonflatbuffer);

//BSEQ conversion
var bseq = BSEQSerializer.Deserialize(new BinaryReader(File.Open("d020.bseq", FileMode.Open, FileAccess.Read)));
var jsonbseq = JsonConvert.SerializeObject(bseq, Formatting.Indented);
File.WriteAllText("d020.bseq.json", jsonbseq);
