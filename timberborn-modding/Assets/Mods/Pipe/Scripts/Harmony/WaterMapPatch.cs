using UnityEngine;
using Bindito.Core;
using HarmonyLib;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal class WaterMapPatch : BaseComponent
  {
    private static WaterMapExtender waterMapExtender;

    [Inject]
    public void InjectDependencies(
      WaterMapExtender _waterMapExtender
    )
    {
      waterMapExtender = _waterMapExtender;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterMap", "OnFullObstacleAdded")]
    [HarmonyPatch(new[] { typeof(object), typeof(Vector3Int) })]
    static bool OnFullObstacleAddedPrefix(object sender, Vector3Int coordinates)
    {
      ModUtils.Log($"[harmony.WaterMapPatch.OnFullObstacleAddedPrefix] HarmonyFailed={HarmonyModStarter.Failed} WaterRadarFailed={waterMapExtender == null}");
      if (HarmonyModStarter.Failed || waterMapExtender == null)
        return true;
      return waterMapExtender.CanAddFullObstacle(coordinates);
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterMap", "OnFullObstacleRemoved")]
    [HarmonyPatch(new[] { typeof(object), typeof(Vector3Int) })]
    static bool OnFullObstacleRemovedPrefix(object sender, Vector3Int coordinates)
    {
      ModUtils.Log($"[harmony.WaterMapPatch.OnFullObstacleAddedPrefix] HarmonyFailed={HarmonyModStarter.Failed} WaterRadarFailed={waterMapExtender == null}");
      if (HarmonyModStarter.Failed || waterMapExtender == null)
        return true;
      return waterMapExtender.CanRemoveFullObstacle(coordinates);
    }
  }
}