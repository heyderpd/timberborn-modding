using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.TerrainSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal enum WaterObstacleType
  {
    BLOCK,
    HORIZONTAL,
    EMPTY
  }

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
      ModUtils.Log($"[WaterRadar.FindPipe] 01 Underground={terrainService.Underground(coordinate)}");
      if (terrainService.Underground(coordinate))
        return null;
      var block = blockService.GetMiddleObjectAt(coordinate);
      ModUtils.Log($"[WaterRadar.FindPipe] 02 IsFinished={block?.IsFinished != true}");
      if (block?.IsFinished != true)
        return null;
      var pipe = block.GetComponentFast<PipeNode>();
      ModUtils.Log($"[WaterRadar.FindPipe] 03 block={pipe == null}");
      if (pipe != null)
        return pipe;
      return null;
    }

    public WaterObstacleType FindWaterObstacle(Vector3Int coordinate)
    {
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 01 Underground={terrainService.Underground(coordinate)}");
      if (terrainService.Underground(coordinate))
        return WaterObstacleType.BLOCK;
      var blockMiddle = blockService.GetMiddleObjectAt(coordinate);
      var waterObstacleMiddle = blockMiddle?.IsFinished == true && blockMiddle?.GetComponentFast<WaterObstacle>() != null;
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 03 blockMiddle={blockMiddle} waterObstacleMiddle={waterObstacleMiddle}");
      if (waterObstacleMiddle)
        return WaterObstacleType.BLOCK;
      var blockBottom = blockService.GetBottomObjectAt(coordinate);
      var hasWaterObstacleBottom = blockBottom?.IsFinished == true && blockBottom?.GetComponentFast<WaterObstacle>() != null;
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 04 blockBottom={blockBottom} hasWaterObstacleBottom={hasWaterObstacleBottom}");
      if (hasWaterObstacleBottom)
        return WaterObstacleType.HORIZONTAL;
      return WaterObstacleType.EMPTY;
    }
  }
}
