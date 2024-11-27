using UnityEngine;
using Timberborn.Common;

namespace Mods.OldGopher.Pipe
{
  internal class GhostWaterObstacle
  {
    public readonly GhostBlockObject block;

    public readonly ReadOnlyList<Vector2Int> obstacleCoordinates;

    public GhostWaterObstacle(
      GhostBlockObject _block,
      ReadOnlyList<Vector2Int> _obstacleCoordinates
    )
    {
      block = _block;
      obstacleCoordinates = _obstacleCoordinates;
    }
  }
}
