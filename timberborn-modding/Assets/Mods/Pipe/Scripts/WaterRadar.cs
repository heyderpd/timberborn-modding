using System.Collections.Generic;
using System.Linq;
using Bindito.Core;
using UnityEngine;
using Timberborn.Common;
using Timberborn.TickSystem;
using Timberborn.BlockSystem;
using Timberborn.SingletonSystem;
using Timberborn.WaterObjects;
using System.Reflection;
using Timberborn.MapStateSystem;
using Timberborn.TerrainSystem;
using System;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal enum WaterEventType
  {
    SET,
    UNSET
  }

  internal enum WaterObstacleType
  {
    BLOCK,
    HORIZONTAL,
    EMPTY
  }

  internal class WaterEvents
  {
    public readonly WaterEventType type;

    public readonly BlockObject block;

    public readonly Vector3Int gate;

    public WaterEvents(
      WaterEventType _type,
      BlockObject _block
    )
    {
      type = _type;
      block = _block;
    }

    public WaterEvents(
      WaterEventType _type,
      Vector3Int _gate
    )
    {
      type = _type;
      gate = _gate;
    }
  }

  internal class WaterColumn
  {
    public readonly Vector3Int floorCoord;

    public readonly string indexXY;

    public readonly int roof;

    public readonly int floor;

    public WaterColumn(
      Vector3Int _floorCoord,
      int _roof
    )
    {
      floorCoord = _floorCoord;
      indexXY = WaterUtils.ToXYIndex(floorCoord);
      roof = _roof;
      floor = _floorCoord.z;
    }
  }

  internal class WaterColumnGroup
  {
    public readonly Vector2Int indexXY;

    public readonly HashSet<Vector3Int> Gates = new HashSet<Vector3Int>();

    public readonly HashSet<WaterColumn> Columns = new HashSet<WaterColumn>();

    public readonly Dictionary<string, WaterColumn> columnsCache = new Dictionary<string, WaterColumn>(); // index3D to Column

    public WaterColumnGroup(
      Vector2Int _indexXY
    )
    {
      indexXY = _indexXY;
    }
    
    public bool IsEmpty()
    {
      return Gates.Count > 0;
    }

    public void Clear()
    {
      Gates.Clear();
      Columns.Clear();
      columnsCache.Clear();
    }

    public void RecalculeCache()
    {
      columnsCache.Clear();
      foreach (var gate in Gates)
      {
        foreach (var column in Columns)
        {
          if (column.roof > gate.z && gate.z >= column.floor)
          {
            columnsCache.Add(WaterUtils.To3DIndex(gate), column);
            break;
          }
        }
      }
    }
  }

  internal static class WaterUtils
  {
    public static string To3DIndex(Vector3Int coordinates)
    {
      if (coordinates == null)
        return null;
      var coord = $"{coordinates.x}:{coordinates.y}:{coordinates.z}";
      return coord;
    }

    public static string ToXYIndex(Vector3Int coordinates)
    {
      if (coordinates == null)
        return null;
      var coord = $"{coordinates.x}:{coordinates.y}";
      return coord;
    }

    public static Vector2Int ToVector2(Vector3Int coordinates)
    {
      return new Vector2Int(coordinates.x, coordinates.y);
    }

    public static string ToXYIndex(Vector2Int coordinates)
    {
      if (coordinates == null)
        return null;
      var coord = $"{coordinates.x}:{coordinates.y}";
      return coord;
    }
  }

  internal class WaterRadar : ITickableSingleton, ILoadableSingleton
  {
    private EventBus eventBus;

    private ITerrainService terrainService;

    private BlockService blockService;

    private readonly Queue<WaterEvents> gates = new Queue<WaterEvents>();

    private readonly Queue<WaterEvents> blocks = new Queue<WaterEvents>();

    private readonly Dictionary<Vector3Int, int> gateCoordinates = new Dictionary<Vector3Int, int>();

    private readonly HashSet<Vector3Int> gateAdd = new HashSet<Vector3Int>();

    private readonly HashSet<Vector3Int> gateRemove = new HashSet<Vector3Int>();

    private readonly HashSet<Vector2Int> columnUpdate = new HashSet<Vector2Int>();

    private readonly Dictionary<string, WaterColumnGroup> waterColumnsMap = new Dictionary<string, WaterColumnGroup>(); // indexXY to Column's

    [Inject]
    public void InjectDependencies(
      EventBus _eventBus,
      ITerrainService _terrainService,
      BlockService _blockService
    )
    {
      eventBus = _eventBus;
      terrainService = _terrainService;
      blockService = _blockService;
    }

    public void Load()
    {
      //eventBus.Register(this);
    }

    public void Tick()
    {/*
      BlockEventHandler();
      GateEventHandler();
      ColumEventHandler();*/
    }

    [OnEvent]
    public void OnBlockObjectSet(BlockObjectSetEvent blockEvent)
    {
      if (!blockEvent?.BlockObject)
        return;
      blocks.Enqueue(new WaterEvents(WaterEventType.SET, blockEvent.BlockObject));
    }

    [OnEvent]
    public void OnBlockObjectUnset(BlockObjectUnsetEvent blockEvent)
    {
      if (!blockEvent?.BlockObject)
        return;
      blocks.Enqueue(new WaterEvents(WaterEventType.UNSET, blockEvent.BlockObject));
    }

    public void OnGateCreate(Vector3Int coordinate)
    {
      if (coordinate == null)
        return;
      gates.Enqueue(new WaterEvents(WaterEventType.SET, coordinate));
    }

    public void OnGateDelete(Vector3Int coordinate)
    {
      if (coordinate == null)
        return;
      gates.Enqueue(new WaterEvents(WaterEventType.UNSET, coordinate));
    }

    public void GateAddCoordinate(Vector3Int coordinate)
    {
      if (gateCoordinates.TryGetValue(coordinate, out var value))
        gateCoordinates[coordinate] += 1;
      else
        gateCoordinates.Add(coordinate, 1);
      gateAdd.Add(coordinate);
    }

    public void GateRemoveCoordinate(Vector3Int coordinate)
    {
      if (gateCoordinates.TryGetValue(coordinate, out var value))
      {
        value -= 1;
        if (value > 0)
          gateCoordinates[coordinate] = value;
        else
          gateCoordinates.Remove(coordinate);
        gateRemove.Add(coordinate);
      }
    }

    private void GateEventHandler()
    {
      if (!gates.IsEmpty())
        return;
      while (!gates.IsEmpty())
      {
        var change = gates.Dequeue();
        switch (change.type)
        {
          case WaterEventType.SET:
            GateAddCoordinate(change.gate);
            break;

          case WaterEventType.UNSET:
            GateRemoveCoordinate(change.gate);
            break;

          default:
            break;
        }
      }
    }

    public WaterObstacleType FindWaterObstacle(Vector3Int coordinate)
    {
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 01 Underground={terrainService.Underground(coordinate)}");
      if (terrainService.Underground(coordinate))
        return WaterObstacleType.BLOCK;
      var blockMiddle = blockService.GetMiddleObjectAt(coordinate);
      var waterObstacleMiddle = blockMiddle?.IsFinished == true && blockMiddle?.GetComponentFast<WaterObstacle>() != null;
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 02 blockMiddle={blockMiddle} waterObstacleMiddle={waterObstacleMiddle}");
      if (waterObstacleMiddle)
        return WaterObstacleType.BLOCK;
      var blockBottom = blockService.GetBottomObjectAt(coordinate);
      var hasWaterObstacleBottom = blockBottom?.IsFinished == true && blockBottom?.GetComponentFast<WaterObstacle>() != null;
      ModUtils.Log($"[WaterRadar.FindWaterObstacle] 03 blockBottom={blockBottom} hasWaterObstacleBottom={hasWaterObstacleBottom}");
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
        ModUtils.Log($"[WaterRadar.SlowFindFloor] found={found} LOOP");
        if (found != WaterObstacleType.EMPTY)
        {
          floor = offset.z + (found == WaterObstacleType.BLOCK ? 1 : 0);
          break;
        }
        offset.z -= 1;
      }
      var floorCoordinate = new Vector3Int(coordinate.x, coordinate.y, floor);
      ModUtils.Log($"[WaterRadar.SlowFindFloor] coordinate={coordinate} floorCoordinate={floorCoordinate}");
      return floorCoordinate;
    }

    private void TryCreateColumn(WaterColumnGroup ColumnGroup, Vector3Int offset, int lastFloor)
    {
      if (offset.z > lastFloor)
      {
        ColumnGroup.Columns.Add(
          new WaterColumn(
            new Vector3Int(offset.x, offset.y, lastFloor),
            offset.z
          )
        );
      }
    }

    private WaterColumnGroup ColumnsCreate(Vector3Int index3D)
    {
      var indexXY = WaterUtils.ToVector2(index3D);
      var ColumnGroup = new WaterColumnGroup(indexXY);
      var lastFloor = 0;
      var offset = new Vector3Int(indexXY.x, indexXY.y, lastFloor);
      // while up Z from map botton with CheckPosition
      while (offset.z < MapSize.MaxGameTerrainHeight)
      {
        var found = FindWaterObstacle(offset);
        ModUtils.Log($"[WaterRadar.ColumnsCreate] offset={offset} found={found}");
        if (found == WaterObstacleType.BLOCK)
        {
          TryCreateColumn(ColumnGroup, offset, lastFloor);
          lastFloor = offset.z + 1;
        }
        if (found == WaterObstacleType.HORIZONTAL)
        {
          TryCreateColumn(ColumnGroup, offset, lastFloor);
          lastFloor = offset.z;
        }
        // found == WaterObstacleType.EMPTY only ignore!
        offset.z += 1;
      }
      return ColumnGroup;
    }

    private void ColumnsDelete(WaterColumnGroup Columns)
    {
      Columns.Clear();
      var indexXY = WaterUtils.ToXYIndex(Columns.indexXY);
      if (indexXY == null)
        return;
      waterColumnsMap.Remove(indexXY);
    }

    private void ColumEventHandler()
    {
      foreach (Vector3Int index3D in gateAdd)
      {
        var Columns = getWaterColumns(index3D);
        if (Columns != null)
          Columns.Gates.Add(index3D);
        else
          ColumnsCreate(index3D);
        columnUpdate.Add(WaterUtils.ToVector2(index3D));
      }
      gateAdd.Clear();
      foreach (Vector3Int index3D in gateRemove)
      {
        var indexXY = WaterUtils.ToVector2(index3D);
        var Columns = getWaterColumns(index3D);
        if (Columns == null)
          continue;
        Columns.Gates.Remove(index3D);
        columnUpdate.Add(WaterUtils.ToVector2(index3D));
      }
      gateRemove.Clear();
      foreach (Vector2Int indexXY in columnUpdate)
      {
        var Columns = getWaterColumns(indexXY);
        if (Columns == null)
          continue;
        if (Columns.IsEmpty())
          ColumnsDelete(Columns);
        else
          Columns.RecalculeCache();
      }
      columnUpdate.Clear();
    }

    private void BlockEventHandler()
    {
      return;
      if (!blocks.IsEmpty())
        return;
      while (!blocks.IsEmpty())
      {
        var change = blocks.Dequeue();
        switch (change.type)
        {
          case WaterEventType.SET:
            //GateAddCoordinate(change.block);
            break;

          case WaterEventType.UNSET:
            //GateRemoveCoordinate(change.block);
            break;

          default:
            break;
        }
      }
    }
    
    private WaterColumnGroup getWaterColumns(Vector3Int coordinate)
    {
      return getWaterColumns(WaterUtils.ToVector2(coordinate));
    }

    private WaterColumnGroup getWaterColumns(Vector2Int coordinate)
    {
      var indexXY = WaterUtils.ToXYIndex(coordinate);
      if (indexXY == null)
        return null;
      if (!waterColumnsMap.TryGetValue(indexXY, out var Columns))
        return null;
      return Columns;
    }

    public Vector3Int translateCoordinate(Vector3Int coordinate)
    {
      var Columns = getWaterColumns(coordinate);
      var index3D = WaterUtils.To3DIndex(coordinate);
      if (Columns == null || index3D == null)
        return coordinate;
      if (!Columns.columnsCache.TryGetValue(index3D, out var column))
        return coordinate;
      return column.floorCoord;
    }
  }
}
