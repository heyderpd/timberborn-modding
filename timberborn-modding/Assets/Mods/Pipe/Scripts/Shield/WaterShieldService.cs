using System.Collections.Generic;
using System.Collections.Immutable;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterSourceSystem;
using System.Drawing;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldService
  {
    private WaterRadar waterRadar;

    private BlockObject blockObject;

    private Vector3Int coordinate;

    private int height;

    private int size;

    [Inject]
    public void InjectDependencies(
      WaterRadar _waterRadar
    )
    {
      waterRadar = _waterRadar;
    }

    public WaterSource GetWaterSource(Vector3Int coordinate)
    {
      var block = waterRadar.GetMiddleObjectAt(coordinate);
      var waterSource = block?.GetComponentFast<WaterSource>() ?? null;
      return waterSource;
    }

    public ImmutableArray<Vector3Int> DiscoveryShieldField(int _buildLength, int _size, int height, BlockObject block)
    {
      var (x_ref, y_ref) = ModUtils.getRectifyRef(block);
      var size = _size * _buildLength;
      var initial = new Vector3Int(x_ref - _buildLength, y_ref - _buildLength);
      var Coordinates = new List<Vector3Int>();
      for (var z = 0; z < height; z++)
      {
        for (var y = 0; y < size; y++)
        {
          for (var x = 0; x < size; x++)
          {
            coordinate = blockObject.Transform(initial + new Vector3Int(x, y, z));
            if (waterRadar.IsInvalidCoordinate(coordinate) || GetWaterSource(coordinate) != null)
              continue;
            Coordinates.Add(coordinate);
          }
        }
      }
      return Coordinates.ToImmutableArray();
    }
  }
}
