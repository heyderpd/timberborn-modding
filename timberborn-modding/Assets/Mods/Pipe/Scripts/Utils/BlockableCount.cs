using Amazon.Runtime.Internal.Transform;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using System.Collections.Generic;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class BlockableItem
  {
    public WaterObstacle obstacle { get; private set; }

    public int count;

    public BlockableItem(WaterObstacle _obstacle)
    {
      obstacle = _obstacle;
      count = 1;
    }
  }

  internal class BlockableCount
  {
    private readonly Dictionary<Vector3Int, BlockableItem> Blockers = new Dictionary<Vector3Int, BlockableItem>();

    public WaterObstacleState obstacleType;

    public BlockableCount(bool fullObstacle)
    {
      obstacleType = fullObstacle
        ? WaterObstacleState.FULL_OBSTACLE
        : WaterObstacleState.PARTIAL_OBSTACLE;
    }

    private bool IsInvalid(WaterObstacle obstacle)
    {
      if (obstacle == null)
        return false;
      return obstacleType == obstacle.getType();
    }

    public bool Block(WaterObstacle reference)
    {
      if (IsInvalid(reference))
        return false;
      if (Blockers.TryGetValue(reference.Key, out var item))
      {
        item.count += 1;
        return false;
      }
      Blockers.Add(reference.Key, new BlockableItem(reference));
      return true;
    }

    public bool Unblock(Vector3Int referenceKey)
    {
      if (referenceKey == null)
        return false;
      if (Blockers.TryGetValue(referenceKey, out var item))
      {
        item.count -= 1;
        if (item.count <= 0)
        {
          Blockers.Remove(referenceKey);
          return true;
        }
      }
      return false;
    }

    public WaterObstacle Get(Vector3Int key)
    {
      if (key == null)
        return null;
      if (Blockers.TryGetValue(key, out var item))
        return item.obstacle;
      return null;
    }

    public bool Contains(Vector3Int key)
    {
      if (key == null)
        return false;
      if (Blockers.TryGetValue(key, out var item))
        return item.count > 0;
      return false;
    }
  }
}
