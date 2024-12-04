using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterSourceSystem;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldService
  {
    private WaterRadar waterRadar;

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

    public ShieldNode DiscoveryWaterColumn(Vector2Int reference, int height, BlockObject block)
    {
      var Coordinates = new List<Vector3Int>();
      for (var z = height - 1; z >= 0; z--)
      {
        coordinate = block.Transform(new Vector3Int(reference.x, reference.y, z));
        if (waterRadar.IsInvalidCoordinate(coordinate))
          continue;
        Coordinates.Add(coordinate);
      }
      if (Coordinates.Count > 0)
        return new ShieldNode(Coordinates);
      return null;
    }

    public IEnumerable<ShieldNode> DiscoveryShieldField(int totalLimit, int height, BlockObject block)
    {
      var Coordinates = new List<ShieldNode>();
      var (x_ref, y_ref) = ModUtils.getRectifyRef(block);
      var reference = new Vector2Int(x_ref + 1, y_ref + 1);
      var total = 0;
      var move = 0;
      var moveLimit = -1;
      var direction = 3;
      while (total < totalLimit)
      {
        var column = DiscoveryWaterColumn(reference, height, block);
        if (column != null)
          yield return column;
        move++;
        total++;
        if (move > moveLimit)
        {
          move = 0;
          direction++;
          direction = direction > 3 ? 0 : direction;
          if (direction == 2 || direction == 0)
            moveLimit++;
        }
        if (direction == 0)
          reference.x++;
        else if (direction == 1)
          reference.y++;
        else if (direction == 2)
          reference.x--;
        else
          reference.y--;
      }
    }
  }
}
