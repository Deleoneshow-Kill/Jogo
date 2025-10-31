using UnityEngine;
using CleanRPG.Systems;

namespace CleanRPG.Battle
{
    public partial class BattleBootstrap3D : MonoBehaviour
    {
        void InitializeComboHooks()
        {
            CleanRPG.Systems.SceneEnsure.EnsureEssentials();
        }
    }
}
