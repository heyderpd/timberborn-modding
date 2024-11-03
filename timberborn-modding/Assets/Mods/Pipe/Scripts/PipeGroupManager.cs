using Moq;
using System.Collections.Generic;
using System.Linq;
using Timberborn.Common;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Mods.Pipe.Scripts
{
  internal static class PipeGroupManager
  {
    private static readonly HashSet<PipeGroup> Groups = new HashSet<PipeGroup>();

    private static TickCount nodesTick = new TickCount();

    private static bool GroupNotExist(PipeGroup group)
    {
      return group != null && Groups.Contains(group);
    }

    private static void UpdatePipeNodeCount()
    {
      int PipeNodeCount = Groups
        .Where((PipeGroup group) => group.isEnabled)
        .Aggregate(
          0,
          (int count, PipeGroup group) =>
          {
            count += group.Pipes
              .Where((PipeNode node) => node.isEnabled)
              .Count();
            return count;
          }
        );
      nodesTick.SetMaxTicks(PipeNodeCount);
    }

    public static void ActionGroupRecalculeGates(PipeGroupChange change)
    {
      if (GroupNotExist(change.group))
        return;
      change.group.recalculeGates();
    }

    public static void ActionPipeJoin(PipeGroupChange change)
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

    public static void ActionPipeNodeCreate(PipeGroupChange change)
    {
      var pipe = change.node;
      var group = new PipeGroup();
      pipe.SetGroup(group);
      Groups.Add(group);
      UpdatePipeNodeCount();
    }

    public static void ActionPipeNodeRemove(PipeGroupChange change)
    {
      if (GroupNotExist(change.group))
        return;
      change.group.PipeRemove(change.node);
      UpdatePipeNodeCount();
    }

    public static void ActionPipeNodeCheckGates(PipeGroupChange change) {
      change.node.CheckGates();
    }

    public static void ActionGateCheckInput(PipeGroupChange change)
    {
      change.gate.CheckInput();
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

          case PipeGroupChangeTypes.PIPE_JOIN:
            ActionPipeJoin(change);
            break;

          case PipeGroupChangeTypes.PIPE_CREATE:
            ActionPipeNodeCreate(change);
            break;

          case PipeGroupChangeTypes.PIPE_REMOVE:
            ActionPipeNodeRemove(change);
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

    public static void Tick()
    {
      if (nodesTick.Skip())
        return;
      ConsumeChanges();
      DoMoveWater();
    }
  }
}
