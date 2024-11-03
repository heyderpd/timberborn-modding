using System.Collections.Generic;
using System.Linq;
using Bindito.Core;
using Timberborn.Common;
using Timberborn.TickSystem;
using Timberborn.SingletonSystem;
using UnityEngine;
using Timberborn.BlockSystem;
using UnityEngine.UIElements;
using Moq;

namespace Mods.Pipe.Scripts
{
  internal class PipeGroupManager : ITickableSingleton, ILoadableSingleton
  {
    private readonly HashSet<PipeGroup> Groups = new HashSet<PipeGroup>();

    private readonly List<WaterGate> GroupRecreateGates = new List<WaterGate>();

    private bool working = false;

    private EventBus eventBus;

    private PipeGroupQueue pipeGroupQueue;

    [Inject]
    public void InjectDependencies(
      EventBus _eventBus,
      PipeGroupQueue _pipeGroupQueue
    )
    {
      eventBus = _eventBus;
      pipeGroupQueue = _pipeGroupQueue;
    }

    public void Load()
    {
      Debug.Log($"[eventBus.Load] WILL DO");
      eventBus.Register(this);
    }

    private bool GroupNotExist(PipeGroup group)
    {
      return group != null && !group.isEnabled && !Groups.Contains(group);
    }

    private void GroupRecreateTailRecursion(
      PipeNode actualNode,
      ref Queue<PipeNode> pipeWorkList,
      ref HashSet<PipeNode> resolvedNode,
      PipeGroup group = null
    )
    {
      if (actualNode  != null && actualNode.isEnabled && !resolvedNode.Contains(actualNode))
      {
        if (group == null)
        {
          group = new PipeGroup(pipeGroupQueue);
          pipeGroupQueue.GroupRecalculeGates(group);
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
      GroupRecreateTailRecursion(nextNode, ref pipeWorkList, ref resolvedNode, group);
    }

    private void GroupRecreate(PipeNode deletedNode)
    {
      deletedNode.group.SetDisabled();
      var pipeWorkList = new Queue<PipeNode>();
      var resolvedNode = new HashSet<PipeNode>();
      var startNodes = deletedNode.waterGates
        .Select((WaterGate gate) => gate.gateConnected?.pipeNode)
        .Where((PipeNode node) => node != null && node.isEnabled)
        .ToList();
      deletedNode.ReleaseConnections();
      if (startNodes.Count == 0)
        return;
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

    private void ActionGroupRecalculateGates(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionGroupRecalculeGates] DOING group={change.group?.id ?? change.node?.group?.id}  node={change.node?.id}");
      var group = change.node != null ? change.node.group : change.group;
      if (GroupNotExist(group))
        return;
      group.recalculateGates();
    }

    private void ActionPipeJoin(PipeGroupChange change)
    {
      var groupA = change.node?.group;
      var groupB = change.secondNode?.group;
      Debug.Log($"[Manager.ActionPipeJoin] DOING group={change.group?.id} node={change.node.id} otherGroup={groupB?.id}");
      if (GroupNotExist(groupA) || GroupNotExist(groupB))
        return;
      if (groupA.Pipes.Count > groupB.Pipes.Count)
        groupB.UnionTo(groupA);
      else
        groupA.UnionTo(groupB);
    }

    private void ActionPipeNodeCreate(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionPipeNodeCreate] DOING group={change.group?.id} node={change.node.id}");
      var pipe = change.node;
      var group = new PipeGroup(pipeGroupQueue);
      pipe.SetGroup(group);
      Groups.Add(group);
      pipe.SetEnabled();
      pipe.CheckGates(recalculate: false);
      pipeGroupQueue.GroupRecalculeGates(group);
    }

    private void ActionPipeNodeRemove(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionPipeNodeRemove] DOING group={change.group?.id} node={change.node.id}");
      if (GroupNotExist(change.group))
        return;
      change.group.PipeRemove(change.node);
      GroupRecreate(change.node);
    }

    private void ActionPipeNodeCheckChanges(PipeGroupChange change)
    {
      // TODO: talvez quando eu receber varias vezes os eventos. eu deveria dar skip nos outros em um loop de consume? para em um loop processar isso somente uma unica vez?
      var now = Time.fixedTime;
      Debug.Log($"[Manager.ActionPipeNodeCheckChanges] DOING Time.fixedTime={Time.fixedTime}");
      foreach (var group in Groups)
      {
        foreach (var pipe in group.Pipes)
        {
          pipe.WaterGateCheckInput(change.blockObject);
        }
      }
      Debug.Log($"[Manager.ActionPipeNodeCheckChanges] DONE Time.fixedTime={Time.fixedTime} time={now - Time.fixedTime}");
    }

    private void ActionPipeNodeCheckGates(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionPipeNodeCheckGates] DOING group={change.group?.id} node={change.node.id}");
      change.node.CheckGates();
    }

    private void ActionGateCheckInput(PipeGroupChange change)
    {
      Debug.Log($"[Manager.ActionGateCheckInput] DOING group={change.gate?.pipeNode.group?.id} node={change.gate?.pipeNode.id} gate={change.gate.id}");
      change.gate.CheckInput();
    }

    public bool ConsumeChanges()
    {
      if (!pipeGroupQueue.HasChanges)
        return false;
      while (pipeGroupQueue.HasChanges)
      {
        PipeGroupChange change = pipeGroupQueue.Dequeue();
        switch (change.type)
        {
          case PipeGroupChangeTypes.GROUP_RECALCULATE_GATES:
            ActionGroupRecalculateGates(change);
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

          case PipeGroupChangeTypes.PIPE_CHECK_CHANGES:
            ActionPipeNodeCheckChanges(change);
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

    [OnEvent]
    public void OnBlockObjectSet(BlockObjectSetEvent blockEvent)
    {
      pipeGroupQueue.PipeNodeCheckChanges(blockEvent?.BlockObject);
    }

    [OnEvent]
    public void OnBlockObjectUnset(BlockObjectUnsetEvent blockEvent)
    {
      pipeGroupQueue.PipeNodeCheckChanges(blockEvent?.BlockObject);
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
