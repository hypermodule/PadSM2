# PadSM2 (Pad Static Mesh for Grounded 2)

Small tool created to make a cooked UE5.6 static mesh asset compatible with the game Grounded 2
(version 0.3.0.2 at least).

The tool performs the following operations on the static mesh asset:

 * Inserts the extra `1i32` that the game expects in each FStaticMeshSection

 * Uses the game's custom schema for serializing

 * Replaces the asset's default `CollisionProfileName` with a `CollisionResponses` array

 * Changes the mesh asset to use versioned property serialization

## Requirements

PadSM2 requires you to have **.NET 8.0 or later installed**. If you don't already have it, you
can [download it here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)
(select the **Windows Desktop x64** version).

## Usage

```
PadSM2.exe /Path/To/StaticMesh.uasset
```

## Example

Follow these steps to replace the coin static mesh, which is located at:

```
Augusta/Content/Art/World/ZN00_Global/Coins/Coin_Quarter/SM_Coin_Quarter_A.uasset
```

1. Use Unreal Editor to create a blank UE5.6 project named `Augusta`.

2. In the content browser, create the directory structure of the target asset:

```
Art/World/ZN00_Global/Coins/Coin_Quarter/
```

3. Import your static mesh into the newly created `Coin_Quarter` directory and name
it `SM_Coin_Quarter_A`.

4. **IMPORTANT:** For the materials you will have to reference the game's built-in
materials, since the developers have also made engine changes to the material class. :-(
This means that you should place each of the materials that are generated on import
at the path of one of the game's built-in materials (e.g. I will place mine at
`Augusta/Content/_Augusta/Editor/Materials/MI_PlaceholderGOAP`).

5. Cook your mesh by going to clicking on Platforms > Windows > Cook Content.

6. Once the cooking is finished, find the cooked mesh files; they will be located at e.g.

```
C:\Users\user\Documents\Unreal Projects\Augusta\Saved\Cooked\Windows\Augusta\Content\Art\World\ZN00_Global\Coins\Coin_Quarter\SM_Coin_Quarter_A.{uasset,uexp,ubulk}
```

7. Copy the three asset files (uasset, uexp and ubulk) to a new directory somewhere, recreating the directory path starting from `Augusta`.
For example, I will copy mine to:

```
C:\Users\user\Desktop\MyMod_P\Augusta\Content\Art\World\ZN00_Global\Coins\Coin_Quarter\SM_Coin_Quarter_A.{uasset,uexp,ubulk}
```

8. Run PadSM2 on the uasset file:

```
PadSM2.exe C:\Users\user\Desktop\MyMod_P\Augusta\Content\Art\World\ZN00_Global\Coins\Coin_Quarter\SM_Coin_Quarter_A.uasset
```

(If it succeeds, it will create .bak backup files for the original uasset and uexp; the ubulk is untouched always.)

9. Package your mod with [retoc](https://github.com/trumank/retoc):

```
cd C:\Users\user\Desktop
retoc.exe to-zen MyMod_P MyMod_P.utoc --version UE5_6
```

This will produce `MyMod_P.{utoc,ucas,pak}`. Copy these 3 files to the game's Paks directory.

![example](/example.jpg)

## License

PadSM2 is licensed under the MIT license (see `LICENSE.txt`). It uses the third-party libraries
CUE4Parse and UAssetAPI (see `NOTICE.txt` for their licenses).
