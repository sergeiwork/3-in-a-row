#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThreeInARow.Editor
{
    [InitializeOnLoad]
    internal static class UrpMigrationBootstrap
    {
        private const string PipelineAssetPath = "Assets/Game/Rendering/ThreeInARowURP.asset";
        private const string RenderingFolder = "Assets/Game/Rendering";

        static UrpMigrationBootstrap()
        {
            EditorApplication.delayCall += Configure;
        }

        private static void Configure()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                if (!Directory.Exists(RenderingFolder))
                {
                    Directory.CreateDirectory(RenderingFolder);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                var createRenderer = typeof(UniversalRenderPipelineAsset).GetMethod(
                    "CreateRendererAsset",
                    BindingFlags.Static | BindingFlags.NonPublic);

                var renderer = (ScriptableRendererData)createRenderer.Invoke(
                    null,
                    new object[] { PipelineAssetPath, RendererType.UniversalRenderer, true, "Renderer" });

                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                AssetDatabase.SaveAssets();
                Debug.Log("Configured the project to use the Universal Render Pipeline.");
            }
        }
    }
}
#endif
