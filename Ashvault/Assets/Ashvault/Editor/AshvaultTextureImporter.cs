#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ashvault.EditorTools
{
    // Ensure all generated low-res artwork remains crisp after Unity imports it.
    public sealed class AshvaultTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Ashvault/Resources/Textures/")) return;
            var settings = (TextureImporter)assetImporter;
            settings.textureType = TextureImporterType.Default;
            settings.isReadable = true; // Sprite.Create at runtime.
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.alphaIsTransparency = true;
            settings.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
#endif
