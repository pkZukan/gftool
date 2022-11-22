using System.Text.Json;
using GFTool.Core.Cache;
using GFTool.Core.Flatbuffers.TR.ResourceDictionary;
using GFTool.Core.Flatbuffers.TR.Animation;
using GFTool.Core.Models.GFLX;
using GFTool.Core.Serializers.GFLX;
using GFTool.Core.Utils;

////GFPak exporting
static void ConvertGFPAK()
{
    GFPakHashCache.Init();

    var gfpak = GFPakSerializer.Deserialize(new BinaryReader(File.Open("pm0201_37_00.gfpak", FileMode.Open, FileAccess.Read)));
    foreach (var folder in gfpak.folders)
    {
        Console.WriteLine(folder.path);
        foreach (var file in folder.files)
        {
            Console.WriteLine('\t' + file.path);
            Console.WriteLine('\t' + file.name);
        }
    }
}
//Flatbuffer Serialization
static void ConvertFlatbuffer()
{
    var flatbuffer = FlatBufferConverter.DeserializeFrom<PokemonCatalog>("poke_resource_table.trpmcatalog");
    var jsonflatbuffer = JsonSerializer.Serialize(flatbuffer);
    File.WriteAllText("poke_resource_table.trpmcatalogj", jsonflatbuffer);
}

//TRANM Serialization
static void ConvertTRANMtoJSON(string[] args)
{
    //Console.WriteLine(QuaternionConverter.);
    var flatbuffer = FlatBufferConverter.DeserializeFrom<TRANM>(args[0]);
    var jsonflatbuffer = JsonSerializer.Serialize(flatbuffer, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(args[1], jsonflatbuffer);
}

static void ConvertJSONtoTRANM(string[] args)
{
    var stream = File.ReadAllText(args[0]);
    var flatbufferjson = JsonSerializer.Deserialize<TRANM>(stream);
    File.WriteAllBytes(args[1], FlatBufferConverter.SerializeFrom<TRANM>(flatbufferjson));
}

//BSEQ conversion
static void ConvertBSEQ()
{
    var bseq = BSEQSerializer.Deserialize(new BinaryReader(File.Open("d020.bseq", FileMode.Open, FileAccess.Read)));
    var jsonbseq = JsonSerializer.Serialize<Sequence>(bseq, new JsonSerializerOptions() { WriteIndented = true });
    File.WriteAllText("d020.bseq.json", jsonbseq);
}

ConvertTRANMtoJSON(args);
//ConvertJSONtoTRANM(args);
