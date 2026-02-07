using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace PadSM2;

public class Program
{
    public static void Main(string[] args)
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
        
        var unversionedUasset = ByteAsset.Read(tmpUassetPath);
        var unversionedUexp = ByteAsset.Read(tmpUexpPath);
        
        var augustaUsmap = MeshConverter.GetUsmap("PadSM2.Augusta.usmap");
        var versionedAsset = MeshConverter.ParseAsset(unversionedUasset, unversionedUexp, augustaUsmap, CustomSerializationFlags.NoDummies);
        versionedAsset.PackageFlags &= ~EPackageFlags.PKG_UnversionedProperties;
        PropertyTypeNameFixer.Populate(versionedAsset);
        versionedAsset.AddNameReference(new FString("None"));

        var outputDir = Path.GetDirectoryName(uassetPath);
        if (string.IsNullOrEmpty(outputDir)) outputDir = ".";
        var tmpOutBase = Path.GetFileNameWithoutExtension(uassetPath) + ".padsm2.tmp." + Random.Shared.Next();
        var tmpOutPath = Path.Join(outputDir, tmpOutBase + ".uasset");
        var tmpOutUexpPath = Path.ChangeExtension(tmpOutPath, ".uexp");

        versionedAsset.Write(tmpOutPath);

        var uassetBakPath = Path.ChangeExtension(uassetPath, ".uasset.bak");
        var uexpBakPath = Path.ChangeExtension(uexpPath, ".uexp.bak");

        if (File.Exists(uassetPath))
        {
            File.Move(uassetPath, uassetBakPath, true);
        }
        if (File.Exists(uexpPath))
        {
            File.Move(uexpPath, uexpBakPath, true);
        }

        File.Move(tmpOutPath, uassetPath, true);
        if (File.Exists(tmpOutUexpPath))
        {
            File.Move(tmpOutUexpPath, uexpPath, true);
        }
    }
}
