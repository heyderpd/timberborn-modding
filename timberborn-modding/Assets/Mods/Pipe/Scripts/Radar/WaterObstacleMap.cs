using Amazon.Runtime.Internal.Transform;
using Bindito.Core;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using System.Collections.Generic;
using Timberborn.BlockSystem;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal static class WaterObstacleMap
  {
    public static readonly BlockableCount<Vector3Int> fullObstacle = new BlockableCount<Vector3Int>();

    public static readonly Dictionary<Vector3Int, float> partialObstacle = new Dictionary<Vector3Int, float>();

    public static void Clear()
    {
      fullObstacle.Clear();
      partialObstacle.Clear();
    }

    public static bool CanAddFullObstacle(Vector3Int coordinate)
    {
      fullObstacle.Block(coordinate);
      return true;
    }

    public static bool CanRemoveFullObstacle(Vector3Int coordinate)
    {
      return fullObstacle.Unblock(coordinate);
    }

    public static bool CanUpdateInflowLimiter(Vector3Int coordinate, float flowLimit)
    {
      if (partialObstacle.ContainsKey(coordinate))
        partialObstacle[coordinate] = flowLimit;
      else
        partialObstacle.Add(coordinate, flowLimit);
      return true;
    }

    public static bool CanRemoveInflowLimiter(Vector3Int coordinate)
    {
      if (partialObstacle.ContainsKey(coordinate))
        partialObstacle.Remove(coordinate);
      return true;
    }

    public static bool IsBlocked(Vector3Int coordinate)
    {
      if (fullObstacle.Contains(coordinate))
        return true;
      if (partialObstacle.TryGetValue(coordinate, out var flowLimit))
      {
        return flowLimit > 0.7f;
      }
      return false;
    }
  }
}
