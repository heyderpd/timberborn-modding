using System.Linq;
using System.Collections.Immutable;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.TerrainSystem;
using Timberborn.PrefabSystem;

namespace Mods.OldGopher.Pipe
{
  internal class WaterRadar
  {
    private ImmutableArray<string> invalidBuilds = ImmutableArray.Create("floodgate", "levee", "sluice");

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

    public bool IsOutOfMap(Vector3Int coordinate)
    {
      return coordinate.x < 0
        || terrainService.Size.x <= coordinate.x
        || coordinate.y < 0
        || terrainService.Size.y <= coordinate.y;
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
      if (blockMiddle?.IsFinished == true)
      {
        var pipe = blockMiddle?.GetComponentFast<PipeNode>();
        if (pipe != null && pipe == pipeOrigin)
          return WaterObstacleType.EMPTY;
        var prafabName = blockMiddle?.GetComponentFast<Prefab>()?.Name.ToLower() ?? "";
        if (prafabName != "" && invalidBuilds.FirstOrDefault(name => prafabName.Contains(name)) != null)
          return WaterObstacleType.BLOCK;
        if (blockMiddle?.GetComponentFast<WaterObstacle>() != null)
          return WaterObstacleType.BLOCK;
      }
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
