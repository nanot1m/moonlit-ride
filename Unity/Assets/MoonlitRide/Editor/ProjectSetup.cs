using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MoonlitRide.Editor
{
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/MoonlitRide.unity";
        [MenuItem("Moonlit Ride/Prepare project")]
        public static void Prepare()
        {
            RiderAssetBuilder.Import();
            PlayerSettings.companyName = "Moonlit Ride"; PlayerSettings.productName = "Moonlit Ride";
            PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler"); if (input != null) input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo();
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            // Fog is enabled at runtime; retain its variants even when the saved bootstrap scene has no fog.
            graphics.FindProperty("m_FogStripping").intValue = 1;
            graphics.FindProperty("m_FogKeepLinear").boolValue = true;
            var included = graphics.FindProperty("m_AlwaysIncludedShaders");
            // GUI/Text Shader is an internal default-resource shader, not a build asset.
            // Unity includes the font shader automatically through TextMesh/IMGUI.
            for (int i = included.arraySize - 1; i >= 0; i--)
                if (included.GetArrayElementAtIndex(i).objectReferenceValue is Shader old && old.name == "GUI/Text Shader") { included.GetArrayElementAtIndex(i).objectReferenceValue = null; included.DeleteArrayElementAtIndex(i); }
            foreach (string name in new[] { "Standard", "MoonlitRide/Painted", "MoonlitRide/Water", "MoonlitRide/Glow", "MoonlitRide/Sky", "MoonlitRide/Fabric", "MoonlitRide/CoastalBloom", "MoonlitRide/Blouse", "MoonlitRide/Foliage", "MoonlitRide/CoastalGround" })
            {
                var shader = Shader.Find(name); if (!shader) throw new Exception("Missing shader: " + name);
                bool found = false; for (int i = 0; i < included.arraySize; i++) if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (!found) { int i = included.arraySize; included.InsertArrayElementAtIndex(i); included.GetArrayElementAtIndex(i).objectReferenceValue = shader; }
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Moonlit Ride", typeof(MoonlitGame));
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else EditorSceneManager.OpenScene(ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); Debug.Log("MOONLIT_PROJECT_READY");
        }
        [MenuItem("Moonlit Ride/Validate and build Web")]
        public static void BuildWebGL()
        {
            Prepare(); PortChecks.Run();
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // GitHub Pages cannot set Content-Encoding.
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.template = "PROJECT:Moonlit";
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Web build failed: " + report.summary.result);
            File.Copy("../THIRD_PARTY_ASSETS.md", "Builds/WebGL/THIRD_PARTY_ASSETS.md", true);
            File.WriteAllText("Builds/WebGL/.nojekyll", "");
            Debug.Log("MOONLIT_WEB_BUILD_PASSED " + report.summary.totalSize);
        }
        [MenuItem("Moonlit Ride/Validate and build Windows")]
        public static void BuildWindows()
        {
            Prepare(); PortChecks.Run();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/MoonlitRide.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            File.Copy("../THIRD_PARTY_ASSETS.md", "Builds/Windows/THIRD_PARTY_ASSETS.md", true);
            Debug.Log("MOONLIT_WINDOWS_BUILD_PASSED " + report.summary.totalSize);
        }
    }
}
