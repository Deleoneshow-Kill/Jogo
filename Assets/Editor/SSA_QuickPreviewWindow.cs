using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SSA_QuickPreviewWindow : EditorWindow
{
    GameObject prefab;

    [MenuItem("SSA/Preview/Quick Preview")]
    static void Open() => GetWindow<SSA_QuickPreviewWindow>("SSA Quick Preview");

    void OnGUI()
    {
        GUILayout.Label("Prefab para pré-visualizar na cena SSA_TestStage", EditorStyles.boldLabel);
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        if (GUILayout.Button("Spawn & Frame na SSA_TestStage"))
        {
            if (!prefab) { Debug.LogError("Selecione um prefab."); return; }
            const string scenePath = "Assets/Scenes/SSA_TestStage.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError("Cena SSA_TestStage não encontrada. Rode 'SSA/Setup/4) Criar Cena SSA_TestStage'.");
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = prefab.name;
            go.transform.position = Vector3.zero;

            if (!GameObject.Find("SSA_Ground"))
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "SSA_Ground";
                ground.transform.localScale = new Vector3(3, 1, 3);
                ground.transform.position = new Vector3(0, -0.01f, 0);
            }

            FrameCameraTo(go);
            Selection.activeGameObject = go;
        }
    }

    static void FrameCameraTo(GameObject target)
    {
        var cam = Camera.main;
        if (!cam) return;

        var bounds = new Bounds(target.transform.position, Vector3.one);
        foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            bounds.Encapsulate(r.bounds);

        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float dist = size * 2.2f;
        var center = bounds.center;

        cam.transform.position = center + new Vector3(0, size * 0.5f, dist);
        cam.transform.LookAt(center, Vector3.up);
    }
}
