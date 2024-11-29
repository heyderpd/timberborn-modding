using UnityEngine;
using Bindito.Core;
using HarmonyLib;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal class WaterServicePath : BaseComponent
  {
    private static WaterServiceExtender waterServiceExtender;

    [Inject]
    public void InjectDependencies(
      WaterServiceExtender _waterServiceExtender
    )
    {
      waterServiceExtender = _waterServiceExtender;
    }

    static bool isUnsafe(string method)
    {
      if (HarmonyModStarter.Failed || waterServiceExtender == null)
      {
        Debug.Log($"[WaterMapPatch.{method}] Failed={HarmonyModStarter.Failed} extender={waterServiceExtender == null}");
        return true;
      }
      return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "UpdateInflowLimiter")]
    [HarmonyPatch(new[] { typeof(object) })]
    static bool _Prefix_UpdateInflowLimiter(Vector3Int coordinates, float flowLimit)
    {
      if (isUnsafe("UpdateInflowLimiter"))
        return false;
      var proceed = waterServiceExtender.CanUpdateInflowLimiter(coordinates, flowLimit);
      Debug.Log($"[WaterMapPatch.UpdateInflowLimiter] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "RemoveInflowLimiter")]
    [HarmonyPatch(new[] { typeof(object) })]
    static bool _Prefix_RemoveInflowLimiter(Vector3Int coordinates)
    {
      if (isUnsafe("RemoveInflowLimiter"))
        return false;
      var proceed = waterServiceExtender.CanRemoveInflowLimiter(coordinates);
      Debug.Log($"[WaterMapPatch.RemoveInflowLimiter] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "AddFullObstacle")]
    [HarmonyPatch(new[] { typeof(object) })]
    static bool _Prefix_AddFullObstacle(Vector3Int coordinates)
    {
      if (isUnsafe("AddFullObstacle"))
        return false;
      var proceed = waterServiceExtender.CanAddFullObstacle(coordinates);
      Debug.Log($"[WaterMapPatch.AddFullObstacle] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "RemoveFullObstacle")]
    [HarmonyPatch(new[] { typeof(object) })]
    static bool _Prefix_RemoveFullObstacle(Vector3Int coordinates)
    {
      if (isUnsafe("RemoveFullObstacle"))
        return false;
      var proceed = waterServiceExtender.CanRemoveFullObstacle(coordinates);
      Debug.Log($"[WaterMapPatch.RemoveFullObstacle] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }
  }
}
