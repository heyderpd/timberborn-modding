using System.Collections.Immutable;
using System.Collections.Generic;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class ShieldNode
  {
    private ImmutableArray<Vector3Int> Coordinates;

    public bool Active => added;

    public bool Inactive => !added;

    private bool added = false;

    public ShieldNode(List<Vector3Int> _Coordinates)
    {
      Coordinates = _Coordinates.ToImmutableArray();
    }

    public void SetActive(WaterRadar waterRadar)
    {
      if (added)
        return;
      foreach(var coordinate in Coordinates)
      {
        waterRadar.AddFullObstacle(coordinate);
      }
      added = true;
    }

    public void SetInactive(WaterRadar waterRadar)
    {
      if (!added)
        return;
      foreach (var coordinate in Coordinates)
      {
        waterRadar.RemoveFullObstacle(coordinate);
      }
      added = false;
    }
  }
}
