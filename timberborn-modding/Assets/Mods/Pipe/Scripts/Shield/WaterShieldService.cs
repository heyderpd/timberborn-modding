using System.Collections.Generic;
using System.Collections.Immutable;
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

    public ImmutableArray<ShieldNode> DiscoveryShieldField(int size, int height, BlockObject block)
    {
      Debug.Log("DiscoveryShieldField start");
      var Coordinates = new List<ShieldNode>();
      var (x_ref, y_ref) = ModUtils.getRectifyRef(block);
      var reference = new Vector2Int(x_ref, y_ref);
      Debug.Log($"DiscoveryShieldField start reference={reference}");
      var totalLimit = size * size * height;
      var total = 0;
      var move = 0;
      var moveLimit = -1;
      var direction = 3;
      while (total < totalLimit)
      {
        //for (var z = 0; z < height; z++)
        for (var z = height - 1; z >= 0; z--)
        {
          coordinate = block.Transform(new Vector3Int(reference.x, reference.y, z));
          if (waterRadar.IsInvalidCoordinate(coordinate) || WaterSourceMap.IsBlocked(coordinate))
            continue;
          Debug.Log($"DiscoveryShieldField coordinate={coordinate} will");
          Coordinates.Add(new ShieldNode(coordinate));
        }
        move++;
        total++;
        Debug.Log($"DiscoveryShieldField loop A move={move} moveLimit={moveLimit} direction={direction}");
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
        Debug.Log($"DiscoveryShieldField loop B move={move} moveLimit={moveLimit} direction={direction}");
      }
      Debug.Log("DiscoveryShieldField end");
      return Coordinates.ToImmutableArray();
    }
  }
}
