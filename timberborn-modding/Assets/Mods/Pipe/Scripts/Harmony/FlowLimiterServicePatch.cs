using System;
using System.Reflection;
using Bindito.Core;
using UnityEngine;
using HarmonyLib;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal class FlowLimiterServicePatch : BaseComponent
  {
    private static WaterRadar waterRadar;

    private static Type Modification = null;

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
      Modification = AccessTools.TypeByName("Timberborn.WaterSystem.FlowLimiterService.Modification");
      return AccessTools.Method(
        AccessTools.TypeByName("Timberborn.WaterSystem.FlowLimiterService"),
        "OnFullObstacleAdded",
        new[] { typeof(object) }
      );
    }

    static bool Prefix(object _modification)
    {
      var modification = Convert.ChangeType(_modification, Modification);
      Debug.Log($"[harmony] test modification={modification}");
      return false;
    }
  }
}
