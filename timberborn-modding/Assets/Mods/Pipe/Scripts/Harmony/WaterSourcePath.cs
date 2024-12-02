using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Timberborn.WaterSourceSystem;
using System.Linq;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal static class WaterSourcePath
  {
    private static MethodBase _original_UpdateInflowLimiter = null;

    private static bool _isUnsafe(string method)
    {
      if (ModStarter.Failed)
      {
        Debug.Log($"[WaterSourcePath.{method}] Failed={ModStarter.Failed}");
        return true;
      }
      return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("Timberborn.WaterSourceSystem.WaterSource", "InitializeEntity")]
    public static void _Postfix_InitializeEntity(WaterSource __instance)
    {
      if (_isUnsafe("InitializeEntity") || __instance?.Coordinates == null)
        return;
      //WaterSourceMap.Add(__instance);
      WaterSourceMap.Block(__instance.Coordinates);
      Debug.Log($"[WaterSourcePath.InitializeEntity] Coordinates={__instance.Coordinates.Count()}");
    }

    [HarmonyPostfix]
    [HarmonyPatch("Timberborn.WaterSourceSystem.WaterSource", "DeleteEntity")]
    public static void _Postfix_DeleteEntity(WaterSource __instance)
    {
      if (_isUnsafe("DeleteEntity") || __instance?.Coordinates == null)
        return;
      //WaterSourceMap.Remove(__instance);
      WaterSourceMap.Unblock(__instance.Coordinates);
      Debug.Log($"[WaterSourcePath.DeleteEntity] Coordinates={__instance.Coordinates.Count()}");
    }
  }
}
