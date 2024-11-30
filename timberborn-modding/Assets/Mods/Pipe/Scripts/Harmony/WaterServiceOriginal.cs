using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace Mods.OldGopher.Pipe
{
  [HarmonyPatch]
  internal static class WaterServiceOriginal
  {
    private static bool initialized;

    private static object Instance = null;

    private static MethodBase _original_UpdateInflowLimiter = null;

    private static MethodBase _original_RemoveInflowLimiter = null;

    private static MethodBase _original_AddFullObstacle = null;

    private static MethodBase _original_RemoveFullObstacle = null;

    static bool _isUnsafe(string method)
    {
      if (HarmonyModStarter.Failed || Instance == null)
      {
        Debug.Log($"[WaterMapPatch.{method}] Failed={HarmonyModStarter.Failed} extender={Instance == null}");
        return true;
      }
      return false;
    }

    public static void GetOriginalMethods(Harmony harmony)
    {
      initialized = true;
      var _type_WaterService = AccessTools.TypeByName("Timberborn.WaterSystem.WaterService");
      // get original UpdateInflowLimiter
      var _methodInfo_UpdateInflowLimiter = AccessTools.Method(_type_WaterService, "UpdateInflowLimiter", new[] { typeof(Vector3Int), typeof(float) });
      _original_UpdateInflowLimiter = Harmony.GetOriginalMethod(_methodInfo_UpdateInflowLimiter);
      // get original RemoveInflowLimiter
      var _methodInfo_RemoveInflowLimiter = AccessTools.Method(_type_WaterService, "RemoveInflowLimiter", new[] { typeof(Vector3Int) });
      _original_RemoveInflowLimiter = Harmony.GetOriginalMethod(_methodInfo_RemoveInflowLimiter);
      // get original AddFullObstacle
      var _methodInfo_AddFullObstacle = AccessTools.Method(_type_WaterService, "AddFullObstacle", new[] { typeof(Vector3Int) });
      _original_AddFullObstacle = Harmony.GetOriginalMethod(_methodInfo_AddFullObstacle);
      // get original RemoveFullObstacle
      var _methodInfo_RemoveFullObstacle = AccessTools.Method(_type_WaterService, "RemoveFullObstacle", new[] { typeof(Vector3Int) });
      _original_RemoveFullObstacle = Harmony.GetOriginalMethod(_methodInfo_RemoveFullObstacle);
    }

    [HarmonyPostfix]
    [HarmonyPatch("Timberborn.WaterSystem.WaterService", "WaterService", MethodType.Constructor)]
    static void _Postfix_Constructor(ref object __instance)
    {
      //if (initialized)
      //  return;
      Instance = __instance;
      Debug.Log($"[Harmony.WaterServiceOriginal._Postfix_Constructor] _Postfix_Constructor Instance={Instance != null}");
    }

    public static void UpdateInflowLimiter(Vector3Int coordinate, float flowLimit)
    {
      try
      {
        if (_isUnsafe("UpdateInflowLimiter"))
          return;
        _original_UpdateInflowLimiter.Invoke(Instance, new[] { (object)coordinate, (object)flowLimit });
      }
      catch (Exception err)
      {
        Debug.Log($"[Harmony.Failed] UpdateInflowLimiter error={err}");
      }
    }

    public static void RemoveInflowLimiter(Vector3Int coordinate)
    {
      try
      {
        if (_isUnsafe("RemoveInflowLimiter"))
          return;
        _original_RemoveInflowLimiter.Invoke(Instance, new[] { (object)coordinate });
      }
      catch (Exception err)
      {
        Debug.Log($"[Harmony.Failed] RemoveInflowLimiter error={err}");
      }
    }

    public static void AddFullObstacle(Vector3Int coordinate)
    {
      try
      {
        if (_isUnsafe("AddFullObstacle"))
          return;
        _original_AddFullObstacle.Invoke(Instance, new[] { (object)coordinate });
      }
      catch (Exception err)
      {
        Debug.Log($"[Harmony.Failed] AddFullObstacle error={err}");
      }
    }

    public static void RemoveFullObstacle(Vector3Int coordinate)
    {
      try
      {
        if (_isUnsafe("RemoveFullObstacle"))
          return;
        _original_RemoveFullObstacle.Invoke(Instance, new[] { (object)coordinate });
      }
      catch (Exception err)
      {
        Debug.Log($"[Harmony.Failed] RemoveFullObstacle error={err}");
      }
    }
  }
}
