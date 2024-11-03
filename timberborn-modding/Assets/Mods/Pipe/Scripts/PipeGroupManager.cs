using System.Collections.Generic;
using System.Linq;
using Timberborn.Common;
using UnityEngine;

namespace Mods.Pipe.Scripts
{
  internal static class PipeGroupManager
  {
    private static readonly HashSet<PipeGroup> Groups = new HashSet<PipeGroup>();

    private static readonly List<WaterGate> GroupRecreateGates = new List<WaterGate>();

    private static bool working = false;

    private static bool GroupNotExist(PipeGroup group)
    {
      return group != null && !Groups.Contains(group);
    }

    private static void GroupRecreateTailRecursion(
      PipeNode actualNode,
      ref Queue<PipeNode> pipeWorkList,
      ref HashSet<PipeNode> resolvedNode,
      PipeGroup group = null
    )
    {
      if (actualNode  != null && actualNode.isEnabled && !resolvedNode.Contains(actualNode))
      {
        if (group == null)
          group = new PipeGroup();
          PipeGroupQueue.GroupRecalculeGates(group);
        actualNode.SetGroup(group);
        resolvedNode.Add(actualNode);
        var connectedNodes = actualNode.waterGates
          .Select((WaterGate gate) => gate.gateConnected?.pipeNode)
          .Where((PipeNode node) => node != null && node.isEnabled)
          .ToList();
        foreach (var node in connectedNodes)
        {
          if (!resolvedNode.Contains(node))
            pipeWorkList.Enqueue(node);
        }
      }
      if (pipeWorkList.IsEmpty())
        return;
      var nextNode = pipeWorkList.Dequeue();
      GroupRecreateTailRecursion(nextNode, ref pipeWorkList, ref resolvedNode, group);
    }

    private static void GroupRecreate(PipeNode deletedNode)
    {
      deletedNode.group.SetDisabled();
      var pipeWorkList = new Queue<PipeNode>();
      var resolvedNode = new HashSet<PipeNode>();
      var startNodes = deletedNode.waterGates
        .Select((WaterGate gate) => gate.gateConnected?.pipeNode)
        .Where((PipeNode node) => node != null && node.isEnabled)
        .ToList();
      foreach (var node in startNodes)
      {
        GroupRecreateTailRecursion(node, ref pipeWorkList, ref resolvedNode);
      }
      deletedNode.group.Pipes.ExceptWith(resolvedNode);
      var forgottenPipes = new Queue<PipeNode>(deletedNode.group.Pipes);
      while (!forgottenPipes.IsEmpty())
      {
        var nextNode = forgottenPipes.Dequeue();
        if (nextNode.isEnabled)
          GroupRecreateTailRecursion(nextNode, ref pipeWorkList, ref resolvedNode);
      }
      pipeWorkList.Clear();
      resolvedNode.Clear();
      deletedNode.group.Clear();
    }

    private static void ActionGroupRecalculeGates(PipeGroupChange change)
    {
      if (GroupNotExist(change.group))
        return;
      change.group.recalculeGates();
    }

    private static void ActionPipeJoin(PipeGroupChange change)
    {
      var groupA = change.node?.group;
      var groupB = change.secondNode?.group;
      if (GroupNotExist(groupA) || GroupNotExist(groupB))
        return;
      if (groupA.Pipes.Count > groupB.Pipes.Count)
        groupB.UnionTo(groupA);
      else
        groupA.UnionTo(groupB);
    }

    private static void ActionPipeNodeCreate(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionPipeNodeCreate] node={change.node.id} start");
      var pipe = change.node;
      var group = new PipeGroup();
      pipe.SetGroup(group);
      Groups.Add(group);
      pipe.CheckGates();
      Debug.Log($"[Manager.ActionPipeNodeCheckGates] node={change.node.id} end");
    }

    private static void ActionPipeNodeRemove(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionPipeNodeRemove] node={change.node.id} start");
      if (GroupNotExist(change.group))
        return;
      change.group.PipeRemove(change.node);
      GroupRecreate(change.node);
      Debug.Log($"[Manager.ActionPipeNodeRemove] node={change.node.id} end");
    }

    private static void ActionPipeNodeCheckGates(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionPipeNodeCheckGates] node={change.node.id} start");
      change.node.CheckGates();
      Debug.Log($"[Manager.ActionPipeNodeCheckGates] node={change.node.id} end");
    }

    private static void ActionGateCheckInput(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionGateCheckInput] gate={change.gate.id} start");
      change.gate.CheckInput();
      Debug.Log($"[Manager.ActionGateCheckInput] gate={change.gate.id} end");
    }

    public static bool ConsumeChanges()
    {
      if (!PipeGroupQueue.HasChanges)
        return false;
      while (PipeGroupQueue.HasChanges)
      {
        PipeGroupChange change = PipeGroupQueue.Dequeue();
        switch (change.type)
        {
          case PipeGroupChangeTypes.GROUP_RECALCULE_GATES:
            ActionGroupRecalculeGates(change);
            break;

          case PipeGroupChangeTypes.PIPE_CREATE:
            ActionPipeNodeCreate(change);
            break;

          case PipeGroupChangeTypes.PIPE_REMOVE:
            ActionPipeNodeRemove(change);
            break;

          case PipeGroupChangeTypes.PIPE_JOIN:
            ActionPipeJoin(change);
            break;

          case PipeGroupChangeTypes.PIPE_CHECK_GATES:
            ActionPipeNodeCheckGates(change);
            break;

          case PipeGroupChangeTypes.GATE_CHECK_INPUT:
            ActionGateCheckInput(change);
            break;

          default:
            break;
        }
      }
      return true;
    }

    private static void DoMoveWater()
    {
      foreach (var group in Groups)
      {
        group.DoMoveWater();
      }
    }

    public static void Tick(PipeGroup group)
    {
      Debug.Log($"*** Time.fixedDeltaTime={Time.fixedDeltaTime}");
      Debug.Log($"[Manager.Tick] working={working} try");
      if (working || TimerControl.Skip())
        return;
      Debug.Log($"[Manager.Tick] start vvv");
      working = true;
      ConsumeChanges();
      DoMoveWater();
      working = false;
      Debug.Log($"[Manager.Tick] end ^^^");
    }
  }
}
