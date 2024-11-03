using System.Linq;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.TerrainSystem;
using Timberborn.WaterBuildings;

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

    public WaterObstacleType FindWaterObstacle(Vector3Int coordinate, WaterGateSide? Side = null)
    {
      var Ceiling = Side == WaterGateSide.TOP ? 0f : 0.75f;
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 01 Underground={terrainService.Underground(coordinate)} Ceiling={Ceiling}");
      if (terrainService.Underground(coordinate))
        return WaterObstacleType.BLOCK;
      var floodgate = blockService.GetObjectsWithComponentAt<Floodgate>(coordinate).FirstOrDefault();
      var hasFloodgateObstacle = floodgate?.enabled == true && floodgate?.Height > Ceiling;
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 02 floodgate={floodgate} enabled={floodgate?.enabled} Height={floodgate?.Height} hasFloodgateObstacle={hasFloodgateObstacle}");
      if (hasFloodgateObstacle)
        return WaterObstacleType.BLOCK;
      if (floodgate == null)
      {
        var blockMiddle = blockService.GetMiddleObjectAt(coordinate);
        var waterObstacleMiddle = blockMiddle?.IsFinished == true && blockMiddle?.GetComponentFast<WaterObstacle>() != null;
        ModUtils.Log($"[WaterRadar.FindWaterObstacle] 03 blockMiddle={blockMiddle} waterObstacleMiddle={waterObstacleMiddle}");
        if (waterObstacleMiddle)
          return WaterObstacleType.BLOCK;
      }
      var blockBottom = blockService.GetBottomObjectAt(coordinate);
      var hasWaterObstacleBottom = blockBottom?.IsFinished == true && blockBottom?.GetComponentFast<WaterObstacle>() != null;
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 04 blockBottom={blockBottom} hasWaterObstacleBottom={hasWaterObstacleBottom}");
      if (hasWaterObstacleBottom)
        return WaterObstacleType.HORIZONTAL;
      return WaterObstacleType.EMPTY;
    }

    public Vector3Int FloorFinderSlower(Vector3Int coordinate)
    {
      int floor = coordinate.z;
      var offset = new Vector3Int(coordinate.x, coordinate.y, coordinate.z);
      while (offset.z > 0)
      {
        var found = FindWaterObstacle(offset);
        if (found != WaterObstacleType.EMPTY)
        {
          floor = offset.z + (found == WaterObstacleType.BLOCK ? 1 : 0);
          break;
        }
        offset.z -= 1;
      }
      var floorCoordinate = new Vector3Int(coordinate.x, coordinate.y, floor);
      return floorCoordinate;
    }
  }
}
