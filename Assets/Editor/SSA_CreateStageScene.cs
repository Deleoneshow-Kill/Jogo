using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class SSA_CreateStageScene
{
    [MenuItem("SSA/Setup/4) Criar Cena SSA_TestStage")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var key = new GameObject("Directional Light Key");
        var ldKey = key.AddComponent<Light>();
        ldKey.type = LightType.Directional;
        ldKey.intensity = 1.1f;
        key.transform.rotation = Quaternion.Euler(50, -30, 0);

        var rim = new GameObject("Directional Light Rim");
        var ldRim = rim.AddComponent<Light>();
        ldRim.type = LightType.Directional;
        ldRim.intensity = 0.4f;
        ldRim.color = new Color(0.8f, 0.9f, 1f);
        rim.transform.rotation = Quaternion.Euler(20, 150, 0);

        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("SkyboxProcedural.mat");
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.5f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.32f, 0.35f);

        var cam = Camera.main;
        if (cam)
        {
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
        }

        SSA_CreateURPPostProcessing.CreatePost();

        string path = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, path + "/SSA_TestStage.unity");
        Debug.Log("[SSA] Cena SSA_TestStage criada.");
    }
}
