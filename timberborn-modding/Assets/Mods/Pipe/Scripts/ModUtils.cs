using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.Coordinates;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class ModUtils
  {
    public static readonly bool enabled = true;

    public static readonly System.Random random = new System.Random();

    private static readonly ImmutableArray<Vector3Int> coordOffsets = ImmutableArray.Create(
      // z
      new Vector3Int(0, 0, 1),
      new Vector3Int(0, 0, -1),
      // y
      new Vector3Int(0, 1, 0),
      new Vector3Int(0, -1, 0),
      // x
      new Vector3Int(1, 0, 0),
      new Vector3Int(-1, 0, 0)
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

    private static Tuple<int, int, int, int, int, int> getRectifyRef(BlockObject block)
    {
      int xp = block.Coordinates.x;
      int yp = block.Coordinates.y;
      int zp = block.Coordinates.z;
      int xl = block.Blocks.Size.x - 1;
      int yl = block.Blocks.Size.y - 1;
      int zl = block.Blocks.Size.z - 1;
      if (block.Orientation == Orientation.Cw90 || block.Orientation == Orientation.Cw270)
      {
        int aux = xl;
        xl = yl;
        yl = aux;
      }
      if (block.FlipMode.IsFlipped)
      {
        if (block.Orientation == Orientation.Cw0 || block.Orientation == Orientation.Cw180)
          xp = xp - xl;
        else
          yp = yp - yl;
      }
      if (block.Orientation == Orientation.Cw180)
        yp = yp - yl;
      if (block.Orientation == Orientation.Cw270)
        xp = xp - xl;
      ModUtils.Log($"[TailRecursion.getRectifyRef] xp={xp} yp={yp} zp={zp} | xl={xl} yl={yl} zl={zl}");
      return new Tuple<int, int, int, int, int, int>(xp, yp, zp, xl, yl, zl);
    }

    public static IEnumerable<Vector3Int> getReflexPositions(BlockObject block)
    {
      if (block?.IsFinished != true || block.Blocks.Size.x == 0 || block.Blocks.Size.y == 0 || block.Blocks.Size.z == 0)
        yield break;
      var (xp, yp, zp, xl, yl, zl) = getRectifyRef(block);
      // near in body
      for (var x = 0; x <= xl + 0; x++)
      {
        for (var y = 0; y <= yl + 0; y++)
        {
          for (var z = 0; z <= zl + 0; z++)
          {
            yield return new Vector3Int(x + xp, y + yp, z + zp);
          }
        }
      }
      // near in axis X
      for (var y = 0; y <= yl + 0; y++)
      {
        for (var z = 0; z <= zl + 0; z++)
        {
          yield return new Vector3Int(
            xp - 1,
            y + yp,
            z + zp
          );
          yield return new Vector3Int(
            xp + xl + 1,
            y + yp,
            z + zp
          );
        }
      }
      // near in axis Y
      for (var x = 0; x <= xl + 0; x++)
      {
        for (var z = 0; z <= zl + 0; z++)
        {
          yield return new Vector3Int(
            x + xp,
            yp - 1,
            z + zp
          );
          yield return new Vector3Int(
            x + xp,
            yp + yl + 1,
            z + zp
          );
        }
      }
      // near in axis Z
      for (var x = 0; x <= xl + 0; x++)
      {
        for (var y = 0; y <= yl + 0; y++)
        {
          yield return new Vector3Int(
            x + xp,
            y + yp,
            zp - 1
          );
          yield return new Vector3Int(
            x + xp,
            y + yp,
            zp + zl + 1
          );
        }
      }
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

    public static float GetValueStep(float _value, float _valueMax, int _steps = 5)
    {
      var value = Mathf.Min(_value, _valueMax);
      value = Mathf.Max(value, 0f);
      value = value / _valueMax;
      var steps = _valueMax / _steps;
      value = (int)(value / steps);
      value = Mathf.Min(value * steps, 1f);
      value = value * _valueMax;
      ModUtils.Log($"[GetValueStep] value={_value} valueMax={_valueMax} result={value}");
      return value;
    }

    public static T GetRandomItem<T>(List<T> list)
    {
      try
      {
        int index = random.Next(list.Count());
        return list[index];
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [ModUtils.GetRandomItem] err={err}");
        return default(T);
      }
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

  internal class TailRecursion
  {
    private class Scope
    {
      private readonly PipeGroupManager pipeGroupManager;

      private readonly PipeGroupQueue pipeGroupQueue;

      private readonly HashSet<PipeGroup> groupsCreated;

      private readonly HashSet<PipeNode> nodesResolved;

      private readonly Queue<PipeNode> nodesUnresolved;

      public Scope(
        PipeGroupManager _pipeGroupManager,
        PipeGroupQueue _pipeGroupQueue,
        IEnumerable<PipeNode> _nodesUnresolved
      )
      {
        pipeGroupManager = _pipeGroupManager;
        pipeGroupQueue = _pipeGroupQueue;
        groupsCreated = new HashSet<PipeGroup>();
        nodesResolved = new HashSet<PipeNode>();
        nodesUnresolved = new Queue<PipeNode>(_nodesUnresolved);
        ModUtils.Log($"[TailRecursion.Scope] 01 _nodesUnresolved={nodesUnresolved.Count}");
      }

      public PipeNode GetNext()
      {
        if (nodesUnresolved.IsEmpty())
          return null;
        return nodesUnresolved.Dequeue();
      }

      public PipeGroup GroupCreate()
      {
        var group = pipeGroupManager.createGroup();
        groupsCreated.Add(group);
        return group;
      }

      public void ResolveNode(PipeNode node)
      {
        nodesResolved.Add(node);
      }

      public bool IsInvalidNode(PipeNode node)
      {
        return node == null || !node.isEnabled || nodesResolved.Contains(node);
      }

      public bool IsValidNode(PipeNode node)
      {
        return !IsInvalidNode(node);
      }

      public void GroupInitialize()
      {
        foreach (var group in groupsCreated)
        {
          pipeGroupQueue.Group_RecalculateGates(group);
        }
      }

      public void Clear()
      {
        groupsCreated.Clear();
        nodesUnresolved.Clear();
        nodesResolved.Clear();
      }
    }

    private static void resolveNode(
      PipeNode actualNode,
      PipeGroup group,
      ref Scope scope,
      ref Queue<PipeNode> nextNodes
    )
    {
      ModUtils.Log($"[TailRecursion.resolveNode] start actualNode={actualNode?.id}");
      if (scope.IsInvalidNode(actualNode))
        return;
      scope.ResolveNode(actualNode);
      actualNode.SetGroup(group);
      actualNode.CheckGates();
      var connectedNodes = actualNode.waterGates
        .Select((WaterGate gate) => gate.gateConnected?.pipeNode)
        .Where((PipeNode node) => node != null && node.isEnabled)
        .ToList();
      foreach (var node in connectedNodes)
      {
        if (scope.IsValidNode(node))
          nextNodes.Enqueue(node);
      }
      ModUtils.Log($"[TailRecursion.resolveNode] end");
    }

    private static void findNewGroup(
      PipeNode firstNode,
      ref Scope scope
    )
    {
      ModUtils.Log($"[TailRecursion.findNewGroup] start firstNode={firstNode?.id}");
      if (scope.IsInvalidNode(firstNode))
        return;
      var group = scope.GroupCreate();
      var nextNodes = new Queue<PipeNode>();
      resolveNode(firstNode, group, ref scope, ref nextNodes);
      while (!nextNodes.IsEmpty())
      {
        resolveNode(nextNodes.Dequeue(), group, ref scope, ref nextNodes);
      }
      ModUtils.Log($"[TailRecursion.findNewGroup] end");
    }

    public static void groupRecreate(
      PipeGroupManager pipeGroupManager,
      PipeGroupQueue pipeGroupQueue,
      PipeNode deletedNode
    )
    {
      ModUtils.Log($"[TailRecursion.groupRecreate] start");
      deletedNode.group.SetDisabled();
      var nodesUnresolved = deletedNode.group.Pipes
        .Where((PipeNode node) => node != null && node.isEnabled)
        .ToList();
      var initialNodes = deletedNode.waterGates
        .Select((WaterGate gate) => gate.gateConnected?.pipeNode)
        .Where((PipeNode node) => node != null && node.isEnabled)
        .ToList();
      ModUtils.Log($"[TailRecursion.groupRecreate] nodesUnresolved={nodesUnresolved.Count} initialNodes={initialNodes.Count}");
      var scope = new Scope(
        pipeGroupManager,
        pipeGroupQueue,
        nodesUnresolved.Union(initialNodes)
      );
      PipeNode actualNode = null;
      while ((actualNode = scope.GetNext()) != null)
      {
        findNewGroup(actualNode, ref scope);
      }
      scope.GroupInitialize();
      scope.Clear();
      pipeGroupQueue.Group_Remove(deletedNode.group);
      ModUtils.Log($"[TailRecursion.groupRecreate] end");
    }
  }
}