
using System;
using UnityEngine;
namespace CleanRPG.Systems
{
    public class GachaSystem
    {
        [Serializable] public class Banner{ public string id, displayName; public Rates baseRates; public int softPityStart, hardPity; public string softPityCurve; public string[] featuredPool, standardPoolS, standardPoolA, standardPoolB; }
        [Serializable] public class Rates{ public double S,A,B; }
        private Banner banner; private System.Random rng = new System.Random(); private int pullsSinceS=0;
        public GachaSystem(string bannerId){ var ta = Resources.Load<TextAsset>("Gacha/"+bannerId); if (ta) banner = JsonUtility.FromJson<Banner>(ta.text);
            if (banner==null) banner = new Banner{ id="default", displayName="Default", baseRates=new Rates{S=0.006,A=0.08,B=0.914}, softPityStart=75, hardPity=90,
                featuredPool=new string[0], standardPoolS=new string[0], standardPoolA=new string[0], standardPoolB=new string[0]}; }
        public string Pull()
        {
            pullsSinceS++;
            double pS = banner.baseRates.S;
            if (pullsSinceS >= banner.softPityStart) pS += 0.02 * (pullsSinceS - banner.softPityStart);
            if (pullsSinceS >= banner.hardPity) pS = 1.0;
            double roll = rng.NextDouble();
            if (roll < pS){ pullsSinceS=0; if (banner.featuredPool.Length>0 && rng.NextDouble()<0.5) return banner.featuredPool[rng.Next(banner.featuredPool.Length)];
                if (banner.standardPoolS.Length>0) return banner.standardPoolS[rng.Next(banner.standardPoolS.Length)]; return "S_unit"; }
            else if (roll < pS + banner.baseRates.A){ if (banner.standardPoolA.Length>0) return banner.standardPoolA[rng.Next(banner.standardPoolA.Length)]; return "A_unit"; }
            else { if (banner.standardPoolB.Length>0) return banner.standardPoolB[rng.Next(banner.standardPoolB.Length)]; return "B_unit"; }
        }
    }
}
