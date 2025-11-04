using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;

/// Automates creation of the Mixamo hero showcase without referencing existing franchises.
public static class MixamoHeroBuilder
{
    const string heroFolder = "Assets/Art/Mixamo/Hero";
    const string animationFolder = "Assets/Art/Mixamo/Hero/Animations";
    const string vfxFolder = "Assets/Art/Mixamo/Hero/VFX";
    const string postFolder = "Assets/Art/Mixamo/Hero/Post";
    const string timelineFolder = "Assets/Art/Mixamo/Hero/Timelines";
    const string settingsFolder = "Assets/Settings";
    const string controllerPath = animationFolder + "/Hero.controller";
    const string prefabPath = heroFolder + "/HeroShowcase.prefab";
    const string timelinePath = timelineFolder + "/HeroShowcase.playable";
    const string volumeProfilePath = postFolder + "/HeroVolumeProfile.asset";
    const string vfxPrefabPath = vfxFolder + "/HeroMagicVFX.prefab";
    const string urpAssetPath = settingsFolder + "/HeroURPAsset.asset";
    const string rendererAssetPath = settingsFolder + "/HeroForwardRenderer.asset";
    [MenuItem("Tools/Mixamo Hero/Run Full Setup")]
    static void RunFullSetup()
    {
        try
        {
            var urpAsset = EnsureUrpAsset();
            if (urpAsset == null)
            {
                EditorUtility.DisplayDialog("Mixamo Hero", "Nao foi possivel encontrar ou criar um Universal Render Pipeline Asset. Verifique se o pacote URP esta instalado.", "Ok");
                return;
            }

            ApplyUrpAsset(urpAsset);
            UpgradeProjectMaterials();
            BuildShowcase();
            PlaceHeroPrefabInScene();

            Debug.Log("Setup completo: URP ativo, materiais atualizados e prefab HeroShowcase recriado.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Falha ao executar o setup completo: " + ex.Message + "\n" + ex.StackTrace);
        }
    }


    static readonly string[] requiredFolders =
    {
        heroFolder,
        animationFolder,
        vfxFolder,
        postFolder,
        timelineFolder,
        settingsFolder
    };

    static readonly string[] requiredClipNames =
    {
        "Idle",
        "Walk",
        "Run",
        "Punch",
        "Magic"
    };

    static UniversalRenderPipelineAsset EnsureUrpAsset()
    {
        var existingGuid = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").FirstOrDefault();
        if (!string.IsNullOrEmpty(existingGuid))
        {
            var existingPath = AssetDatabase.GUIDToAssetPath(existingGuid);
            var existingAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(existingPath);
            if (existingAsset != null)
                return existingAsset;
        }

        EnsureFolder(settingsFolder);

        var assetFromPath = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
        if (assetFromPath != null)
            return assetFromPath;

        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererAssetPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "HeroForwardRenderer";
            AssetDatabase.CreateAsset(rendererData, rendererAssetPath);
        }

        var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        urpAsset.name = "HeroURPAsset";
        AssetDatabase.CreateAsset(urpAsset, urpAssetPath);

        var serializedAsset = new SerializedObject(urpAsset);
        var rendererList = serializedAsset.FindProperty("m_RendererDataList");
        if (rendererList != null)
        {
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
        }

        var defaultRendererIndex = serializedAsset.FindProperty("m_DefaultRendererIndex");
        if (defaultRendererIndex != null)
            defaultRendererIndex.intValue = 0;

