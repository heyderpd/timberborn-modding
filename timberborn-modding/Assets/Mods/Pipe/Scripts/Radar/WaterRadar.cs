using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.TerrainSystem;

namespace Mods.OldGopher.Pipe
{
  internal class WaterRadar
  {
    private ITerrainService terrainService;

    private BlockService blockService;

    private WaterObstacleMap waterObstacleMap;

    [Inject]
    public void InjectDependencies(
      ITerrainService _terrainService,
      BlockService _blockService,
      WaterObstacleMap _waterObstacleMap
    )
    {
      terrainService = _terrainService;
      blockService = _blockService;
      waterObstacleMap = _waterObstacleMap;
    }

    public bool IsOutOfMap(Vector3Int coordinate)
    {
      return coordinate.x < 0
        || terrainService.Size.x <= coordinate.x
        || coordinate.y < 0
        || terrainService.Size.y <= coordinate.y;
    }

    public bool Underground(Vector3Int coordinate)
    {
      if (IsOutOfMap(coordinate))
        return true;
      return terrainService.Underground(coordinate);
    }

    public bool IsInvalidCoordinate(Vector3Int coordinate)
    {
      if (IsOutOfMap(coordinate))
        return true;
      if (Underground(coordinate))
        return true;
      return false;
    }

    public BlockObject GetMiddleObjectAt(Vector3Int coordinate)
    {
      var block = blockService.GetMiddleObjectAt(coordinate);
      if (block?.IsFinished != true)
        return null;
      return block;
    }

    private PipeNode FindPipe(BlockObject block)
    {
      var pipe = block?.GetComponentFast<PipeNode>() ?? null;
      if (pipe != null)
        return pipe;
      return null;
    }

    private (PipeNode, WaterObstacleType) FindMiddleWaterObstacle(Vector3Int coordinate, bool checkOutOfMap)
    {
      if (checkOutOfMap && IsOutOfMap(coordinate))
        return (null, WaterObstacleType.BLOCK);
      if (terrainService.Underground(coordinate))
        return (null, WaterObstacleType.BLOCK);
      var blockMiddle = GetMiddleObjectAt(coordinate);
      var existWaterObstacle = HarmonyModStarter.Failed
        ? SimpleObstacleMap.Exist(blockMiddle)
        : waterObstacleMap.Exist(coordinate);
      var obstacle = existWaterObstacle
        ? WaterObstacleType.BLOCK
        : WaterObstacleType.EMPTY;
      var pipe = FindPipe(blockMiddle);
      return (pipe, obstacle);
    }

    private WaterObstacleType FindBottomWaterObstacle(Vector3Int coordinate)
    {
      var blockBottom = blockService.GetBottomObjectAt(coordinate);
      var hasWaterObstacleBottom = blockBottom?.IsFinished == true && blockBottom?.GetComponentFast<WaterObstacle>() != null;
      if (hasWaterObstacleBottom)
        return WaterObstacleType.HORIZONTAL;
      return WaterObstacleType.EMPTY;
    }

    public (PipeNode, WaterObstacleType) FindWaterObstacle(Vector3Int coordinate, bool checkOutOfMap = true)
    {
      var (pipe, obstacle) = FindMiddleWaterObstacle(coordinate, checkOutOfMap);
      if (pipe != null || obstacle == WaterObstacleType.BLOCK)
        return (pipe, obstacle);
      obstacle = FindBottomWaterObstacle(coordinate);
      return (null, obstacle);
    }

    public bool IsBlockedAnyObject(Vector3Int coordinate)
    {
      var block = blockService.GetObjectsAt(coordinate);
      return !block.IsEmpty();
    }
  }
}
