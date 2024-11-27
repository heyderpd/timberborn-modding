using UnityEngine;
using Timberborn.ModManagerScene;
using HarmonyLib;

namespace Mods.OldGopher.Pipe
{
  public class ModStarter : IModStarter
  {
    public void StartMod(IModEnvironment modEnvironment)
    {
      Debug.Log($"harmony path start");
      var harmony = new Harmony("Mods.OldGopher.Pipe.Harmony");
      harmony.PatchAll();
    }
  }
}