        serializedAsset.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        return urpAsset;
    }

    static void ApplyUrpAsset(UniversalRenderPipelineAsset asset)
    {
        if (asset == null)
            return;

        GraphicsSettings.defaultRenderPipeline = asset;

        var currentQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(currentQuality, false);

        AssetDatabase.SaveAssets();
    }

    static void UpgradeProjectMaterials()
    {
        EditorApplication.ExecuteMenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Materials to URP");
        EditorApplication.ExecuteMenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Selected Materials to URP");
    }

    [MenuItem("Tools/Mixamo Hero/Build Showcase")] 
    static void BuildShowcase()
    {
        try
        {
            EnsureFolders();
            var model = FindModel();
            if (model == null)
            {
                EditorUtility.DisplayDialog("Mixamo Hero", "Nenhum FBX Mixamo encontrado. Coloque o arquivo em Assets/Art/Mixamo/Hero e inclua as anima\u00e7\u00f5es Idle/Walk/Run/Punch/Magic.", "Ok");
                return;
            }

            var allClips = LoadAnimationClips(model);
            var resolvedClips = ResolveRequiredClips(allClips, out var missingMessage);
            if (resolvedClips == null)
            {
                EditorUtility.DisplayDialog("Mixamo Hero", missingMessage, "Ok");
                return;
            }

            var controller = BuildAnimatorController(resolvedClips);
            var vfxPrefab = BuildVfxPrefab();
            var volumeProfile = BuildVolumeProfile();
            var timeline = BuildTimelineAsset();

            CreatePrefab(model, controller, vfxPrefab, volumeProfile, timeline, resolvedClips);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Mixamo hero showcase criada. Abra o prefab HeroShowcase para revisar e ajustar.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Falha ao montar o showcase: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    static void EnsureFolders()
    {
        foreach (var folder in requiredFolders)
        {
            EnsureFolder(folder);
        }
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        var parent = Path.GetDirectoryName(folder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent.Replace('\\', '/'));

        var normalizedParent = string.IsNullOrEmpty(parent) ? "" : parent.Replace('\\', '/');
        var folderName = Path.GetFileName(folder);
        if (string.IsNullOrEmpty(normalizedParent))
            throw new InvalidOperationException("Pasta raiz ausente: " + folder);
        AssetDatabase.CreateFolder(normalizedParent, folderName);
    }

    static GameObject FindModel()
    {
        var guids = AssetDatabase.FindAssets("t:Model", new[] { heroFolder });
        if (guids.Length == 0)
            return null;

        GameObject fallback = null;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
                continue;

            fallback ??= asset;
            if (asset.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0 || asset.GetComponentsInChildren<MeshRenderer>(true).Length > 0)
                return asset;
        }

        return fallback;
    }

    static List<AnimationClip> LoadAnimationClips(GameObject model)
    {
        var clips = new List<AnimationClip>();
        var seen = new HashSet<string>();

        void AddClip(AnimationClip clip)
        {
            if (clip == null)
                return;
            if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                return;
            var key = AssetDatabase.GetAssetPath(clip) + ":" + clip.name;
            if (seen.Add(key))
                clips.Add(clip);
        }

        if (model != null)
        {
            var path = AssetDatabase.GetAssetPath(model);
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                AddClip(clip);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { heroFolder }))
        {
            var clipPath = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            AddClip(clip);
        }

        return clips;
    }

    static Dictionary<string, AnimationClip> ResolveRequiredClips(IEnumerable<AnimationClip> clips, out string message)
    {
        var result = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();
        var clipList = clips.ToList();

        foreach (var required in requiredClipNames)
        {
            var clip = FindClip(clipList, required);
            if (clip == null)
            {
                missing.Add(required);
                continue;
            }

            result[required] = clip;
        }

        if (missing.Count > 0)
        {
            var available = clipList.Count == 0 ? "nenhum clip localizado" : string.Join(", ", clipList.Select(c => c.name));
            message = "Nao encontrei as animacoes " + string.Join(", ", missing) + ". Clips disponiveis: " + available + ".";
            return null;
        }

        message = string.Empty;
        return result;
    }

    static AnimationClip FindClip(IEnumerable<AnimationClip> clips, string requiredName)
    {
        var normalizedRequired = NormalizeName(requiredName);
        AnimationClip fallback = null;

        foreach (var clip in clips)
        {
            var normalizedClip = NormalizeName(clip.name);
            if (normalizedClip == normalizedRequired)
                return clip;

            if (fallback == null && normalizedClip.Contains(normalizedRequired))
                fallback = clip;
        }

        return fallback;
    }

    static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    static AnimationClip GetClip(Dictionary<string, AnimationClip> clips, string key)
    {
        return clips.FirstOrDefault(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    static AnimatorController BuildAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("PunchTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("UltimateTrigger", AnimatorControllerParameterType.Trigger);

        var layer = controller.layers[0];
        var sm = layer.stateMachine;
        foreach (var child in sm.states.ToArray())
            sm.RemoveState(child.state);

        var blendTree = new BlendTree
        {
            name = "LocomotionTree",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(blendTree, controller);

        var idle = GetClip(clips, "Idle");
        var walk = GetClip(clips, "Walk");
        var run = GetClip(clips, "Run");
        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(walk, 0.5f);
        blendTree.AddChild(run, 1f);

        var locomotion = sm.AddState("Locomotion");
        locomotion.motion = blendTree;

        var punchState = sm.AddState("Punch");
        punchState.motion = GetClip(clips, "Punch");
        var ultimateState = sm.AddState("Ultimate");
        ultimateState.motion = GetClip(clips, "Magic");

        var punchTransition = sm.AddAnyStateTransition(punchState);
        ConfigureTriggerTransition(punchTransition, "PunchTrigger");
        var ultimateTransition = sm.AddAnyStateTransition(ultimateState);
        ConfigureTriggerTransition(ultimateTransition, "UltimateTrigger");

        var punchToLocomotion = punchState.AddTransition(locomotion);
        ConfigureReturnTransition(punchToLocomotion);
        var ultimateToLocomotion = ultimateState.AddTransition(locomotion);
        ConfigureReturnTransition(ultimateToLocomotion);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void ConfigureTriggerTransition(AnimatorStateTransition transition, string parameter)
    {
        transition.duration = 0.05f;
        transition.hasExitTime = false;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    static void ConfigureReturnTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = true;
        transition.exitTime = 0.95f;
        transition.duration = 0.1f;
    }

    static GameObject BuildVfxPrefab()
    {
        if (File.Exists(vfxPrefabPath))
            return AssetDatabase.LoadAssetAtPath<GameObject>(vfxPrefabPath);

        var go = new GameObject("HeroMagicVFX");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 2f;
        main.startLifetime = 0.6f;
        main.startSpeed = 2f;
        main.startSize = 0.3f;
        main.loop = false;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var burst = new ParticleSystem.Burst(0f, 32);
        emission.SetBursts(new[] { burst });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.1f;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, vfxPrefabPath);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static VolumeProfile BuildVolumeProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumeProfilePath);
        if (profile != null)
            return profile;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, volumeProfilePath);

        profile.Add<Bloom>().SetAllOverridesTo(true);
        profile.TryGet(out Bloom bloom);
        if (bloom != null)
        {
            bloom.intensity.value = 5f;
            bloom.threshold.value = 1.2f;
        }

        profile.Add<ColorAdjustments>().SetAllOverridesTo(true);
        profile.TryGet(out ColorAdjustments color);
        if (color != null)
        {
            color.postExposure.value = 0.5f;
            color.saturation.value = 10f;
        }

        EditorUtility.SetDirty(profile);
        return profile;
    }

    static TimelineAsset BuildTimelineAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
        if (asset != null)
            AssetDatabase.DeleteAsset(timelinePath);

        asset = ScriptableObject.CreateInstance<TimelineAsset>();
        asset.durationMode = TimelineAsset.DurationMode.FixedLength;
        asset.fixedDuration = 2.0f;
        AssetDatabase.CreateAsset(asset, timelinePath);
        return asset;
    }

    static void CreatePrefab(GameObject model, RuntimeAnimatorController controller, GameObject vfxPrefab, VolumeProfile volumeProfile, TimelineAsset timeline, Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);

        var root = new GameObject("HeroShowcaseRoot");

        var cameraGo = new GameObject("ShowcaseCamera");
        cameraGo.transform.SetParent(root.transform);
        var camera = cameraGo.AddComponent<Camera>();
        cameraGo.AddComponent<CinemachineBrain>();
        cameraGo.AddComponent<UniversalAdditionalCameraData>();

        var vcamGo = new GameObject("HeroCloseUp");
        vcamGo.transform.SetParent(root.transform);
        var vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
        vcam.m_Lens.FieldOfView = 40f;
    vcam.Priority = 20;

        var volumeGo = new GameObject("HeroPostVolume");
        volumeGo.transform.SetParent(root.transform);
        var volume = volumeGo.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.profile = volumeProfile;

        var heroInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        heroInstance.name = "MixamoHero";
        heroInstance.transform.SetParent(root.transform);
        heroInstance.transform.localPosition = Vector3.zero;

        var animator = heroInstance.GetComponent<Animator>();
        if (animator == null)
            animator = heroInstance.AddComponent<Animator>();
        animator.applyRootMotion = true;
        animator.runtimeAnimatorController = controller;

        var ik = heroInstance.GetComponent<HeroCombatIK>();
        if (ik == null)
            ik = heroInstance.AddComponent<HeroCombatIK>();

        var ikTarget = new GameObject("IK_RightHand_Target").transform;
        ikTarget.SetParent(heroInstance.transform, false);
        ikTarget.localPosition = new Vector3(0.35f, 1.35f, 0.25f);
        ikTarget.localRotation = Quaternion.Euler(0f, 90f, 0f);
        ik.SetTarget(ikTarget);

        vcam.Follow = heroInstance.transform;
        vcam.LookAt = heroInstance.transform;
        vcamGo.transform.SetPositionAndRotation(heroInstance.transform.position + new Vector3(0.8f, 1.4f, -1.6f), Quaternion.LookRotation(heroInstance.transform.position + Vector3.up * 1.2f - (heroInstance.transform.position + new Vector3(0.8f, 1.4f, -1.6f))));

        var vfxInstance = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
        vfxInstance.transform.SetParent(heroInstance.transform, false);
        vfxInstance.transform.localPosition = new Vector3(0f, 1.1f, 0.3f);
        vfxInstance.SetActive(false);

        var director = root.AddComponent<PlayableDirector>();
        director.playableAsset = timeline;

        ConfigureTimeline(timeline, director, animator, vcam, vfxInstance, clips);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    static void ConfigureTimeline(TimelineAsset timeline, PlayableDirector director, Animator animator, CinemachineVirtualCamera vcam, GameObject vfxInstance, Dictionary<string, AnimationClip> clips)
    {
        foreach (var track in timeline.GetRootTracks().ToArray())
            timeline.DeleteTrack(track);

        var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Hero Animation");
        var animationClip = animationTrack.CreateClip<AnimationPlayableAsset>();
        animationClip.duration = 2.0;
        var playableAsset = (AnimationPlayableAsset)animationClip.asset;
        playableAsset.clip = GetClip(clips, "Magic");
        playableAsset.removeStartOffset = true;
        director.SetGenericBinding(animationTrack, animator);

        var cameraActivationTrack = timeline.CreateTrack<ActivationTrack>(null, "Camera Activation");
        var cameraClip = cameraActivationTrack.CreateDefaultClip();
        cameraClip.start = 0.0;
        cameraClip.duration = timeline.fixedDuration;
        director.SetGenericBinding(cameraActivationTrack, vcam.gameObject);

        var vfxActivationTrack = timeline.CreateTrack<ActivationTrack>(null, "VFX");
        var activationClip = vfxActivationTrack.CreateDefaultClip();
        activationClip.start = 1.0;
        activationClip.duration = 1.0;
        director.SetGenericBinding(vfxActivationTrack, vfxInstance);

        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
    }

    [MenuItem("Tools/Mixamo Hero/Open Optimization Checklist")]
    static void OpenProfiler()
    {
        EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
        Debug.Log("Abra o Profiler e, na aba Rendering, confira Batches, Overdraw e Post-process Layer para avaliar o custo.");
    }

    static void PlaceHeroPrefabInScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Nao foi possivel localizar o prefab HeroShowcase em " + prefabPath);
            return;
        }

        var existing = GameObject.Find(prefab.name);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log("HeroShowcase ja estava presente na cena. Selecionando o objeto existente.");
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            Debug.LogWarning("Falha ao instanciar o prefab HeroShowcase na cena atual.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(instance, "Adicionar HeroShowcase");
        instance.transform.position = Vector3.zero;
        Selection.activeGameObject = instance;
        var scene = instance.scene;
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("HeroShowcase adicionado na cena atual. Ajuste posicao/escala conforme necessario.");
    }
}