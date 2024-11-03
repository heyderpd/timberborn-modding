
using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  public enum PipeGroupChangeTypes
  {
    GROUP_RECALCULATE_GATES,
    PIPE_CREATE,
    PIPE_REMOVE,
    PIPE_JOIN,
    PIPE_CHECK_CHANGES,
    PIPE_CHECK_GATES,
    GATE_CHECK_INPUT
  }

  internal readonly struct PipeGroupChange
  {
    public readonly PipeGroup group;

    public readonly PipeNode node;

    public readonly PipeNode secondNode;

    public readonly BlockObject blockObject;

    public readonly WaterGate gate;

    public readonly PipeGroupChangeTypes type;
    
    public PipeGroupChange(
      PipeGroupChangeTypes _type,
      PipeGroup _group = null,
      PipeNode _node = null,
      PipeNode _secondNode = null,
      BlockObject _blockObject = null,
      WaterGate _gate = null
    )
    {
      type = _type;
      group = _group;
      node = _node;
      secondNode = _secondNode;
      blockObject = _blockObject;
      gate = _gate;
    }
  }
  
  internal class PipeGroupQueue
  {
    private readonly Queue<PipeGroupChange> changes = new Queue<PipeGroupChange>();

    public bool HasChanges => !changes.IsEmpty();

    public PipeGroupChange Dequeue()
    {
      return changes.Dequeue();
    }

    public void GroupRecalculeGates(PipeGroup group)
    {
      if (group == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GROUP_RECALCULATE_GATES,
        _group: group
      ));
    }

    public void GroupRecalculateGates(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GROUP_RECALCULATE_GATES,
        _node: node
      ));
    }

    public void PipeNodeJoin(PipeNode node, PipeNode secondNode)
    {
      if (node == null || secondNode == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_JOIN,
        _node: node,
        _secondNode: secondNode
      ));
    }

    public void PipeNodeCreate(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_CREATE,
        _node: node
      ));
    }

    public void PipeNodeRemove(PipeGroup group, PipeNode node)
    {
      if (group == null || node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_REMOVE,
        _group: group,
        _node: node
      ));
    }

    public void PipeNodeCheckGates(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_CHECK_GATES,
        _node: node
      ));
    }

    public void PipeNodeCheckChanges(BlockObject blockObject)
    {
      if (blockObject == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_CHECK_CHANGES,
        _blockObject: blockObject
      ));
    }

    public void WaterGateCheckInput(WaterGate gate)
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
