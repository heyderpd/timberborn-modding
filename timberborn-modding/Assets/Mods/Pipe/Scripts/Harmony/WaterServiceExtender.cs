using Bindito.Core;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterServiceExtender
  {
    private WaterObstacleMap waterObstacleMap;

    [Inject]
    public void InjectDependencies(
      WaterObstacleMap _waterObstacleMap
    )
    {
      waterObstacleMap = _waterObstacleMap;
    }

    // TO ADD PARTIAL
    public bool CanUpdateInflowLimiter(Vector3Int coordinate, float flowLimit)
    {
      var state = waterObstacleMap.getCurrentState(coordinate);
      //var occupied = waterObstacleMap.isOccupied(coordinate);
      var added = waterObstacleMap.addPartial(coordinate, flowLimit);
      if (state == WaterObstacleState.FULL_OBSTACLE)
        return false;
      return added;
    }
    
    public bool CanRemoveInflowLimiter(Vector3Int coordinate)
    {



    }

    public bool CanAddFullObstacle(Vector3Int coordinate)
    {
      var state = waterObstacleMap.getCurrentState(coordinate);
      if (state == WaterObstacleState.FULL_OBSTACLE)
        return false;
      var added = waterObstacleMap.addBlock(coordinate);
      return true;
    }

    public bool CanRemoveFullObstacle(Vector3Int coordinate)
    {
      var oldState = waterObstacleMap.getCurrentState(coordinate);
      var removed = waterObstacleMap.removeBlock(coordinate);
      var newState = waterObstacleMap.getCurrentState(coordinate);



    }
  }
}
