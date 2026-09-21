using System;
using System.IO;
using RagnaRock.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RagnaRock.Editor
{
    public static class BuildTools
    {
        public const string ScenePath = "Assets/RagnaRock/Scenes/RagnaRock.unity";
        [MenuItem("RagnaRock/Abrir jogo", priority = 1)]
        public static void OpenGame()
        {
            if (EditorApplication.isPlaying) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("RagnaRock/Validar projeto", priority = 10)]
        public static void ValidateProject()
        {
            if (EditorApplication.isCompiling) throw new BuildFailedException("Aguarde a compilação do Editor.");
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RagnaRock/Resources/Campaign.json");
            if (asset == null) throw new BuildFailedException("Campanha ausente.");
            var campaign = JsonUtility.FromJson<CampaignDefinition>(asset.text);
            if (campaign == null) throw new BuildFailedException("Campanha não pôde ser desserializada.");
            campaign.Validate();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new BuildFailedException("Cena inicial ausente.");
            foreach (string shaderName in new [] { "StageLit", "SonicFX" })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/RagnaRock/Resources/"+shaderName+".shader");
                if (shader == null || ShaderUtil.ShaderHasError(shader))
                    throw new BuildFailedException("Shader ausente ou com erro: " + shaderName);
            }
            if (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") == null)
                throw new BuildFailedException("Fonte interna do Unity indisponível.");
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                throw new BuildFailedException("Este projeto usa o Built-in Render Pipeline; remova a SRP nas configurações gráficas.");
            Debug.Log("RagnaRock: recursos presentes e campanha válida ("+campaign.TotalWaves+" ondas). Execute também EditMode/PlayMode e o checklist de release.");
        }
        [MenuItem("RagnaRock/Build Windows x64", priority = 20)]
        public static void BuildWindows()
        {
            ValidateProject();
            if (EditorApplication.isPlaying) throw new BuildFailedException("Saia do Play Mode antes de compilar.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new BuildFailedException("Instale Windows Build Support neste Editor pelo Unity Hub.");
            PlayerSettings.companyName = "Dingous";
            PlayerSettings.productName = "RagnaRock";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.dingous.ragnarock");
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
            PlayerSettings.resizableWindow = true;
            EditorBuildSettings.scenes = new [] { new EditorBuildSettingsScene(ScenePath, true) };
            const string destination = "Builds/Windows/RagnaRock.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new [] { ScenePath }, locationPathName = destination,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Build não concluído: " + report.summary.result + "; erros: " + report.summary.totalErrors);
            Debug.Log("Build Windows criado em " + Path.GetFullPath(destination) + ". Isso não substitui a homologação de release.");
        }
    }
}
