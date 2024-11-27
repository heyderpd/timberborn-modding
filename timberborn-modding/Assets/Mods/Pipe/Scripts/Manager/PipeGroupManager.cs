using System;
using System.Linq;
using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.TickSystem;
using Timberborn.BlockSystem;
using Timberborn.SingletonSystem;

namespace Mods.OldGopher.Pipe
{
  internal class PipeGroupManager : ITickableSingleton, ILoadableSingleton
  {
    private readonly HashSet<PipeGroup> Groups = new HashSet<PipeGroup>();

    private readonly Dictionary<Vector3Int, PipeNode> PipeLocation = new Dictionary<Vector3Int, PipeNode>();

    private readonly PipeGroupChangeDebounce<Vector3Int> debounce_pipeHandleEvents = new PipeGroupChangeDebounce<Vector3Int>(PipeGroupChangeTypes.GATE_CHECK_BY_BLOCKEVENT);

    private readonly PipeGroupChangeDebounce<PipeGroup> debounce_groupRecalculateGates = new PipeGroupChangeDebounce<PipeGroup>(PipeGroupChangeTypes.GROUP_RECALCULATE_GATES);

    private static bool working = false;

    private EventBus eventBus;

    private BlockService blockService;

    private PipeGroupQueue pipeGroupQueue;

    private TickCount beaverTick = new TickCount(60);

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
      if (debounce_groupRecalculateGates.IsEmpty)
        return;
      foreach (var group in debounce_groupRecalculateGates.Items)
      {
        if (GroupNotExist(group))
          continue;
        group.recalculateGates();
      }
      debounce_groupRecalculateGates.Clear();
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
      pipe.DisablePowerConsumption();
      if (PipeLocation.ContainsKey(pipe.coordinates))
        PipeLocation.Remove(pipe.coordinates);
      PipeLocation.Add(pipe.coordinates, pipe);
    }

    private void Action_Pipe_Remove(PipeGroupChange change)
    {
      if (GroupNotExist(change.node.group))
        return;
      var pipe = change.node;
      if (PipeLocation.TryGetValue(pipe.coordinates, out var exist))
      {
        if (exist == pipe)
          PipeLocation.Remove(pipe.coordinates);
      }
      change.node.group.PipeRemove(pipe);
      TailRecursion.groupRecreate(this, pipeGroupQueue, pipe);
      change.node.Disconnection();
    }

    private void Action_Pipe_CheckGates(PipeGroupChange change)
    {
      change.node?.CheckGates();
    }

    private void Action_Pipe_HandleEvents()
    {
      if (debounce_pipeHandleEvents.IsEmpty)
        return;
      HashSet<WaterGate> Gates = new HashSet<WaterGate>();
      foreach (var coordinate in debounce_pipeHandleEvents.Items)
      {
        if (PipeLocation.TryGetValue(coordinate, out var pipe))
        {
          pipe.CheckGates();
        }
      }
      debounce_pipeHandleEvents.Clear();
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
      debounce_pipeHandleEvents.Clear();
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
            Translate_Pipe_HandleEvents(change);
            break;

          case PipeGroupChangeTypes.GATE_CHECK:
            Action_Gate_Check(change);
            break;

          default:
            break;
        }
      }
      Action_Pipe_HandleEvents();
      Action_Group_RecalculateGates();
      return true;
    }

    private void Translate_Pipe_HandleEvents(PipeGroupChange change)
    {
      if (change.blockObject?.IsFinished != true)
        return;
      foreach (var pos in ModUtils.getReflexPositions(change.blockObject))
      {
        debounce_pipeHandleEvents.Store(change.type, pos);
      }
    }

    [OnEvent]
    public void OnBlockObjectSet(EnteredFinishedStateEvent blockEvent)
    {
      ModUtils.Log($"[Manager.OnBlockObjectSet] IsFinished={blockEvent?.BlockObject?.IsFinished}");
      if (blockEvent?.BlockObject?.IsFinished == true)
        pipeGroupQueue.Gate_CheckByBlockEvent(blockEvent?.BlockObject);
    }

    [OnEvent]
    public void OnBlockObjectUnset(BlockObjectUnsetEvent blockEvent)
    {
      ModUtils.Log($"[Manager.OnBlockObjectUnset] IsFinished={blockEvent?.BlockObject?.IsFinished}");
      if (blockEvent?.BlockObject?.IsFinished == true)
        pipeGroupQueue.Gate_CheckByBlockEvent(blockEvent?.BlockObject);
    }

    private void DoMoveWater()
    {
      foreach (var group in Groups)
      {
        group.DoMoveWater();
      }
    }

    private void ShowPipeBeaver()
    {
      try
      {
        if (!beaverTick.Skip())
          return;
        if (!PipeBeaver.GetRandomChance())
          return;
        var group = ModUtils.GetRandomItem<PipeGroup>(Groups.Where(group => group.PipesCount == 1).ToList());
        if (group == null)
          return;
        var pipe = ModUtils.GetRandomItem<PipeNode>(group.Pipes.Where(pipe => pipe.canBeaver).ToList());
        if (pipe == null)
          return;
        pipe.WildBeaverAppears();
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [Manager.ShowPipeBeaver] err={err}");
      }
    }

    public void Tick()
    {
      if (working)
        return;
      working = true;
      ConsumeChanges();
      DoMoveWater();
      ShowPipeBeaver();
      working = false;
    }
  }
}
