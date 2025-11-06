using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_RENDER_PIPELINE_URP
namespace JogoSSA.Editor
{
    public static class SSASampleStageCreator
    {
        private const string MenuPath = "Jogo/Create SSA Sample Stage";
        private const string StageMatFolder = "Assets/JogoSSA/Materials/Stage";

        private const string FloorMatPath = StageMatFolder + "/M_Stage_Floor.mat";
        private const string PadMatPath = StageMatFolder + "/M_Stage_Pad.mat";
        private const string EdgeMatPath = StageMatFolder + "/M_Stage_Edge.mat";
        private const string SkyMatPath = StageMatFolder + "/M_Stage_Sky.mat";
        private const string PillarMatPath = StageMatFolder + "/M_Stage_Pillar.mat";
        private const string LightMatPath = StageMatFolder + "/M_Stage_Lightband.mat";

        [MenuItem(MenuPath, priority = 70)]
        private static void CreateStageFromMenu()
        {
            CreateStage(interactive: true);
        }

        public static GameObject CreateStage(bool interactive = false)
        {
            EnsureMaterials();

            var stageRoot = new GameObject("SSA_StageSample");
            Undo.RegisterCreatedObjectUndo(stageRoot, "Create SSA Stage Sample");
            stageRoot.transform.position = Vector3.zero;

            var floor = CreateQuad("Floor", new Vector3(0f, 0f, 0f), new Vector3(16f, 12f, 1f), Quaternion.Euler(90f, 0f, 0f), LoadMaterial(FloorMatPath), stageRoot.transform);
            var pad = CreateCylinder("BattlePad", new Vector3(0f, 0.01f, 0f), new Vector3(6.5f, 0.15f, 6.5f), LoadMaterial(PadMatPath), stageRoot.transform);
            var edge = CreateTorus("PadEdge", new Vector3(0f, 0.2f, 0f), 6.5f, 0.18f, 32, LoadMaterial(EdgeMatPath), stageRoot.transform);

            // Light bands
            CreateStrip(stageRoot.transform, "LightStrip_L", new Vector3(-6.5f, 0.25f, 0f), new Vector3(0.3f, 0.3f, 7.5f), LoadMaterial(LightMatPath));
            CreateStrip(stageRoot.transform, "LightStrip_R", new Vector3(6.5f, 0.25f, 0f), new Vector3(0.3f, 0.3f, 7.5f), LoadMaterial(LightMatPath));

            // Pillars
            CreatePillar(stageRoot.transform, new Vector3(-6.5f, 3.4f, -4f), LoadMaterial(PillarMatPath));
            CreatePillar(stageRoot.transform, new Vector3(6.5f, 3.4f, -4f), LoadMaterial(PillarMatPath));
            CreatePillar(stageRoot.transform, new Vector3(-6.5f, 3.4f, 4f), LoadMaterial(PillarMatPath));
            CreatePillar(stageRoot.transform, new Vector3(6.5f, 3.4f, 4f), LoadMaterial(PillarMatPath));

            // Background arch and sky
            var arch = CreateCylinder("BackgroundArch", new Vector3(0f, 3.6f, -7.5f), new Vector3(10f, 0.6f, 3.5f), LoadMaterial(PillarMatPath), stageRoot.transform);
            arch.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            var sky = CreateQuad("SkyBackdrop", new Vector3(0f, 5.5f, -11f), new Vector3(22f, 10f, 1f), Quaternion.identity, LoadMaterial(SkyMatPath), stageRoot.transform);

            // Side walls
            CreateWall(stageRoot.transform, new Vector3(-9f, 2.5f, 0f), new Vector3(0.3f, 5f, 16f), LoadMaterial(PillarMatPath));
            CreateWall(stageRoot.transform, new Vector3(9f, 2.5f, 0f), new Vector3(0.3f, 5f, 16f), LoadMaterial(PillarMatPath));

            if (interactive)
            {
                Selection.activeGameObject = stageRoot;
            }

            return stageRoot;
        }

