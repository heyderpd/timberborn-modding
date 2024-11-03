using System.Collections.Generic;
using Bindito.Core;
using Timberborn.Common;
using Timberborn.TickSystem;
using Timberborn.BlockSystem;
using Timberborn.SingletonSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeGroupManager : ITickableSingleton, ILoadableSingleton
  {
    private readonly HashSet<PipeGroup> Groups = new HashSet<PipeGroup>();

    private readonly PipeGroupChangeDebounce<BlockObject> debounce_gateCheckByBlockEvent = new PipeGroupChangeDebounce<BlockObject>(PipeGroupChangeTypes.GATE_CHECK_BY_BLOCKEVENT);

    private readonly PipeGroupChangeDebounce<PipeGroup> debounce_groupRecalculateGates = new PipeGroupChangeDebounce<PipeGroup>(PipeGroupChangeTypes.GROUP_RECALCULATE_GATES);

    private static bool working = false;

    private EventBus eventBus;

    private BlockService blockService;

    private PipeGroupQueue pipeGroupQueue;

    [Inject]
    public void InjectDependencies(
      EventBus _eventBus,
      BlockService _blockService,
      PipeGroupQueue _pipeGroupQueue
    )
    {
      eventBus = _eventBus;
      blockService = _blockService;
      pipeGroupQueue = _pipeGroupQueue;
    }

    public void Load()
    {
      eventBus.Register(this);
    }

    private bool GroupNotExist(PipeGroup group)
    {
      ModUtils.Log($"[Manager._GroupNotExist] group={group?.id} groupC1={group == null} groupC2={group?.isEnabled != true} groupC3={!Groups.Contains(group)}");
      return group == null || group?.isEnabled != true || !Groups.Contains(group);
    }
    
    public PipeGroup createGroup()
    {
      var group = new PipeGroup(pipeGroupQueue);
      Groups.Add(group);
      return group;
    }

    private void Action_Group_RecalculateGates()
    {
      ModUtils.Log($"[Manager.Action_Group_RecalculateGates] count={debounce_groupRecalculateGates.Count} DOING");
      if (debounce_groupRecalculateGates.IsEmpty)
        return;
      foreach (var group in debounce_groupRecalculateGates.Items)
      {
        if (GroupNotExist(group))
          continue;
        ModUtils.Log($"[Manager.Action_Group_RecalculateGates] group={group.id} LOOP");
        group.recalculateGates();
      }
      debounce_groupRecalculateGates.Clear();
      ModUtils.Log($"[Manager.Action_Group_RecalculateGates] DONE");
    }

    private void Action_Group_Remove(PipeGroupChange change)
    {
      if (change.group == null)
        return;
      change.group.Destroy();
      Groups.Remove(change.group);
    }

    private void Action_Pipe_Join(PipeGroupChange change)
    {
      var groupA = change.node?.group;
      var groupB = change.secondNode?.group;
      if (GroupNotExist(groupA) || GroupNotExist(groupB))
        return;
      if (groupA == groupB)
        return;
      if (groupA.Pipes.Count > groupB.Pipes.Count)
        groupB.UnionTo(groupA);
      else
        groupA.UnionTo(groupB);
    }

    private void Action_Pipe_Create(PipeGroupChange change)
    {
      var pipe = change.node;
      var group = createGroup();
      pipe.SetGroup(group);
      pipe.SetEnabled();
      pipe.CheckGates();
    }

    private void Action_Pipe_Remove(PipeGroupChange change)
    {
      if (GroupNotExist(change.node.group))
        return;
      change.node.group.PipeRemove(change.node);
      TailRecursion.groupRecreate(this, pipeGroupQueue, change.node);
      change.node.Disconnection();
    }

    private void Action_Pipe_CheckGates(PipeGroupChange change)
    {
      change.node?.CheckGates();
    }

    private void Action_Gate_Check_ByBlockEvent()
    {
      ModUtils.Log($"[Manager.Action_Gate_Check_ByBlockEvent] count={debounce_gateCheckByBlockEvent.Count} DOING");
      if (debounce_gateCheckByBlockEvent.IsEmpty)
        return;
      HashSet<WaterGate> Gates = new HashSet<WaterGate>();
      foreach (var block in debounce_gateCheckByBlockEvent.Items)
      {
        if (block?.IsFinished == false)
          continue;
        var _gates = ModUtils.getNearWaterGates(blockService, block);
        if (_gates != null)
          Gates.AddRange(_gates);
      }
      ModUtils.Log($"[Manager.Action_Gate_Check_ByBlockEvent] Gates={Gates.Count} COUNT");
      if (Gates.Count > 0)
      {
        foreach (var gate in Gates)
        {
          ModUtils.Log($"[Manager.Action_Gate_Check_ByBlockEvent] gate={gate.id} LOOP");
          var changed = gate.CheckInput();
          if (changed)
            pipeGroupQueue.Group_RecalculateGates(gate.pipeNode);
        }
      }
      Gates.Clear();
      debounce_gateCheckByBlockEvent.Clear();
      ModUtils.Log($"[Manager.Action_Gate_Check_ByBlockEvent] DONE");
    }

    private void Action_Gate_Check(PipeGroupChange change)
    {
      var changed = change.gate?.CheckInput() ?? false;
      if (changed)
        pipeGroupQueue.Group_RecalculateGates(change.gate.pipeNode?.group);
    }

    private bool ConsumeChanges()
    {
      debounce_groupRecalculateGates.Clear();
      debounce_gateCheckByBlockEvent.Clear();
      if (!pipeGroupQueue.HasChanges)
        return false;
      while (pipeGroupQueue.HasChanges)
      {
        PipeGroupChange change = pipeGroupQueue.Dequeue();
        switch (change.type)
        {
          case PipeGroupChangeTypes.GROUP_RECALCULATE_GATES:
            debounce_groupRecalculateGates.Store(change.type, change.node?.group != null ? change.node.group : change.group);
            break;

          case PipeGroupChangeTypes.GROUP_REMOVE:
            Action_Group_Remove(change);
            break;

          case PipeGroupChangeTypes.PIPE_CREATE:
            Action_Pipe_Create(change);
            break;

          case PipeGroupChangeTypes.PIPE_REMOVE:
            Action_Pipe_Remove(change);
            break;

          case PipeGroupChangeTypes.PIPE_JOIN:
            Action_Pipe_Join(change);
            break;

          case PipeGroupChangeTypes.PIPE_CHECK_GATES:
            Action_Pipe_CheckGates(change);
            break;

          case PipeGroupChangeTypes.GATE_CHECK_BY_BLOCKEVENT:
            debounce_gateCheckByBlockEvent.Store(change.type, change.blockObject);
            break;

          case PipeGroupChangeTypes.GATE_CHECK:
            Action_Gate_Check(change);
            break;

          default:
            break;
        }
      }
      Action_Group_RecalculateGates();
      Action_Gate_Check_ByBlockEvent();
      return true;
    }

    [OnEvent]
    public void OnBlockObjectSet(BlockObjectSetEvent blockEvent)
    {
      ModUtils.Log($"[Manager.OnBlockObjectSet] called IsFinished={blockEvent?.BlockObject?.IsFinished} size={blockEvent?.BlockObject?.Blocks?.Size}");
      pipeGroupQueue.Gate_CheckByBlockEvent(blockEvent?.BlockObject);
    }

    [OnEvent]
    public void OnBlockObjectUnset(BlockObjectUnsetEvent blockEvent)
    {
      ModUtils.Log($"[Manager.OnBlockObjectUnset] called IsFinished={blockEvent?.BlockObject?.IsFinished}");
      pipeGroupQueue.Gate_CheckByBlockEvent(blockEvent?.BlockObject);
    }

    private void DoMoveWater()
    {
      foreach (var group in Groups)
      {
        group.DoMoveWater();
      }
    }

    public void Tick()
    {
      if (working)
        return;
      working = true;
      ConsumeChanges();
      DoMoveWater();
      working = false;
    }
  }
}
