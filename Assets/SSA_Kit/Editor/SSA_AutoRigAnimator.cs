using UnityEditor; using UnityEditor.Animations; using UnityEngine; using System.Linq;
public static class SSA_AutoRigAnimator
{
    [MenuItem("SSA/Characters/1) Definir Rig Humanoid (pasta selecionada)")]
    public static void SetHumanoidRigOnFolder(){
        var obj=Selection.activeObject; if(!obj){ Debug.LogError("Selecione uma pasta com FBX."); return; }
        var path=AssetDatabase.GetAssetPath(obj); if(!AssetDatabase.IsValidFolder(path)){ Debug.LogError("Seleção não é pasta."); return; }
        var guids=AssetDatabase.FindAssets("t:Model", new[]{path}); int ok=0;
        foreach(var g in guids){ var p=AssetDatabase.GUIDToAssetPath(g); var mi=AssetImporter.GetAtPath(p) as ModelImporter; if(mi==null) continue;
            mi.animationType=ModelImporterAnimationType.Human; mi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; mi.optimizeGameObjects=true; mi.SaveAndReimport(); ok++; }
        Debug.Log($"[SSA] Rig Humanoid aplicado em {ok} modelos."); }

    [MenuItem("SSA/Characters/2) Criar Animator (Idle/Attack/Hit/Die) a partir da pasta")]
    public static void BuildAnimatorFromFolder(){
        var obj=Selection.activeObject; if(!obj){ Debug.LogError("Selecione uma pasta com animações (FBX)."); return; }
        var path=AssetDatabase.GetAssetPath(obj); if(!AssetDatabase.IsValidFolder(path)){ Debug.LogError("Seleção não é pasta."); return; }
        if(!AssetDatabase.IsValidFolder("Assets/SSA_Kit")) AssetDatabase.CreateFolder("Assets","SSA_Kit");
        if(!AssetDatabase.IsValidFolder("Assets/SSA_Kit/Animators")) AssetDatabase.CreateFolder("Assets/SSA_Kit","Animators");
        var controllerPath="Assets/SSA_Kit/Animators/SSA_Character.controller";
        var ctrl=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        var guids=AssetDatabase.FindAssets("t:AnimationClip", new[]{path});
        AnimationClip idle=null, attack=null, hit=null, die=null;
        foreach(var g in guids){ var p=AssetDatabase.GUIDToAssetPath(g); var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(p); if(!clip) continue;
            var n=clip.name.ToLowerInvariant();
            if(idle==null && (n.Contains("idle")||n.Contains("breath")||n.Contains("stance"))) idle=clip;
            if(attack==null && (n.Contains("attack")||n.Contains("slash")||n.Contains("punch"))) attack=clip;
            if(hit==null && (n.Contains("hit")||n.Contains("damage"))) hit=clip;
            if(die==null && (n.Contains("die")||n.Contains("death"))) die=clip; }
        var layer=ctrl.layers[0]; var sm=layer.stateMachine;
        var stIdle=sm.AddState("Idle"); stIdle.motion=idle; sm.defaultState=stIdle;
        var stAttack=sm.AddState("Attack"); stAttack.motion=attack;
        var stHit=sm.AddState("Hit"); stHit.motion=hit;
        var stDie=sm.AddState("Die"); stDie.motion=die;
        var tAtk=stIdle.AddTransition(stAttack); tAtk.AddCondition(AnimatorConditionMode.If,0,"Attack"); tAtk.hasExitTime=false; tAtk.duration=0.05f;
        var tAtkBack=stAttack.AddTransition(stIdle); tAtkBack.hasExitTime=true; tAtkBack.exitTime=0.9f; tAtkBack.duration=0.05f;
        var tHit=stIdle.AddTransition(stHit); tHit.AddCondition(AnimatorConditionMode.If,0,"Hit"); tHit.hasExitTime=false; tHit.duration=0.02f;
        var tHitBack=stHit.AddTransition(stIdle); tHitBack.hasExitTime=true; tHitBack.exitTime=0.95f; tHitBack.duration=0.02f;
        var tDie=stIdle.AddTransition(stDie); tDie.AddCondition(AnimatorConditionMode.If,0,"Die"); tDie.hasExitTime=false; tDie.duration=0.02f;
        AssetDatabase.SaveAssets(); Selection.activeObject=ctrl; Debug.Log("[SSA] Animator criado em Assets/SSA_Kit/Animators/SSA_Character.controller"); }
}