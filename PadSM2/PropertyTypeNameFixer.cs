using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace PadSM2;

public static class PropertyTypeNameFixer
{
    public static void Populate(UAsset asset)
    {
        if (asset == null) return;
        if (asset.HasUnversionedProperties) return;
        if (asset.ObjectVersionUE5 < ObjectVersionUE5.PROPERTY_TAG_COMPLETE_TYPE_NAME) return;

        asset.ResolveAncestries();

        foreach (var export in asset.Exports)
        {
            switch (export)
            {
                case NormalExport normalExport:
                    if (normalExport.Data != null)
                    {
                        foreach (var prop in normalExport.Data) PopulateProperty(prop, asset);
                    }
                    break;
            }
        }
    }

    private static void PopulateProperty(PropertyData property, UAsset asset)
    {
        if (property == null) return;

        if (property.PropertyTypeName == null)
        {
            property.PropertyTypeName = BuildPropertyTypeName(property, asset);
        }

        switch (property)
        {
            case StructPropertyData structProp:
                if (structProp.Value != null)
                {
                    foreach (var child in structProp.Value) PopulateProperty(child, asset);
                }
                break;
            case SetPropertyData setProp:
                if (setProp.ElementsToRemove != null)
                {
                    foreach (var child in setProp.ElementsToRemove) PopulateProperty(child, asset);
                }
                if (setProp.Value != null)
                {
                    foreach (var child in setProp.Value) PopulateProperty(child, asset);
                }
                break;
            case ArrayPropertyData arrayProp:
                if (arrayProp.Value != null)
                {
                    foreach (var child in arrayProp.Value) PopulateProperty(child, asset);
                }
                break;
            case MapPropertyData mapProp:
                if (mapProp.KeysToRemove != null)
                {
                    foreach (var child in mapProp.KeysToRemove) PopulateProperty(child, asset);
                }
                if (mapProp.Value != null)
                {
                    foreach (var entry in mapProp.Value)
                    {
                        PopulateProperty(entry.Key, asset);
                        PopulateProperty(entry.Value, asset);
                    }
                }
                break;
        }
    }

    private static FPropertyTypeName BuildPropertyTypeName(PropertyData property, UAsset asset)
    {
        var nodes = BuildNodes(property, asset);
        return new FPropertyTypeName(nodes, true);
    }

    private static List<FPropertyTypeNameNode> BuildNodes(PropertyData property, UAsset asset)
    {
        switch (property)
        {
            case SetPropertyData setProp:
                return BuildArrayNodes(setProp, asset, "SetProperty");
            case ArrayPropertyData arrayProp:
                return BuildArrayNodes(arrayProp, asset, "ArrayProperty");
            case MapPropertyData mapProp:
                return BuildMapNodes(mapProp, asset);
            case StructPropertyData structProp:
                return BuildStructNodes(property.PropertyType?.Value ?? "StructProperty", structProp.StructType, property, asset);
            case RawStructPropertyData rawStructProp:
                return BuildStructNodes(property.PropertyType?.Value ?? "RawStructProperty", rawStructProp.StructType, property, asset);
            case EnumPropertyData enumProp:
                return BuildEnumNodes(enumProp, asset);
            case BytePropertyData byteProp:
                return BuildByteNodes(byteProp, asset);
            default:
                return BuildSimpleNodes(property.PropertyType?.Value ?? "None", asset);
        }
    }

    private static List<FPropertyTypeNameNode> BuildArrayNodes(ArrayPropertyData arrayProp, UAsset asset, string rootType)
    {
        var elementNodes = GetArrayElementNodes(arrayProp, asset);
        return BuildNodeList(rootType, 1, elementNodes, asset);
    }

