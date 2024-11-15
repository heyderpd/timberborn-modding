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