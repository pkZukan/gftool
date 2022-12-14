using System.Text.Json;
using Trinity.Core.Cache;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary.LA;
using Trinity.Core.Flatbuffers.TR.Animation;
using Trinity.Core.Models.GFLX;
using Trinity.Core.Serializers.GFLX;
using Trinity.Core.Serializers;
using Trinity.Core.Utils;
using System.Diagnostics;
using GFTool.Core.Flatbuffers.TR.Scene;

////GFPak exporting
static void ConvertGFPAK(string[] args)
{
    var path = Path.GetFullPath(args[0]);
    Trace.WriteLine(path);
    GFPakHashCache.Init();
    var paks = Directory.EnumerateFiles(args[0], "*.gfpak");
    foreach (var pak in paks) {
        var gfpak = GFPakSerializer.Deserialize(new BinaryReader(File.Open(pak, FileMode.Open, FileAccess.Read)));
        foreach (var gfFolder in gfpak.folders)
        {
            foreach (var file in gfFolder.files)
            {
                var fullname = path + "\\" + file.fullname;
                FileInfo finfo = new FileInfo(fullname);
                finfo.Directory.Create();
                File.WriteAllBytes(finfo.FullName, file.data);
            }
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

static void ReadText(string[] args) {
    string tableFile = args[0];
    string datFile = tableFile.Replace("tbl", "dat");
    var br = new BinaryReader(File.Open(tableFile, FileMode.Open));
    /*var dic = AHTBSerializer.Deserialize(br);
    int i = 0;
    foreach (var d in dic) 
        Console.WriteLine("ind: {0} | Key = {1}, Value = {2}", i++, d.Key, d.Value);*/
}

static void TraverseTrsot(SceneEntry ent) {
    if(ent.SubObjects.Length == 0) return;
    
    foreach (var e in ent.SubObjects) {
        Console.WriteLine(e.TypeName);
        TraverseTrsot(e);
        File.WriteAllBytes(e.TypeName, e.NestedType);
    }
}

static void Test(string[] args) {
    var trsot = FlatBufferConverter.DeserializeFrom<TrinitySceneObjTemplate>(args[0]);
    foreach (var t in trsot.SceneObjectList) {
        Console.WriteLine(t.TypeName);
        TraverseTrsot(t);
        File.WriteAllBytes(t.TypeName, t.NestedType);
    }
}

static void ParseCommands(string[] args) {
    if (args.Length < 3) {
        Console.WriteLine("Usage: CLI [option] <arg>");
        Console.WriteLine("Options:\n\t-t = type of command [text, gfpak]");
    }
    int i = 0;
    string commType = "";
    switch (args[i++]) {
        case "-t": commType = args[i++]; break;
    }
    string[] newArgs = args.Skip(i).ToArray();
    switch (commType) {
        case "text": ReadText(newArgs); break;
        case "gfpak": ConvertGFPAK(newArgs); break;
    }
}

//ConvertTRANMtoJSON(args);
//ConvertJSONtoTRANM(args);
//ConvertGFPAK(args);
//ReadText(args);
ParseCommands(args);