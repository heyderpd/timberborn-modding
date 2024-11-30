using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal static class WaterServicePath
  {
    private static MethodBase _original_UpdateInflowLimiter = null;

    private static bool _isUnsafe(string method)
    {
      if (HarmonyModStarter.Failed)
      {
        Debug.Log($"[WaterMapPatch.{method}] Failed={HarmonyModStarter.Failed}");
        return true;
      }
      return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "UpdateInflowLimiter")]
    [HarmonyPatch(new[] { typeof(Vector3Int), typeof(float) })]
    private static bool _Prefix_UpdateInflowLimiter(Vector3Int coordinates, float flowLimit)
    {
      if (_isUnsafe("UpdateInflowLimiter"))
        return false;
      var proceed = WaterObstacleMap.CanUpdateInflowLimiter(coordinates, flowLimit);
      Debug.Log($"[WaterMapPatch.UpdateInflowLimiter] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "RemoveInflowLimiter")]
    [HarmonyPatch(new[] { typeof(Vector3Int) })]
    private static bool _Prefix_RemoveInflowLimiter(Vector3Int coordinates)
    {
      if (_isUnsafe("RemoveInflowLimiter"))
        return false;
      var proceed = WaterObstacleMap.CanRemoveInflowLimiter(coordinates);
      Debug.Log($"[WaterMapPatch.RemoveInflowLimiter] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "AddFullObstacle")]
    [HarmonyPatch(new[] { typeof(Vector3Int) })]
    private static bool _Prefix_AddFullObstacle(Vector3Int coordinates)
    {
      if (_isUnsafe("AddFullObstacle"))
        return false;
      var proceed = WaterObstacleMap.CanAddFullObstacle(coordinates);
      Debug.Log($"[WaterMapPatch.AddFullObstacle] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "RemoveFullObstacle")]
    [HarmonyPatch(new[] { typeof(Vector3Int) })]
    private static bool _Prefix_RemoveFullObstacle(Vector3Int coordinates)
    {
      if (_isUnsafe("RemoveFullObstacle"))
        return false;
      var proceed = WaterObstacleMap.CanRemoveFullObstacle(coordinates);
      Debug.Log($"[WaterMapPatch.RemoveFullObstacle] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }
  }
}