        private static void EnsureMaterials()
        {
            Directory.CreateDirectory(StageMatFolder);

            CreateMaterialIfMissing(FloorMatPath, new Color(0.18f, 0.18f, 0.24f), smoothness: 0.6f);
            CreateMaterialIfMissing(PadMatPath, new Color(0.32f, 0.46f, 0.95f), smoothness: 0.7f, emission: new Color(0.22f, 0.45f, 1.0f) * 0.15f);
            CreateMaterialIfMissing(EdgeMatPath, new Color(0.06f, 0.08f, 0.2f), smoothness: 0.9f, emission: new Color(0.15f, 0.35f, 1.2f) * 0.4f);
            CreateMaterialIfMissing(SkyMatPath, new Color(0.35f, 0.52f, 0.92f), smoothness: 0.1f);
            CreateMaterialIfMissing(PillarMatPath, new Color(0.83f, 0.82f, 0.78f), metallic: 0.05f, smoothness: 0.4f);
            CreateMaterialIfMissing(LightMatPath, new Color(0.12f, 0.28f, 0.85f), smoothness: 0.8f, emission: new Color(0.28f, 0.55f, 1.6f) * 1.2f);
        }

        private static void CreateMaterialIfMissing(string path, Color baseColor, float metallic = 0f, float smoothness = 0.5f, Color? emission = null)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                return;
            }

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = Path.GetFileNameWithoutExtension(path);
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            AssetDatabase.CreateAsset(mat, path);
        }

        private static Material LoadMaterial(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static GameObject CreateQuad(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(go, "Create SSA Stage Part");
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = rotation;
            AssignMaterial(go, material);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(go, "Create SSA Stage Part");
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            AssignMaterial(go, material);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject CreateStrip(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var strip = CreateQuad(name, position, scale, Quaternion.Euler(90f, 0f, 0f), material, parent);
            return strip;
        }

        private static GameObject CreateWall(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(wall, "Create SSA Stage Wall");
            wall.name = "StageWall";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            AssignMaterial(wall, material);
            Object.DestroyImmediate(wall.GetComponent<Collider>());
            return wall;
        }

        private static GameObject CreateTorus(string name, Vector3 position, float radius, float thickness, int segments, Material material, Transform parent)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create SSA Stage Torus");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            meshFilter.sharedMesh = GenerateTorusMesh(radius, thickness, segments);
            return go;
        }

        private static Mesh GenerateTorusMesh(float radius, float thickness, int segments)
        {
            const int tubeSegments = 12;
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int seg = 0; seg <= segments; seg++)
            {
                float segmentFraction = seg / (float)segments;
                float segmentAngle = segmentFraction * Mathf.PI * 2f;
                Matrix4x4 ringMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, Mathf.Rad2Deg * segmentAngle, 0f), Vector3.one);

                for (int tube = 0; tube <= tubeSegments; tube++)
                {
                    float tubeFraction = tube / (float)tubeSegments;
                    float tubeAngle = tubeFraction * Mathf.PI * 2f;
                    Vector3 localPos = new Vector3(Mathf.Cos(tubeAngle) * thickness, Mathf.Sin(tubeAngle) * thickness, 0f);
                    Vector3 offset = new Vector3(radius, 0f, 0f);
                    Vector3 ringPos = ringMatrix.MultiplyPoint3x4(localPos + offset);
                    Vector3 ringNormal = ringMatrix.MultiplyVector(localPos.normalized);

                    vertices.Add(ringPos);
                    normals.Add(ringNormal);
                    uvs.Add(new Vector2(segmentFraction, tubeFraction));
                }
            }

            int stride = tubeSegments + 1;
            for (int seg = 0; seg < segments; seg++)
            {
                for (int tube = 0; tube < tubeSegments; tube++)
                {
                    int current = seg * stride + tube;
                    int next = current + stride;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreatePillar(Transform parent, Vector3 position, Material material)
        {
            var baseObj = CreateCylinder("PillarBase", position + new Vector3(0f, -1.5f, 0f), new Vector3(0.8f, 0.4f, 0.8f), material, parent);
            var shaft = CreateCylinder("PillarShaft", position, new Vector3(0.4f, 3.2f, 0.4f), material, parent);
            var cap = CreateCylinder("PillarCap", position + new Vector3(0f, 3.3f, 0f), new Vector3(0.95f, 0.3f, 0.95f), material, parent);
        }

        private static void AssignMaterial(GameObject go, Material material)
        {
            if (material == null)
            {
                return;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
#endif
