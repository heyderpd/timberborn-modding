using System;
using UnityEngine;
using Timberborn.ModManagerScene;
using HarmonyLib;

namespace Mods.OldGopher.Pipe
{
  public class HarmonyModStarter : IModStarter
  {
    public static bool Loaded { get; private set; }

    public void StartMod(IModEnvironment modEnvironment)
    {
      try
      {
        var harmony = new Harmony("EXIST.ERROR.NOT");
        harmony.PatchAll();
        Loaded = true;
        Debug.Log($"[OldGopher] Harmony loaded");
      }
      catch (Exception err)
      {
        Debug.Log($"[OldGopher] Harmony failed error={err}");
      }
    }
  }
}
