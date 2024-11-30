using System.Collections.Generic;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterObstacleMap
  {
    private readonly BlockableCount fullObstacle = new BlockableCount(fullObstacle: true);

    private readonly BlockableCount partialObstacle = new BlockableCount(fullObstacle: false);

    public bool isOccupied(Vector3Int coordinate)
    {
      return fullObstacle.Contains(coordinate) || partialObstacle.Contains(coordinate);
    }

    public bool isEmpty(Vector3Int coordinate)
    {
      return !isOccupied(coordinate);
    }

    public WaterObstacleState getCurrentState(Vector3Int coordinate)
    {
      var hasAnyFullObstacle = fullObstacle.Contains(coordinate);
      if (hasAnyFullObstacle)
        return WaterObstacleState.FULL_OBSTACLE;
      var hasAnyPartialObstacle = partialObstacle.Contains(coordinate);
      if (hasAnyFullObstacle)
        return WaterObstacleState.PARTIAL_OBSTACLE;
      return WaterObstacleState.EMPTY;
    }

    public bool addPartial(Vector3Int coordinate, float flowLimit)
    {
      var obstacle = partialObstacle.Get(coordinate);
      if (obstacle == null)
      {
        partialObstacle.Block(new WaterObstacle(coordinate, flowLimit));
        return true;
      }
      if (obstacle.flowLimit != flowLimit)
      {
        obstacle.setFlowLimit(flowLimit);
        return true;
      }
      return false;
    }

    public bool removePartial(Vector3Int coordinate)
    {
      return partialObstacle.Unblock(coordinate);
    }

    public bool addBlock(Vector3Int coordinate)
    {
      var obstacle = fullObstacle.Get(coordinate);
      if (obstacle == null)
      {
        fullObstacle.Block(new WaterObstacle(coordinate));
        return true;
      }
      return false;
    }

    public bool removeBlock(Vector3Int coordinate)
    {
      return fullObstacle.Unblock(coordinate);
    }
  }
}
