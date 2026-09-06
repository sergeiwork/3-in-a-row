using UnityEditor;
using UnityEngine;

namespace ThreeInARow.Editor
{
    /// <summary>
    /// Keeps sourced E0 artwork ready for Unity UI without hand-editing every importer.
    /// </summary>
    public sealed class E0AssetImportSettings : AssetPostprocessor
    {
        private const string E0Root = "Assets/Game/Presentation/Art/E0/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(E0Root, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = assetPath.Contains("/Enemies/") ? 2048 : 512;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(E0Root, System.StringComparison.Ordinal)) return;

            var importer = (AudioImporter)assetImporter;
            var isMusic = assetPath.Contains("/Audio/Music/");
            importer.forceToMono = !isMusic;
            importer.loadInBackground = isMusic;

            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = isMusic ? 0.62f : 0.78f;
            settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
        }
    }
}
