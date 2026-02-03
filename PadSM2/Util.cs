using System.Reflection;

namespace PadSM2;

public record ByteAsset(string Name, byte[] Bytes)
{
    public static ByteAsset Read(string path) => new(path, File.ReadAllBytes(path));
}

public static class Util
{
    public static Stream GetEmbeddedResource(string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(path);

        return stream != null ? stream : throw new FileNotFoundException("Couldn't find embedded resource: " + path);
    }

    public static byte[] GetEmbeddedResourceBytes(string path)
    {
        var stream = GetEmbeddedResource(path);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}