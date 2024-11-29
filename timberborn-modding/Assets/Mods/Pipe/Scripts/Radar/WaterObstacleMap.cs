using Bindito.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterObstacleMap
  {
    private readonly HashSet<Vector3Int> virtualObstacle = new HashSet<Vector3Int>();

    private readonly BlockableCount<Vector3Int> nativeObstacle = new BlockableCount<Vector3Int>();

    private bool IsInvalid(Vector3Int coordinate)
    {
      return coordinate == null;
    }

    public void SetVirtual(Vector3Int coordinate)
    {
      if (IsInvalid(coordinate) || virtualObstacle.Contains(coordinate))
        return;
      virtualObstacle.Add(coordinate);
    }

    public void UnsetVirtual(Vector3Int coordinate)
    {
      if (virtualObstacle.Contains(coordinate))
        virtualObstacle.Remove(coordinate);
    }

    public void SetNative(Vector3Int coordinate)
    {
      if (IsInvalid(coordinate))
        return;
      nativeObstacle.Block(coordinate);
    }

    public void UnsetNative(Vector3Int coordinate)
    {
      nativeObstacle.Unblock(coordinate);
    }

    public bool Exist(Vector3Int coordinate)
    {
      if (IsInvalid(coordinate))
        return false;
      return nativeObstacle.Contains(coordinate);
    }

    public bool NotExist(Vector3Int coordinate)
    {
      return !Exist(coordinate);
    }
  }
}
