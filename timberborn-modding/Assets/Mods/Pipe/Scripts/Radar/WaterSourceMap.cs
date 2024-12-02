using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;
using Timberborn.Common;
using Timberborn.WaterSourceSystem;

namespace Mods.OldGopher.Pipe
{
  internal static class WaterSourceMap
  {
    //private static readonly HashSet<WaterSource> waterSources = new HashSet<WaterSource>();

    private static readonly HashSet<Vector3Int> waterSourceCoordinates = new HashSet<Vector3Int>();

    //private static readonly HashSet<Vector2Int> waterSourceCoordinatesXY = new HashSet<Vector2Int>();

    public static void Clear()
    {
      //waterSources.Clear();
      waterSourceCoordinates.Clear();
    }

    /*public static void Add(WaterSource waterSource)
    {
      if (waterSource == null)
        return;
      waterSources.Add(waterSource);
    }

    public static void Remove(WaterSource waterSource)
    {
      if (!waterSources.Contains(waterSource))
        return;
      waterSources.Remove(waterSource);
    }*/

    public static void Block(ImmutableArray<Vector3Int> Coordinates)
    {
      if (Coordinates == null)
        return;
      foreach (var coordinate in Coordinates)
      {
        Debug.Log($"[WaterSourceMap.Block] coordinate={coordinate} xy={coordinate.XY()}");
        waterSourceCoordinates.Add(coordinate);
        //waterSourceCoordinatesXY.Add(coordinate.XY());
      }
    }

    public static void Unblock(ImmutableArray<Vector3Int> Coordinates)
    {
      if (Coordinates == null)
        return;
      foreach (var coordinate in Coordinates)
      {
        Debug.Log($"[WaterSourceMap.Unblock] coordinate={coordinate}  xy={coordinate.XY()}");
        //if (waterSourceCoordinatesXY.Contains(coordinate.XY()))
        //  waterSourceCoordinatesXY.Remove(coordinate.XY());
        if (waterSourceCoordinates.Contains(coordinate))
          waterSourceCoordinates.Remove(coordinate);
      }
    }

    public static bool IsBlocked(Vector3Int coordinate)
    {
      if (coordinate == null)
        return true;
      var exist = waterSourceCoordinates.Contains(coordinate);
      Debug.Log($"[WaterSourceMap.IsBlocked] coordinate={coordinate} xy={coordinate.XY()} exist={exist}");
      return exist;
    }
  }
}
