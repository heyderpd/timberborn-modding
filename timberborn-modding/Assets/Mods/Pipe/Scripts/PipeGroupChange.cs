
using System.Collections.Generic;
using Timberborn.Common;
using UnityEngine;

namespace Mods.Pipe.Scripts
{
  public enum PipeGroupChangeTypes
  {
    CHANGED,
    ADD,
    REMOVE,
    CHECK_PIPE,
    CHECK_GATE,
    MIGRATE
  }

  internal readonly struct PipeGroupChange
  {
    public readonly PipeGroup group;

    public readonly PipeNode node;

    public readonly WaterGate gate;

    public readonly PipeGroupChangeTypes type;

    public PipeGroupChange(
      PipeGroup _group,
      PipeNode _node,
      WaterGate _gate,
      PipeGroupChangeTypes _type
    )
    {
      group = _group;
      node = _node;
      gate = _gate;
      type = _type;
    }
  }

  internal class PipeGroupQueue
  {
    private readonly PipeGroup group;

    private readonly Queue<PipeGroupChange> changes = new Queue<PipeGroupChange>();

    private bool HasChanges => !changes.IsEmpty();

    public PipeGroupQueue(PipeGroup _group)
    {
      group = _group;
    }

    public void AddChanged()
    {
      changes.Enqueue(new PipeGroupChange(null, null, null, PipeGroupChangeTypes.CHANGED));
    }

    public void AddNode(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(null, node, null, PipeGroupChangeTypes.ADD));
    }

    public void RemoveNode(PipeNode node)
    {
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(null, node, null, PipeGroupChangeTypes.REMOVE));
    }
    
    public void CheckPipe(PipeNode node)
    {
      Debug.Log($"CHANGE.CheckPipe group={group.id} pipe={node.id} ADD");
      if (node == null)
        return;
      changes.Enqueue(new PipeGroupChange(null, node, null, PipeGroupChangeTypes.CHECK_PIPE));
    }

    public void CheckGate(WaterGate gate)
    {
      if (gate == null)
        return;
      changes.Enqueue(new PipeGroupChange(null, null, gate, PipeGroupChangeTypes.CHECK_GATE));
    }

    public void MigrateAndClear(PipeGroup _group)
    {
      if (_group == null)
        return;
      changes.Enqueue(new PipeGroupChange(_group, null, null, PipeGroupChangeTypes.MIGRATE));
    }

    public bool ConsumeChanges(HashSet<PipeNode> nodes)
    {
      if (!HasChanges)
        return false;
      while (HasChanges)
      {
        PipeGroupChange change = changes.Dequeue();
        switch (change.type)
        {
          case PipeGroupChangeTypes.CHANGED:
            break;

          case PipeGroupChangeTypes.ADD:
            nodes.Add(change.node);
            break;

          case PipeGroupChangeTypes.REMOVE:
            nodes.Remove(change.node);
            break;

          case PipeGroupChangeTypes.CHECK_PIPE:
            Debug.Log($"CHANGE.CheckPipe group={group.id} pipe={change.node.id} CALL");
            change.node.CheckNear();
            break;

          case PipeGroupChangeTypes.CHECK_GATE:
            change.gate.CheckClearInput();
            break;

          case PipeGroupChangeTypes.MIGRATE:
            foreach (var node in nodes)
            {
              node.SetGroup(change.group);
            }
            group.Clear();
            changes.Clear();
            return false;

          default:
            break;
        }
      }
      return true;
    }
  }
}
