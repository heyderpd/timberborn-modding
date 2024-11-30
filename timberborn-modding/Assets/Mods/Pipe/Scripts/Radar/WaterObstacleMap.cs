using Bindito.Core;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal static class WaterObstacleMap
  {
    public static readonly BlockableCount<Vector3Int> fullObstacle = new BlockableCount<Vector3Int>();

    public static readonly BlockableCount<Vector3Int> partialObstacle = new BlockableCount<Vector3Int>();

    public static void Clear()
    {
      fullObstacle.Clear();
      partialObstacle.Clear();
    }

    public static bool CanUpdateInflowLimiter(Vector3Int coordinate, float flowLimit)
    {
      partialObstacle.Block(coordinate);
      return true;
    }

    public static bool CanRemoveInflowLimiter(Vector3Int coordinate)
    {
      partialObstacle.Unblock(coordinate);
      return true;
    }

    public static bool CanAddFullObstacle(Vector3Int coordinate)
    {
      partialObstacle.Block(coordinate);
      return true;
    }

    public static bool CanRemoveFullObstacle(Vector3Int coordinate)
    {
      return partialObstacle.Unblock(coordinate);
    }
  }
}
