using System;
using UnityEngine;
using HarmonyLib;
using Timberborn.ModManagerScene;

namespace Mods.OldGopher.Pipe
{
  public class ModStarter : IModStarter
  {
    public static bool Loaded { get; private set; }

    public static bool Failed => !Loaded;

    public void StartMod(IModEnvironment modEnvironment)
    {
      Debug.Log($"[OldGopher] Harmony init");
      try
      {
        var harmony = new Harmony("Mods.OldGopher.Pipe");
        WaterServiceOriginal.GetOriginalMethods(harmony);
        try
        {
          harmony.PatchAll();
          Loaded = true;
          Debug.Log($"[OldGopher] Harmony loaded");
        }
        catch (Exception err)
        {
          Debug.Log($"[OldGopher] Harmony failed error={err}");
          harmony.UnpatchAll();
          Loaded = false;
        }
      }
      catch (Exception err)
      {
        Debug.Log($"[OldGopher] Harmony fatal error={err}");
      }
      Debug.Log($"[OldGopher] Harmony end");
    }
  }
}