    private static List<FPropertyTypeNameNode> GetArrayElementNodes(ArrayPropertyData arrayProp, UAsset asset)
    {
        if (arrayProp.Value != null && arrayProp.Value.Length > 0 && arrayProp.Value[0] != null)
        {
            return BuildNodes(arrayProp.Value[0], asset);
        }

        if (asset.Mappings != null && asset.Mappings.TryGetPropertyData(arrayProp.Name, arrayProp.Ancestry, asset, out UsmapArrayData arrData))
        {
            return BuildNodesFromUsmapPropertyData(arrData.InnerType, asset);
        }

        var arrayType = arrayProp.ArrayType;
        if (arrayType == null)
        {
            return BuildStructNodes("StructProperty", null, arrayProp, asset);
        }

        if (arrayType.Value?.Value == "StructProperty")
        {
            var structType = arrayProp.DummyStruct?.StructType;
            var arrayName = arrayProp.Name?.Value?.Value;
            if (structType == null && arrayName != null && asset.ArrayStructTypeOverride.TryGetValue(arrayName, out var overrideType))
            {
                structType = new FName(asset, overrideType);
            }
            return BuildStructNodes("StructProperty", structType, arrayProp, asset);
        }

        return BuildSimpleNodes(arrayType.Value?.Value ?? "None", asset);
    }

    private static List<FPropertyTypeNameNode> BuildMapNodes(MapPropertyData mapProp, UAsset asset)
    {
        if (mapProp.Value != null && mapProp.Value.Count > 0)
        {
            var firstKey = mapProp.Value.Keys.First();
            var firstValue = mapProp.Value[0];
            var keyNodes = BuildNodes(firstKey, asset);
            var valueNodes = BuildNodes(firstValue, asset);
            return BuildNodeList("MapProperty", 2, keyNodes.Concat(valueNodes).ToList(), asset);
        }

        if (asset.Mappings != null && asset.Mappings.TryGetPropertyData(mapProp.Name, mapProp.Ancestry, asset, out UsmapMapData mapData))
        {
            var keyNodes = BuildNodesFromUsmapPropertyData(mapData.InnerType, asset);
            var valueNodes = BuildNodesFromUsmapPropertyData(mapData.ValueType, asset);
            return BuildNodeList("MapProperty", 2, keyNodes.Concat(valueNodes).ToList(), asset);
        }

        var keyType = mapProp.KeyType;
        var valueType = mapProp.ValueType;
        if (keyType == null || valueType == null)
        {
            var keyNodesFallback1 = BuildStructNodes("StructProperty", null, mapProp, asset);
            var valueNodesFallback1 = BuildStructNodes("StructProperty", null, mapProp, asset);
            return BuildNodeList("MapProperty", 2, keyNodesFallback1.Concat(valueNodesFallback1).ToList(), asset);
        }

        var keyNodesFallback = BuildSimpleNodes(keyType.Value?.Value ?? "None", asset);
        var valueNodesFallback = BuildSimpleNodes(valueType.Value?.Value ?? "None", asset);
        return BuildNodeList("MapProperty", 2, keyNodesFallback.Concat(valueNodesFallback).ToList(), asset);
    }

    private static List<FPropertyTypeNameNode> BuildStructNodes(string rootType, FName structType, PropertyData property, UAsset asset)
    {
        if ((structType == null || structType.Value?.Value == "Generic") && asset.Mappings != null)
        {
            if (asset.Mappings.TryGetPropertyData(property.Name, property.Ancestry, asset, out UsmapStructData structData))
            {
                structType = new FName(asset, structData.StructType);
            }
        }

        var structName = EnsureName(asset, structType, "Generic");
        var childNodes = new List<FPropertyTypeNameNode> { CreateNode(structName, 0) };
        return BuildNodeList(rootType, 1, childNodes, asset);
    }

    private static List<FPropertyTypeNameNode> BuildEnumNodes(EnumPropertyData enumProp, UAsset asset)
    {
        var enumType = enumProp.EnumType;
        if (enumType == null && asset.Mappings != null)
        {
            if (asset.Mappings.TryGetPropertyData(enumProp.Name, enumProp.Ancestry, asset, out UsmapEnumData enumData))
            {
                enumType = new FName(asset, enumData.Name);
            }
        }

        var enumName = EnsureName(asset, enumType, "None");
        var childNodes = new List<FPropertyTypeNameNode> { CreateNode(enumName, 0) };
        return BuildNodeList("EnumProperty", 1, childNodes, asset);
    }

