using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace PadSM2;

public static class MeshConverter
{
    public static Usmap GetUsmap(string path)
    {
        var usmapStream = Util.GetEmbeddedResource(path);
        var usmap = new Usmap();
        var usmapReader = new UsmapBinaryReader(usmapStream, usmap);
        usmap.Read(usmapReader);
        return usmap;
    }

    public static UAsset ParseAsset(ByteAsset uasset, ByteAsset uexp, Usmap usmap, CustomSerializationFlags customSerializationFlags = CustomSerializationFlags.None)
    {
        var assetStream = new MemoryStream();
        assetStream.Write(uasset.Bytes);
        assetStream.Write(uexp.Bytes);
        assetStream.Position = 0;
        var assetReader = new AssetBinaryReader(assetStream);

        return new UAsset(assetReader, EngineVersion.VER_UE5_6, usmap, useSeparateBulkDataFiles: true, customSerializationFlags: customSerializationFlags);
    }

    private static StructPropertyData CreateResponseChannel(UAsset asset, int index, string channelValue, string responseValue)
    {
        var structPropertyData = new StructPropertyData(FName.DefineDummy(asset, index.ToString()), FName.FromString(asset, "ResponseChannel"));

        var namePropertyData = new NamePropertyData(FName.FromString(asset, "Channel"))
        {
            Value = FName.FromString(asset, channelValue)
        };

        var enumPropertyData = new EnumPropertyData(FName.FromString(asset, "Response"))
        {
            EnumType = FName.FromString(asset, "ECollisionResponse"),
            InnerType = FName.FromString(asset, "ByteProperty"),
            Value = FName.FromString(asset, responseValue)
        };

        structPropertyData.Value.Add(namePropertyData);
        structPropertyData.Value.Add(enumPropertyData);

        return structPropertyData;
    }

    public static UAsset ConvertToAugusta(ByteAsset uasset, ByteAsset uexp)
    {
        var vanillaUsmap = GetUsmap("PadSM2.Generic.usmap");
        var asset = ParseAsset(uasset, uexp, vanillaUsmap);
        
        // Switch the asset over to using a schema from Augusta
        var augustaUsmap = GetUsmap("PadSM2.Augusta.usmap");
        asset.Mappings = augustaUsmap;

        // Turn off unversioned property serialization
        //asset.PackageFlags &= ~EPackageFlags.PKG_UnversionedProperties;

        // Replace CollisionProfileName with CollisionResponses array
        var collisionResponse = new StructPropertyData(FName.FromString(asset, "CollisionResponses"), FName.FromString(asset, "CollisionResponse"));

        var responseArray = new ArrayPropertyData(FName.FromString(asset, "ResponseArray"))
        {
            Value = [
                CreateResponseChannel(asset, 0, "SteeringAvoidance", "ECR_Block"),
                CreateResponseChannel(asset, 1, "Attack", "ECR_Block"),
                CreateResponseChannel(asset, 2, "Interaction", "ECR_Block"),
                CreateResponseChannel(asset, 3, "BuildingValidity", "ECR_Overlap"),
                CreateResponseChannel(asset, 4, "BuildingPlacement", "ECR_Overlap"),
            ]
        };

        collisionResponse.Value.Add(responseArray);

        var bodySetup = (NormalExport)asset.Exports.First(x => x.ObjectName.Value.ToString() == "BodySetup");
        var defaultInstance = (StructPropertyData)bodySetup.Data.First(x => x.Name.Value.ToString() == "DefaultInstance");
        defaultInstance.Value[1] = collisionResponse;

        return asset;
    }
}
