using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterObstacle
  {
    public bool isFullObstacle { get; private set; }

    public bool isPartialObstacle => !isFullObstacle;

    private Vector3Int coordinate;

    private float flowLimit;

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
  }
}
