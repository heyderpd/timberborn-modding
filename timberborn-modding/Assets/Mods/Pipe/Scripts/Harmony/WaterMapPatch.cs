using UnityEngine;
using Bindito.Core;
using HarmonyLib;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal class WaterMapPatch : BaseComponent
  {
    private static WaterRadar waterRadar;

    private static int tmpInit = 0;
    private static int tmpCount = 0;

    [Inject]
    public void InjectDependencies(
      WaterRadar _waterRadar
    )
    {
      tmpInit++;
      waterRadar = _waterRadar;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterMap", "OnFullObstacleAdded")]
    [HarmonyPatch(new[] { typeof(object), typeof(Vector3Int) })]
    static bool OnFullObstacleAddedPrefix(object sender, Vector3Int coordinates)
    {
      Debug.Log($"[harmony] OnFullObstacleAddedPrefix coordinates={coordinates} init={tmpInit} waterRadar={waterRadar != null} IsOutOfMap={waterRadar?.IsOutOfMap(coordinates)}");
      return tmpCount % 2 == 0;
    }

    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterMap", "OnFullObstacleRemoved")]
    [HarmonyPatch(new[] { typeof(object), typeof(Vector3Int) })]
    static bool OnFullObstacleRemovedPrefix(object sender, Vector3Int coordinates)
    {
      Debug.Log($"[harmony] OnFullObstacleRemovedPrefix coordinates={coordinates} init={tmpInit} waterRadar={waterRadar != null} IsOutOfMap={waterRadar?.IsOutOfMap(coordinates)}");
      return false;
    }
  }
}
