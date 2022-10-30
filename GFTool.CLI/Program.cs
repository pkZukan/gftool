using System.Text.Json;
using GFToolCore.Models.GFLX;
using GFToolCore.Serializers.GFLX;

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
static void Main(string[] args)
{
    var bseq = BSEQSerializer.Deserialize(new BinaryReader(File.Open("d020.bseq", FileMode.Open, FileAccess.Read)));
    var jsonbseq = JsonSerializer.Serialize<Sequence>(bseq, new JsonSerializerOptions() { WriteIndented = true }); ;
    File.WriteAllText("d020.bseq.json", jsonbseq);
}
