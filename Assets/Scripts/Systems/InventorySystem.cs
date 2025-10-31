
using System.Collections.Generic;
namespace CleanRPG.Systems
{
    public static class InventorySystem
    {
        private static HashSet<string> owned = new HashSet<string>();
        public static void InitAllOwned(string[] ids){ owned = new HashSet<string>(ids); }
        public static bool IsOwned(string id) => owned.Contains(id);
        public static void Unlock(string id){ owned.Add(id); }
        public static string[] OwnedList(){ var arr = new string[owned.Count]; owned.CopyTo(arr); return arr; }
    }
}
