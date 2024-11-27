using UnityEngine;
using Timberborn.ModManagerScene;
using HarmonyLib;

namespace Mods.OldGopher.Pipe.Scripts
{
  public class ModStarter : IModStarter
  {
    public void StartMod(IModEnvironment modEnvironment)
    {
      Debug.Log($"harmony path start");
      var harmony = new Harmony("Mods.OldGopher.Pipe.Scripts");
      harmony.PatchAll();
    }
  }
}
