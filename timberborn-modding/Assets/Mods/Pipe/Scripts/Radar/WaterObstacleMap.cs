using Bindito.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterObstacleMap
  {
    private readonly HashSet<Vector3Int> virtualObstacle;

    private readonly HashSet<Vector3Int> nativeObstacle;

    private WaterRadar waterRadar;

    [Inject]
    public void InjectDependencies(
      WaterRadar _waterRadar
    )
    {
      waterRadar = _waterRadar;
    }

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
      if (IsInvalid(coordinate) || nativeObstacle.Contains(coordinate))
        return;
      nativeObstacle.Add(coordinate);
    }

    public void UnsetNative(Vector3Int coordinate)
    {
      if (nativeObstacle.Contains(coordinate))
        nativeObstacle.Remove(coordinate);
    }

    public bool Exist(Vector3Int coordinate)
    {
      if (IsInvalid(coordinate))
        return false;
      return virtualObstacle.Contains(coordinate) || nativeObstacle.Contains(coordinate);
    }

    public bool NotExist(Vector3Int coordinate)
    {
      return !Exist(coordinate);
    }

    public bool CanAddFullObstacle(Vector3Int coordinate)
    {
      if (waterRadar.IsInvalidCoordinate(coordinate))
        return false;
      var canProceed = NotExist(coordinate);
      SetNative(coordinate);
      return canProceed;
    }

    public bool CanRemoveFullObstacle(Vector3Int coordinate)
    {
      if (waterRadar.IsInvalidCoordinate(coordinate))
        return false;
      var canProceed = Exist(coordinate);
      UnsetNative(coordinate);
      return canProceed;
    }
  }
}
