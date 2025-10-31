
using UnityEngine;
using CleanRPG.Battle;
namespace CleanRPG.Bootstrapper
{
    public static class AutoSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<CleanRPG.Battle.BattleBootstrap3D>() == null)
            {
                var go = new GameObject("Game");
                go.AddComponent<CleanRPG.Battle.BattleBootstrap3D>();
            }
        }
    }
}
