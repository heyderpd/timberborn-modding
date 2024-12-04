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
        waterSourceCoordinates.Add(coordinate);
      }
    }

    public static void Unblock(ImmutableArray<Vector3Int> Coordinates)
    {
      if (Coordinates == null)
        return;
      foreach (var coordinate in Coordinates)
      {
        if (waterSourceCoordinates.Contains(coordinate))
          waterSourceCoordinates.Remove(coordinate);
      }
    }

    public static bool IsBlocked(Vector3Int coordinate)
    {
      if (coordinate == null)
        return true;
      var exist = waterSourceCoordinates.Contains(coordinate);
      return exist;
    }
  }
}
