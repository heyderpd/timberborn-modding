using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal enum WaterObstacleState
  {
    FULL_OBSTACLE,
    PARTIAL_OBSTACLE,
    EMPTY
  }

  internal class WaterObstacle
  {
    public bool isFullObstacle { get; private set; }

    public bool isPartialObstacle => !isFullObstacle;

    public Vector3Int coordinate { get; private set; }

    public float flowLimit { get; private set; }

    public Vector3Int Key => coordinate;

    public WaterObstacle(Vector3Int _coordinate)
    {
      coordinate = _coordinate;
      isFullObstacle = true;
    }

    public WaterObstacle(Vector3Int _coordinate, float _flowLimit)
    {
      coordinate = _coordinate;
      flowLimit = _flowLimit;
      isFullObstacle = false;
    }

    public void setFlowLimit(float _flowLimit)
    {
      flowLimit = _flowLimit;
    }

    public WaterObstacleState getType()
    {
      return isFullObstacle
        ? WaterObstacleState.FULL_OBSTACLE
        : WaterObstacleState.PARTIAL_OBSTACLE;
    }

    public bool isEqual(WaterObstacle obstacle)
    {
      if (getType() != obstacle.getType())
        return false;
      if (isFullObstacle)
        return true;
      return flowLimit == obstacle.flowLimit;
    }
  }
}
