
using System.Collections.Generic;
using Timberborn.Common;
using UnityEditor.Graphs;

namespace Mods.Pipe.Scripts
{
  public enum PipeGroupChangeTypes
  {
    GROUP_RECALCULE_GATES,
    PIPE_CREATE,
    PIPE_REMOVE,
    PIPE_JOIN,
    PIPE_CHECK_GATES,
    GATE_CHECK_INPUT
  }

  internal readonly struct PipeGroupChange
  {
    public readonly PipeGroup group;

    public readonly PipeNode node;

    public readonly PipeNode secondNode;

    public readonly WaterGate gate;

    public readonly PipeGroupChangeTypes type;

    public PipeGroupChange(
      PipeGroupChangeTypes _type,
      PipeGroup _group = null,
      PipeNode _node = null,
      PipeNode _secondNode = null,
      WaterGate _gate = null
    )
    {
      type = _type;
      group = _group;
      node = _node;
      secondNode = _secondNode;
      gate = _gate;
    }
  }
  
  internal static class PipeGroupQueue
  {
    private static readonly Queue<PipeGroupChange> changes = new Queue<PipeGroupChange>();

    public static bool HasChanges => !changes.IsEmpty();

    public static PipeGroupChange Dequeue()
    {
      return changes.Dequeue(); ;
    }

    public static void GroupRecalculeGates(PipeGroup group)
    {
      if (group == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GROUP_RECALCULE_GATES,
        _group: group
      ));
    }

    public static void PipeNodeJoin(PipeNode node, PipeNode secondNode)
    {
      if (node == null || secondNode == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_JOIN,
        _node: node,
        _secondNode: secondNode
      ));
    }

    public static void PipeNodeCreate(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_CREATE,
        _node: node
      ));
    }

    public static void PipeNodeRemove(PipeGroup group, PipeNode node)
    {
      if (group == null || node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_REMOVE,
        _group: group,
        _node: node
      ));
    }

    public static void PipeNodeCheckGates(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_CHECK_GATES,
        _node: node
      ));
    }

    public static void WaterGateCheckInput(WaterGate gate)
    {
      if (gate == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GATE_CHECK_INPUT,
        _gate: gate
      ));
    }
  }
}
