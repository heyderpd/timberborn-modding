using System;
using UnityEngine;
using Timberborn.ModManagerScene;
using HarmonyLib;

namespace Mods.OldGopher.Pipe
{
  public class HarmonyModStarter : IModStarter
  {
    public static bool Loaded { get; private set; }

    public static bool Failed => !Loaded;

    public void StartMod(IModEnvironment modEnvironment)
    {
      try
      {
        var harmony = new Harmony("Mods.OldGopher.Pipe");
        WaterServiceOriginal.GetOriginalMethods(harmony);
        harmony.PatchAll();
        Loaded = true;
        Debug.Log($"[OldGopher] Harmony loaded");
      } catch (Exception err)
      {
        Debug.Log($"[OldGopher] Harmony failed error={err}");
      }
    }
  }
}
