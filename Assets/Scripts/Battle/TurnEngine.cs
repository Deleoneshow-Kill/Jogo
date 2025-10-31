
using UnityEngine;
using System;
namespace CleanRPG.Battle {
  public class TurnEngine : MonoBehaviour {
    public int round=1; public int energy=3; public int maxEnergy=7;
    public event Action OnStateChanged;
    public void NextTurn(){ round++; energy=Mathf.Min(maxEnergy, energy+2); OnStateChanged?.Invoke(); }
    public bool TryUse(int cost){ if(energy<cost) return false; energy-=cost; OnStateChanged?.Invoke(); return true; }
  }
}
