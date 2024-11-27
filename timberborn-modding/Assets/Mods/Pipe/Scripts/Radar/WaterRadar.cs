using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.TerrainSystem;
using Timberborn.PrefabSystem;
using NUnit.Framework.Internal;
using System.Reflection;
using Timberborn.AreaSelectionSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.Common;
using System;

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
      extractWaterObstacleSpec(blockMiddle);
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

    public GhostWaterObstacle extractWaterObstacleSpec(BlockObject? blockObject)
    {
      try
      {
        ModUtils.Log($"[WaterRadar.extractWaterObstacle] 01 blockObject={blockObject}");
        if (blockObject?.IsFinished != true)
          return null;
        var waterObstacle = blockObject?.GetComponentFast<WaterObstacle>();
        ModUtils.Log($"[WaterRadar.extractWaterObstacle] 02 waterObstacle={waterObstacle}");
        if (waterObstacle == null)
          return null;

        foreach (var prop in waterObstacle.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
          try
          {
            ModUtils.Log($"\n[WaterRadar.extractWaterObstacle] prop={prop}");
            ModUtils.Log($"[WaterRadar.extractWaterObstacle] Name={prop.Name}");
            var val = prop.GetValue(waterObstacle);
            ModUtils.Log($"[WaterRadar.extractWaterObstacle] val={val}");
          }
          catch (Exception err)
          {
            ModUtils.Log($"#ERROR [WaterRadar.extractWaterObstacle.loop] err={err}");
          }
        }
        return null;

        /*var part01 = waterObstacle.GetType();
        ModUtils.Log($"[WaterRadar.extractWaterObstacle] 02.5a part01={part01}");
        var part02 = part01.GetProperty("_waterObstacleSpec", BindingFlags.NonPublic | BindingFlags.Instance);
        ModUtils.Log($"[WaterRadar.extractWaterObstacle] 02.5b part02={part02}");
        var waterObstacleSpec = part02.GetValue(waterObstacle);

        /* //var waterObstacleSpec = waterObstacle.GetType().GetProperty("Coordinates", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(waterObstacle);
        ModUtils.Log($"[WaterRadar.extractWaterObstacle] 03 waterObstacleSpec={waterObstacleSpec}");
        if (waterObstacleSpec == null)
          return null;
        var Coordinates = waterObstacleSpec.GetType().GetProperty("Coordinates", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(waterObstacleSpec);
        ModUtils.Log($"[WaterRadar.extractWaterObstacle] 06 Coordinates={Coordinates}");
        if (Coordinates == null)
          return null;
        var obstacle = new GhostWaterObstacle(
          new GhostBlockObject(blockObject),
          (ReadOnlyList <Vector2Int>) Coordinates
        );
        ModUtils.Log($"[WaterRadar.extractWaterObstacle] 05 obstacle={obstacle}");
        return obstacle;*/
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [WaterRadar.extractWaterObstacle] err={err}");
        return null;
      }
    }
  }
}
