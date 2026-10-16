using UnityEditor;
using UnityEngine;

// All character artwork is imported consistently, including on a fresh checkout.
public sealed class PixelArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/") || !assetPath.EndsWith(".png")) return;
        var importer = (TextureImporter)assetImporter;
        // Only supply defaults on first import; Inspector changes must survive reimport.
        if (!importer.importSettingsMissing) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 256;
    }
}
