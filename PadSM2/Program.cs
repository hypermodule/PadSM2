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

        File.Move(uassetPath, Path.ChangeExtension(uassetPath, ".uasset.bak"));
        File.Move(uexpPath, Path.ChangeExtension(uexpPath, ".uexp.bak"));

        convertedAsset.Write(uassetPath);
    }
}
