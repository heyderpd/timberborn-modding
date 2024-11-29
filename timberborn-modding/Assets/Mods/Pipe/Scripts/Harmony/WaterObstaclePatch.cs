using UnityEngine;
using Bindito.Core;
using HarmonyLib;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal class WaterObstaclePatch : BaseComponent
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
    [HarmonyPatch("Timberborn.WaterObjects.WaterObstacle", "AddToWaterService")]
    [HarmonyPatch(new[] { typeof(float) })]
    static bool OnFullObstacleAddedPrefix(float height)
    {
      if (HarmonyModStarter.Failed || waterMapExtender == null)
      {
        Debug.Log($"[WaterMapPatch.OnFullObstacleAddedPrefix] Failed={HarmonyModStarter.Failed} extender={waterMapExtender == null}");
        return true;
      }
      var proceed = waterMapExtender.CanAddFullObstacle(coordinates);
      Debug.Log($"[WaterMapPatch.OnFullObstacleAddedPrefix] coordinates={coordinates} CanAddFullObstacle={proceed}");
      return proceed;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterObjects.WaterObstacle", "RemoveFromWaterService")]
    static bool OnFullObstacleRemovedPrefix()
    {
      if (HarmonyModStarter.Failed || waterMapExtender == null)
      {
        Debug.Log($"[WaterMapPatch.OnFullObstacleRemovedPrefix] Failed={HarmonyModStarter.Failed} extender={waterMapExtender == null}");
        return true;
      }
      var proceed = waterMapExtender.CanRemoveFullObstacle(coordinates);
      Debug.Log($"[WaterMapPatch.OnFullObstacleRemovedPrefix] coordinates={coordinates} CanRemoveFullObstacle={proceed}");
      return proceed;
    }
  }
}
