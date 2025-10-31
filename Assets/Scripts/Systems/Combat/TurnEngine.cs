namespace CleanRPG.Systems{
public class TurnEngine{
    public int Round{get;private set;}=1; public int Energy{get;private set;}=2; public int EnergyPerRound=1;
    public bool TrySpend(int cost){ if(cost>Energy) return false; Energy-=cost; return true;}
    public void NextRound(){ Round++; Energy+=EnergyPerRound; if(Energy>10) Energy=10;}
}}