    private static List<FPropertyTypeNameNode> BuildByteNodes(BytePropertyData byteProp, UAsset asset)
    {
        var enumType = byteProp.EnumType;
        if (byteProp.ByteType == BytePropertyType.Byte)
        {
            enumType ??= new FName(asset, "None");
        }
        else if (enumType == null && asset.Mappings != null)
        {
            if (asset.Mappings.TryGetPropertyData(byteProp.Name, byteProp.Ancestry, asset, out UsmapEnumData enumData))
            {
                enumType = new FName(asset, enumData.Name);
            }
        }

        var enumName = EnsureName(asset, enumType, "None");
        var childNodes = new List<FPropertyTypeNameNode> { CreateNode(enumName, 0) };
        return BuildNodeList("ByteProperty", 1, childNodes, asset);
    }

    private static List<FPropertyTypeNameNode> BuildNodesFromUsmapPropertyData(UsmapPropertyData data, UAsset asset)
    {
        if (data == null) return BuildSimpleNodes("None", asset);

        switch (data)
        {
            case UsmapMapData mapData:
                var keyNodes = BuildNodesFromUsmapPropertyData(mapData.InnerType, asset);
                var valueNodes = BuildNodesFromUsmapPropertyData(mapData.ValueType, asset);
                return BuildNodeList("MapProperty", 2, keyNodes.Concat(valueNodes).ToList(), asset);
            case UsmapArrayData arrData:
                var rootType = data.Type.ToString();
                var innerNodes = BuildNodesFromUsmapPropertyData(arrData.InnerType, asset);
                return BuildNodeList(rootType, 1, innerNodes, asset);
            case UsmapStructData structData:
                var structName = new FName(asset, structData.StructType);
                return BuildNodeList("StructProperty", 1, [CreateNode(structName, 0)], asset);
            case UsmapEnumData enumData:
                var enumName = new FName(asset, enumData.Name);
                return BuildNodeList("EnumProperty", 1, [CreateNode(enumName, 0)], asset);
            case UsmapPropertyData propData when propData.Type == EPropertyType.ByteProperty:
                var byteEnumName = new FName(asset, "None");
                return BuildNodeList("ByteProperty", 1, [CreateNode(byteEnumName, 0)], asset);
            default:
                return BuildSimpleNodes(data.Type.ToString(), asset);
        }
    }

    private static List<FPropertyTypeNameNode> BuildSimpleNodes(string rootType, UAsset asset)
    {
        return BuildNodeList(rootType, 0, null, asset);
    }

    private static List<FPropertyTypeNameNode> BuildNodeList(string rootType, int innerCount, List<FPropertyTypeNameNode> childNodes, UAsset asset)
    {
        var safeRootType = string.IsNullOrEmpty(rootType) ? "None" : rootType;
        var nodes = new List<FPropertyTypeNameNode>(1 + (childNodes?.Count ?? 0))
        {
            CreateNode(new FName(asset, safeRootType), innerCount)
        };

        if (childNodes != null) nodes.AddRange(childNodes);
        return nodes;
    }

    private static FPropertyTypeNameNode CreateNode(FName name, int innerCount)
    {
        return new FPropertyTypeNameNode
        {
            Name = name,
            InnerCount = innerCount
        };
    }

    private static FName EnsureName(UAsset asset, FName name, string fallback)
    {
        if (name == null) return new FName(asset, fallback);
        if (name.IsDummy) return name.Transfer(asset);
        if (name.Asset == null) return new FName(asset, fallback);
        if (name.Value?.Value == null) return new FName(asset, fallback);
        return name.Transfer(asset);
    }
}
