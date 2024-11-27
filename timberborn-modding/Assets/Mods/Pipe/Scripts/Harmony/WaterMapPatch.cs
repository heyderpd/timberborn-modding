using UnityEngine;
using Bindito.Core;
using System.Reflection;
using HarmonyLib;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal class SettingsPatch: BaseComponent
  {
    private static WaterRadar waterRadar;

    [Inject]
    public void InjectDependencies(
      WaterRadar _waterRadar
    )
    {
      Debug.Log($"[harmony] InjectDependencies");
      waterRadar = _waterRadar;
    }

    static MethodInfo TargetMethod()
    {
      return AccessTools.Method(
        AccessTools.TypeByName("Timberborn.WaterSystem.WaterMap"),
        "OnFullObstacleAdded",
        new[] { typeof(object), typeof(Vector3Int) }
      );
    }

    static bool Prefix(object sender, Vector3Int coordinates)
    {
      Debug.Log($"[harmony] test coordinates={coordinates}");
      Debug.Log($"[harmony] test waterRadar={waterRadar != null}");
      Debug.Log($"[harmony] test IsOutOfMap={waterRadar?.IsOutOfMap(coordinates)}");
      return false;
    }
  }

  /*[HarmonyPatch("Timberborn.WaterSystem.WaterMap")]
  public class WaterMapPatch
  {
    [HarmonyPrefix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterMap", new[] { typeof(object), typeof(Vector3Int) })]
    static bool Prefix(object sender, Vector3Int coordinates)
    {
      Debug.Log($"[harmony] test coordinates={coordinates}");
      return false;
    }
  }*/
}
