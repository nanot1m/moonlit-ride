using UnityEditor;
namespace MoonlitRide.Editor {
    public sealed class NatureImporter : AssetPostprocessor {
        bool IsNature => assetPath.StartsWith("Assets/MoonlitRide/Resources/Nature/");
        void OnPreprocessModel() {
            if(!IsNature && !assetPath.StartsWith("Assets/MoonlitRide/Resources/Bicycle/"))return;
            var model=(ModelImporter)assetImporter;model.isReadable=true;model.importAnimation=false;model.addCollider=false;
            model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        }
        void OnPreprocessTexture() {
            if(!IsNature)return;
            var texture=(TextureImporter)assetImporter;texture.maxTextureSize=1024;
            texture.alphaIsTransparency=assetPath.Contains("Leaves") || assetPath.Contains("Grass");
            texture.mipmapEnabled=true;texture.anisoLevel=4;
        }
    }
}
