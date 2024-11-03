using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Timberborn.BlockSystem;
using System.Collections.Immutable;
using Moq;
using Timberborn.Common;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class ModUtils
  {
    public static readonly bool enabled = true;

    private static readonly ImmutableList<Vector3Int> coordOffsets = ImmutableList.Create(
      new Vector3Int(0, 0, 1),
      new Vector3Int(0, 0, -1),
      new Vector3Int(0, 1, 0),
      new Vector3Int(0, -1, 0),
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

  internal class TailRecursion
  {
    private class Scope
    {
      private readonly PipeGroupManager pipeGroupManager;

      private readonly PipeGroupQueue pipeGroupQueue;

      private readonly HashSet<PipeGroup> groupsCreated;

      private readonly HashSet<PipeNode> nodesUnresolved;

      private readonly HashSet<PipeNode> nodesResolved;

      private readonly Queue<PipeNode> nodeFirsts;

      public Scope(
        PipeGroupManager _pipeGroupManager,
        PipeGroupQueue _pipeGroupQueue,
        HashSet<PipeNode> _nodesUnresolved,
        List<PipeNode> _initialNodes
      )
      {
        pipeGroupManager = _pipeGroupManager;
        pipeGroupQueue = _pipeGroupQueue;
        groupsCreated = new HashSet<PipeGroup>();
        nodesUnresolved = new HashSet<PipeNode>(_nodesUnresolved);
        nodesResolved = new HashSet<PipeNode>();
        nodeFirsts = new Queue<PipeNode>(_initialNodes);
      }

      public PipeNode GetNext()
      {
        if (nodeFirsts.Count == 0 && nodesUnresolved.Count > 0)
        {
          nodesUnresolved.ExceptWith(nodesResolved);
          foreach (var node in nodesUnresolved)
          {
            nodeFirsts.Enqueue(node);
          }
          nodesUnresolved.Clear();
        }
        if (nodeFirsts.Count > 0)
          return nodeFirsts.Dequeue();
        return null;
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
        nodeFirsts.Clear();
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
      var nextNodes = deletedNode.waterGates
        .Select((WaterGate gate) => gate.gateConnected?.pipeNode)
        .Where((PipeNode node) => node != null && node.isEnabled)
        .ToList();
      var scope = new Scope(
        pipeGroupManager,
        pipeGroupQueue,
        deletedNode.group.Pipes,
        nextNodes
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
