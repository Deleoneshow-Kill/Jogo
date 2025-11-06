using UnityEngine;

public enum SSA_Team { Player, Enemy }

public class SSA_UnitStats : MonoBehaviour
{
    public string UnitName = "Unit";
    public SSA_Team Team = SSA_Team.Player;
    public int Speed = 100;
    public bool IsAlive = true;
    public Transform SelectionAnchor;

    [Header("Energia (ATB)")]
    public float Energy;
    public float EnergyFillPerSecond = 30f;
}
