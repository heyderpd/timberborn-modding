
using System.Collections.Generic;
using Timberborn.Common;
using Timberborn.BlockSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  public enum PipeGroupChangeTypes
  {
    GROUP_RECALCULATE_GATES,
    GROUP_REMOVE,
    PIPE_CREATE,
    PIPE_REMOVE,
    PIPE_JOIN,
    GATE_CHECK_BY_BLOCKEVENT,
    PIPE_CHECK_GATES,
    GATE_CHECK
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

  internal class PipeGroupChangeDebounce<T>
  {
    private PipeGroupChangeTypes type;

    public HashSet<T> Items { get; private set; } = new HashSet<T>();

    public int Count => Items.Count;

    public bool IsEmpty => Items.Count == 0;

    public PipeGroupChangeDebounce(PipeGroupChangeTypes _type)
    {
      type = _type;
    }

    public void Clear()
    {
      Items.Clear();
    }

    public void Store(PipeGroupChangeTypes _type, T item)
    {
      if (type != _type)
        return;
      if (item == null)
        return;
      Items.Add(item);
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

    public void Group_RecalculateGates(PipeGroup group)
    {
      if (group == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GROUP_RECALCULATE_GATES,
        _group: group
      ));
    }

    public void Group_RecalculateGates(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GROUP_RECALCULATE_GATES,
        _node: node
      ));
    }

    public void Group_Remove(PipeGroup group)
    {
      if (group == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GROUP_REMOVE,
        _group: group
      ));
    }

    public void Pipe_Join(PipeNode node, PipeNode secondNode)
    {
      if (node == null || secondNode == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_JOIN,
        _node: node,
        _secondNode: secondNode
      ));
    }

    public void Pipe_Create(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_CREATE,
        _node: node
      ));
    }

    public void Pipe_Remove(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_REMOVE,
        _node: node
      ));
    }

    public void Pipe_CheckGates(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.PIPE_CHECK_GATES,
        _node: node
      ));
    }

    public void Gate_CheckByBlockEvent(BlockObject blockObject)
    {
      if (blockObject == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GATE_CHECK_BY_BLOCKEVENT,
        _blockObject: blockObject
      ));
    }

    public void Gate_Check(WaterGate gate)
    {
      if (gate == null)
        return;
      changes.Enqueue(new PipeGroupChange(
        _type: PipeGroupChangeTypes.GATE_CHECK,
        _gate: gate
      ));
    }
  }
}
