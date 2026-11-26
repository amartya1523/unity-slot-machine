using UnityEditor;
using UnityEngine;

namespace Underpin.SlotMachine.EditorTools
{
    /// <summary>
    /// Imports everything under Assets/UI/Sprites as UI sprites automatically, so nobody has
    /// to click through import settings. Pixel art (machine + symbols) uses Point filtering to
    /// stay crisp when scaled up; smooth art (popups, buttons) uses Bilinear.
    /// </summary>
    public sealed class SpriteImportProcessor : AssetPostprocessor
    {
        public const string SpriteRoot = "Assets/UI/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteRoot))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;

            bool isPixelArt = assetPath.Contains("/Machine/") || assetPath.Contains("/Symbols/");
            importer.filterMode = isPixelArt ? FilterMode.Point : FilterMode.Bilinear;
        }

        /// <summary>Re-imports any sprite that was imported before this processor existed.</summary>
        public static void EnsureSpritesImported()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteRoot.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                    importer.textureType != TextureImporterType.Sprite)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
