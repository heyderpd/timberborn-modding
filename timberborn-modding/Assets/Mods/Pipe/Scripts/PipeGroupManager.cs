using System.Linq;
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

    private readonly List<WaterGate> GroupRecreateGates = new List<WaterGate>();

    private readonly PipeGroupChangeDebounce<BlockObject> debounce_gateCheckByBlockEvent = new PipeGroupChangeDebounce<BlockObject>(PipeGroupChangeTypes.GATE_CHECK_BY_BLOCKEVENT);

    private readonly PipeGroupChangeDebounce<PipeGroup> debounce_groupRecalculateGates = new PipeGroupChangeDebounce<PipeGroup>(PipeGroupChangeTypes.GROUP_RECALCULATE_GATES);

    private bool working = false;

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

    private bool _GroupNotExist(PipeGroup group)
    {
      ModUtils.Log($"[Manager._GroupNotExist] group={group?.id} groupC1={group == null} groupC2={group?.isEnabled != true} groupC3={!Groups.Contains(group)}");
      return group == null || group?.isEnabled != true || !Groups.Contains(group);
    }
    
    private PipeGroup _createGroup()
    {
      var group = new PipeGroup(pipeGroupQueue);
      Groups.Add(group);
      return group;
    }

    private void _GroupRecreateTailRecursion(
      PipeNode actualNode,
      ref Queue<PipeNode> pipeWorkList,
      ref HashSet<PipeNode> resolvedNode,
      ref HashSet<PipeGroup> createdGroups,
      PipeGroup group = null
    )
    {
      if (actualNode  != null && actualNode.isEnabled && !resolvedNode.Contains(actualNode))
      {
        if (group == null)
        {
          group = _createGroup();
          createdGroups.Add(group);
        }
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
      _GroupRecreateTailRecursion(nextNode, ref pipeWorkList, ref resolvedNode, ref createdGroups, group);
    }

    private void _GroupRecreate(PipeNode deletedNode)
    {
      deletedNode.group.SetDisabled();
      var pipeWorkList = new Queue<PipeNode>();
      var resolvedNode = new HashSet<PipeNode>();
      var createdGroups = new HashSet<PipeGroup>();
      var startNodes = deletedNode.waterGates
        .Select((WaterGate gate) => gate.gateConnected?.pipeNode)
        .Where((PipeNode node) => node != null && node.isEnabled)
        .ToList();
      deletedNode.ReleaseConnections();
      if (startNodes.Count == 0)
        return;
      foreach (var node in startNodes)
      {
        _GroupRecreateTailRecursion(node, ref pipeWorkList, ref resolvedNode, ref createdGroups);
      }
      deletedNode.group.Pipes.ExceptWith(resolvedNode);
      var forgottenPipes = new Queue<PipeNode>(deletedNode.group.Pipes);
      while (!forgottenPipes.IsEmpty())
      {
        var nextNode = forgottenPipes.Dequeue();
        if (nextNode.isEnabled)
          _GroupRecreateTailRecursion(nextNode, ref pipeWorkList, ref resolvedNode, ref createdGroups);
      }
      foreach (var group in createdGroups)
      {
        pipeGroupQueue.Group_RecalculateGates(group);
      }
      createdGroups.Clear();
      pipeWorkList.Clear();
      resolvedNode.Clear();
      pipeGroupQueue.Group_Remove(deletedNode.group);
    }

    private void Action_Group_RecalculateGates()
    {
      ModUtils.Log($"[Manager.Action_Group_RecalculateGates] count={debounce_groupRecalculateGates.Count} DOING");
      if (debounce_groupRecalculateGates.IsEmpty)
        return;
      foreach (var group in debounce_groupRecalculateGates.Items)
      {
        if (_GroupNotExist(group))
          continue;
        ModUtils.Log($"[Manager.Action_Group_RecalculateGates] group={group.id} LOOP");
        group.recalculateGates();
      }
      debounce_groupRecalculateGates.Clear();
      ModUtils.Log($"[Manager.Action_Group_RecalculateGates] DONE");
    }

    private void Action_Group_Remove(PipeGroupChange change)
    {
      change.group?.Destroy();
      Groups.Remove(change.group);
    }

    private void Action_Pipe_Join(PipeGroupChange change)
    {
      var groupA = change.node?.group;
      var groupB = change.secondNode?.group;
      if (_GroupNotExist(groupA) || _GroupNotExist(groupB))
        return;
      if (groupA.Pipes.Count > groupB.Pipes.Count)
        groupB.UnionTo(groupA);
      else
        groupA.UnionTo(groupB);
    }

    private void Action_Pipe_Create(PipeGroupChange change)
    {
      var pipe = change.node;
      var group = _createGroup();
      pipe.SetGroup(group);
      pipe.SetEnabled();
      pipe.CheckGates();
    }

    private void Action_Pipe_Remove(PipeGroupChange change)
    {
      if (_GroupNotExist(change.node.group))
        return;
      change.node.group.PipeRemove(change.node);
      _GroupRecreate(change.node);
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
            debounce_groupRecalculateGates.Store(change.type, change.node?.group ?? change.group);
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
      pipeGroupQueue.Gate_CheckByBlockEvent(blockEvent?.BlockObject);
    }

    [OnEvent]
    public void OnBlockObjectUnset(BlockObjectUnsetEvent blockEvent)
    {
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
