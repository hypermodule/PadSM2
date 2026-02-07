using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace PadSM2;

public class Program
{
    public static void Main(string[] args)
    {
        Debug();
        //ManualTest(args);
    }

    public static void ManualTest(string[] args)
    {
        Console.WriteLine("PadSM2 v0.0.2");

        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: PadmSM2 UASSET_FILE");
            Environment.Exit(1);
        }

        var uassetPath = args[0];
        var uexpPath = Path.ChangeExtension(uassetPath, ".uexp");

        var uasset = ByteAsset.Read(uassetPath);
        var uexp = ByteAsset.Read(uexpPath);

        var (paddedUasset, paddedUexp) = MeshPadder.PadStaticMesh(uasset, uexp);
        var convertedAsset = MeshConverter.ConvertToAugusta(paddedUasset, paddedUexp);

        var tmpDir = Directory.CreateTempSubdirectory();
        var tmpUassetPath = Path.Join(tmpDir.FullName, "converted.uasset");
        var tmpUexpPath = Path.ChangeExtension(tmpUassetPath, ".uexp");
        convertedAsset.Write(tmpUassetPath);
        
        var tmpUasset = ByteAsset.Read(tmpUassetPath);
        var tmpUexp = ByteAsset.Read(tmpUexpPath);
        
        var augustaUsmap = MeshConverter.GetUsmap("PadSM2.Augusta.usmap");
        var x = MeshConverter.ParseAsset(tmpUasset, tmpUexp, augustaUsmap, CustomSerializationFlags.NoDummies);
        x.PackageFlags &= ~EPackageFlags.PKG_UnversionedProperties;
        PropertyTypeNameFixer.Populate(x);
        x.AddNameReference(new FString("None"));
        var outDir = Path.Join(tmpDir.FullName, "out");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Join(outDir, Path.GetFileName(uassetPath));
        x.Write(outPath);
        Console.WriteLine("wrote " + outPath);


        /*
        File.Move(uassetPath, Path.ChangeExtension(uassetPath, ".uasset.bak"));
        File.Move(uexpPath, Path.ChangeExtension(uexpPath, ".uexp.bak"));
        */
    }

    public static void Debug()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).CreateLogger();
        
        const string _archiveDirectory = "/home/hyper/.local/share/Steam/steamapps/common/Grounded2/Augusta/Content/Paks/";
        const string _mapping = "/home/hyper/.local/share/Steam/steamapps/common/Grounded2/Augusta.usmap";
        const string assetPath = "Game/Content/Art/World/ZN00_Global/Coins/Coin_Quarter/SM_Coin_Quarter_A.SM_Coin_Quarter_A";
        
        var version = new VersionContainer(EGame.GAME_Grounded2);
        var provider = new DefaultFileProvider(_archiveDirectory, SearchOption.AllDirectories, version)
        {
            //MappingsContainer = new FileUsmapTypeMappingsProvider(_mapping)
        };
        provider.Initialize();
        provider.Mount();
        
        var obj = provider.LoadPackageObject(assetPath);
        Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
    }
}
