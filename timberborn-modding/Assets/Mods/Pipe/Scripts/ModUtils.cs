using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Timberborn.BlockSystem;
using System.Collections.Immutable;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class ModUtils
  {
    public static readonly bool enabled = false;

    private static readonly ImmutableList<Vector3Int> coordOffsets = ImmutableList.Create(
      new Vector3Int( 0,  0,  1),
      new Vector3Int( 0,  0, -1),
      new Vector3Int( 0,  1,  0),
      new Vector3Int( 0, -1,  0),
      new Vector3Int( 1,  0,  0),
      new Vector3Int(-1,  0,  0)
    );

    public static void Log(string text)
    {
      if (enabled)
        Debug.Log(text);
    }

    public static bool IsFar(Vector3Int origin, Vector3Int destine)
    {
      if (origin == null || destine == null)
        return true;
      int disX = origin.x - destine.x;
      if (disX >= 1 && -1 >= disX)
        return true;
      int disY = origin.y - destine.y;
      if (disY >= 1 && -1 >= disY)
        return true;
      int disZ = origin.z - destine.z;
      if (disZ >= 1 && -1 >= disZ)
        return true;
      int distanceSum = Mathf.Abs(disX) + Mathf.Abs(disY) + Mathf.Abs(disZ);
      if (distanceSum != 1)
        return true;
      return false;
    }

    public static bool IsEqual(Vector3Int origin, Vector3Int destine)
    {
      if (origin == null || destine == null)
        return false;
      if (origin.x != destine.x)
        return false;
      if (origin.y != destine.y)
        return false;
      if (origin.z != destine.z)
        return false;
      return true;
    }

    public static HashSet<WaterGate> getNearWaterGates(BlockService blockService, BlockObject block)
    {
      if (block?.IsFinished == false)
        return null;
      HashSet<WaterGate> Gates = new HashSet<WaterGate>();
      foreach (var offset in coordOffsets)
      {
        var coordinates = block.Transform(offset);
        var pipe = blockService.GetObjectsWithComponentAt<PipeNode>(coordinates).FirstOrDefault();
        if (pipe?.isEnabled == false)
          continue;
        var gate = pipe?.GetGate(block.Coordinates);
        if (gate == null)
          continue;
        Gates.Add(gate);
      }
      if (Gates.Count == 0)
        return null;
      return Gates;
    }
  }

  internal class TickCount
  {
    private int ticks = 0;

    private int maxTicks;

    public TickCount(int maxTicks = 0)
    {
      SetMaxTicks(maxTicks);
    }

    public void SetMaxTicks(int _maxTicks)
    {
      maxTicks = _maxTicks;
    }

    public bool Skip()
    {
      if (maxTicks == 0)
        return false;
      ticks += 1;
      if (ticks >= maxTicks)
      {
        ticks = 0;
        return false;
      }
      return true;
    }
  }

  internal static class TimerControl
  {
    private static float nextTime = 0f;

    private static float fixedDeltaTime = Time.fixedDeltaTime; // = 0.6

    public static bool Skip()
    {
      float now = Time.fixedTime;
      if (now < nextTime)
        return true;
      nextTime = now + fixedDeltaTime;
      return false;
    }
  }
}
