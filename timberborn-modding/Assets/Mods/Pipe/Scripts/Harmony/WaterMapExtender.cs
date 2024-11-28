using Bindito.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterMapExtender
  {
    private readonly HashSet<Vector3Int> virtualObstacle;

    private readonly HashSet<Vector3Int> nativeObstacle;

    private WaterRadar waterRadar;

    private WaterObstacleMap waterObstacleMap;

    [Inject]
    public void InjectDependencies(
      WaterRadar _waterRadar,
      WaterObstacleMap _waterObstacleMap
    )
    {
      waterRadar = _waterRadar;
      waterObstacleMap = _waterObstacleMap;
    }

    public bool CanAddFullObstacle(Vector3Int coordinate)
    {
      if (waterRadar.IsInvalidCoordinate(coordinate))
        return false;
      var canProceed = waterObstacleMap.NotExist(coordinate);
      waterObstacleMap.SetNative(coordinate);
      return canProceed;
    }

    public bool CanRemoveFullObstacle(Vector3Int coordinate)
    {
      if (waterRadar.IsInvalidCoordinate(coordinate))
        return false;
      var canProceed = waterObstacleMap.Exist(coordinate);
      waterObstacleMap.UnsetNative(coordinate);
      return canProceed;
    }
  }
}
