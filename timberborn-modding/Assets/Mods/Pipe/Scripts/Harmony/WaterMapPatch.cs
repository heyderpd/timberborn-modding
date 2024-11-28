using UnityEngine;
using Bindito.Core;
using HarmonyLib;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal class WaterMapPatch : BaseComponent
  {
    private static WaterObstacleMap waterObstacleMap;

    [Inject]
    public void InjectDependencies(
      WaterObstacleMap _waterObstacleMap
    )
    {
      waterObstacleMap = _waterObstacleMap;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterMap", "OnFullObstacleAdded")]
    [HarmonyPatch(new[] { typeof(object), typeof(Vector3Int) })]
    static bool OnFullObstacleAddedPrefix(object sender, Vector3Int coordinates)
    {
      ModUtils.Log($"[harmony.WaterMapPatch.OnFullObstacleAddedPrefix] HarmonyFailed={HarmonyModStarter.Failed} WaterRadarFailed={waterObstacleMap == null}");
      if (HarmonyModStarter.Failed || waterObstacleMap == null)
        return true;
      return waterObstacleMap.CanAddFullObstacle(coordinates);
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterMap", "OnFullObstacleRemoved")]
    [HarmonyPatch(new[] { typeof(object), typeof(Vector3Int) })]
    static bool OnFullObstacleRemovedPrefix(object sender, Vector3Int coordinates)
    {
      ModUtils.Log($"[harmony.WaterMapPatch.OnFullObstacleAddedPrefix] HarmonyFailed={HarmonyModStarter.Failed} WaterRadarFailed={waterObstacleMap == null}");
      if (HarmonyModStarter.Failed || waterObstacleMap == null)
        return true;
      return waterObstacleMap.CanRemoveFullObstacle(coordinates);
    }
  }
}
