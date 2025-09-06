using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace PadSM2;

/// <summary>
/// Class for inserting an 1i32 (little-endian) into each StaticMeshSection
/// of a static mesh asset (not sure what role it plays, but Augusta meshes
/// have this).
/// </summary>
public static class MeshPadder
{
    private static void WriteLong(List<byte> bytes, int offset, long value)
    {
        var longBytes = BitConverter.GetBytes(value);

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(longBytes);
        }

        for (var i = 0; i < longBytes.Length; i++)
        {
            bytes[offset + i] = longBytes[i];
        }
    }

    private static IFileProvider GetGenericUsmap()
    {
        var usmapBytes = Util.GetEmbeddedResourceBytes("PadSM2.Generic.usmap");

        return new StreamedFileProvider("game", pathComparer: null)
        {
            MappingsContainer = new FileUsmapTypeMappingsProvider(usmapBytes)
        };
    }

    private static Package ParseAsset(ByteAsset uasset, ByteAsset uexp)
    {
        var versions = new VersionContainer(EGame.GAME_UE5_4);

        var uassetArchive = new FByteArchive(uasset.Name, uasset.Bytes, versions);
        var uexpArchive = new FByteArchive(uexp.Name, uexp.Bytes, versions);
        var usmapProvider = GetGenericUsmap();

        return new Package(uassetArchive, uexpArchive, (FArchive?)null, provider: usmapProvider);
    }

    public static (ByteAsset, ByteAsset) PadStaticMesh(ByteAsset uasset, ByteAsset uexp)
    {
        var package = ParseAsset(uasset, uexp);

        // Find the StaticMesh export (main export)
        int staticMeshExportIndex = -1;
        UStaticMesh? staticMesh = null;

        for (var i = 0; i < package.ExportMapLength; i++)
        {
            var export = package.ExportsLazy[i].Value;

            if (export is UStaticMesh sm)
            {
                staticMeshExportIndex = i;
                staticMesh = sm;
            }
        }

        if (staticMeshExportIndex == -1 || staticMesh == null)
        {
            throw new Exception("Could not find StaticMesh export");
        }

        // Find the StaticMeshSections
        var staticMeshSections = new List<FStaticMeshSection>();

        foreach (var lod in staticMesh.RenderData.LODs)
        {
            staticMeshSections.AddRange(lod.Sections);
        }

        // Insert padding bytes into each StaticMeshSection
        byte[] padBytes = [1, 0, 0, 0];

        var uexpBytes = uexp.Bytes.ToList();

        for (var i = 0; i < staticMeshSections.Count; i++)
        {
            var staticMeshSection = staticMeshSections[i];

            var insertionOffset = (int)staticMeshSection.OffsetOfPaddingForGrounded2;

            // Remember to shift the offset by the number of bytes already inserted
            insertionOffset += i * padBytes.Length;

            uexpBytes.InsertRange(insertionOffset, padBytes);
        }

        // Update export table with new size of main export
        var sizeIncrease = padBytes.Length * staticMeshSections.Count;

        var mainExport = package.ExportMap[staticMeshExportIndex];

        var newSerialSize = mainExport.SerialSize + sizeIncrease;

        var uassetBytes = uasset.Bytes.ToList();

        WriteLong(uassetBytes, (int)mainExport.OffsetOfSerialSize, newSerialSize);

        // Update export table with new offsets for any subsequent exports
        for (var i = staticMeshExportIndex + 1; i < package.ExportMapLength; i++)
        {
            var objectExport = package.ExportMap[i];
            var serialOffset = objectExport.SerialOffset;
            var newSerialOffset = serialOffset + sizeIncrease;
            WriteLong(uassetBytes, (int)(objectExport.OffsetOfSerialSize + 8), newSerialOffset);
        }

        // Update bulk data start offset
        if (package.Summary.BulkDataStartOffset > 0)
        {
            var newBulkDataStartOffset = package.Summary.BulkDataStartOffset + sizeIncrease;
            WriteLong(uassetBytes, (int)package.Summary.OffsetOfBulkDataStartOffset, newBulkDataStartOffset);
        }

        return (new ByteAsset(uasset.Name, uassetBytes.ToArray()), new ByteAsset(uexp.Name, uexpBytes.ToArray()));
    }
}
