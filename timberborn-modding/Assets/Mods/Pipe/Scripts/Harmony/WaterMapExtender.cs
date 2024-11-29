using Bindito.Core;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterMapExtender
  {
    private WaterObstacleMap waterObstacleMap;

    [Inject]
    public void InjectDependencies(
      WaterObstacleMap _waterObstacleMap
    )
    {
      waterObstacleMap = _waterObstacleMap;
    }

    public bool CanAddFullObstacle(Vector3Int coordinate)
    {
      return waterObstacleMap.SetNative(coordinate);
    }

    public bool CanRemoveFullObstacle(Vector3Int coordinate)
    {
      return waterObstacleMap.UnsetNative(coordinate);
    }
  }
}
