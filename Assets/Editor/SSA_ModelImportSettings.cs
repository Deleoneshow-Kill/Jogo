using UnityEditor;

public class SSA_ModelImportSettings : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        var imp = (ModelImporter)assetImporter;
        if (!assetPath.StartsWith("Assets/Characters") && !assetPath.StartsWith("Assets/_Auto/FBX"))
            return;

    imp.materialImportMode = ModelImporterMaterialImportMode.None;
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.optimizeGameObjects = true;
        imp.importCameras = false;
        imp.importLights = false;
        imp.animationCompression = ModelImporterAnimationCompression.Optimal;
    }
}
