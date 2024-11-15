using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.TerrainSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class WaterRadar
  {
    private ITerrainService terrainService;

    private BlockService blockService;

    [Inject]
    public void InjectDependencies(
      ITerrainService _terrainService,
      BlockService _blockService
    )
    {
      terrainService = _terrainService;
      blockService = _blockService;
    }

    public PipeNode FindPipe(Vector3Int coordinate)
    {
      if (terrainService.Underground(coordinate))
        return null;
      var block = blockService.GetMiddleObjectAt(coordinate);
      if (block?.IsFinished != true)
        return null;
      var pipe = block.GetComponentFast<PipeNode>();
      if (pipe != null)
        return pipe;
      return null;
    }

    public WaterObstacleType FindWaterObstacle(Vector3Int coordinate, PipeNode pipeOrigin = null)
    {
      if (terrainService.Underground(coordinate))
        return WaterObstacleType.BLOCK;
      var blockMiddle = blockService.GetMiddleObjectAt(coordinate);
      var pipe = blockMiddle?.IsFinished == true ? blockMiddle?.GetComponentFast<PipeNode>() : null;
      if (pipe != null && pipe == pipeOrigin)
        return WaterObstacleType.EMPTY;
      var waterObstacleMiddle = blockMiddle?.IsFinished == true && blockMiddle?.GetComponentFast<WaterObstacle>() != null;
      if (waterObstacleMiddle)
        return WaterObstacleType.BLOCK;
      var blockBottom = blockService.GetBottomObjectAt(coordinate);
      var hasWaterObstacleBottom = blockBottom?.IsFinished == true && blockBottom?.GetComponentFast<WaterObstacle>() != null;
      if (hasWaterObstacleBottom)
        return WaterObstacleType.HORIZONTAL;
      return WaterObstacleType.EMPTY;
    }

    public bool IsBlocked(Vector3Int coordinate)
    {
      var block = blockService.GetObjectsAt(coordinate);
      return !block.IsEmpty();
    }
  }
}
