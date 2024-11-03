using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.SingletonSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;

namespace Mods.Pipe.Scripts
{
  internal class PipeNode : TickableComponent,
                            IInitializableEntity,
                            IDeletableEntity,
                            IFinishedStateListener,
                            IPersistentEntity
  {
    public bool isEnabled { get; private set; } = true;

    public bool hasGatesEnabled { get; private set; } = false;

    private static int lastId = 0;

    public readonly int id = lastId++;

    private EventBus eventBus;

    private BlockObject blockObject;

    private BlockService blockService;

    public PipeGroup group { get; private set; }

    public List<WaterGate> waterGates { get; private set; }

    public Vector3Int coordinates { get; private set; }

    [Inject]
    public void InjectDependencies(
      BlockService _blockService,
      EventBus _eventBus
    )
    {
      blockService = _blockService;
      eventBus = _eventBus;
    }

    public void Awake()
    {
      ((Behaviour)this).enabled = false;
      waterGates = new List<WaterGate>();
      GetComponentsFast<WaterGate>(waterGates);
      blockObject = GetComponentFast<BlockObject>();
    }

    public void InitializeEntity()
    {
      coordinates = blockObject.Coordinates;
      PipeGroupQueue.PipeNodeCreate(this);
      //PipeGroupQueue.PipeNodeCheckGates(this);
    }

    public void DeleteEntity()
    {
      isEnabled = false;
      PipeGroupQueue.PipeNodeRemove(group, this);
    }

    public void Save(IEntitySaver entitySaver) { }

    public void Load(IEntityLoader entityLoader) { }

    public void OnEnterFinishedState()
    {
      ((Behaviour)this).enabled = true;
      eventBus.Register(this);
    }

    public void OnExitFinishedState()
    {
      ((Behaviour)this).enabled = false;
      eventBus.Unregister(this);
    }

    public override void Tick()
    {
      if (!isEnabled)
        return;
      PipeGroupManager.Tick(this.group);
    }

    public void SetGroup(PipeGroup _group)
    {
      group = _group;
      group.PipeAdd(this);
    }

    public bool TryConnect(WaterGate startGate, PipeNode node)
    {
      if (group.Same(node?.group))
      {
        Debug.Log($"PIPE.TryConnect pipe={id} thisGroup={group.id} otherGroup={node.group.id} not_same_group");
        return true;
      }
      if (!node.isEnabled)
      {
        Debug.Log($"PIPE.TryConnect pipe={id} thisGroup={group.id} otherGroup={node.group.id} node_disabled");
        return true;
      }
      WaterGate endGate = node.waterGates
        .Find((WaterGate gate) => 
          WaterGateConfig.IsCompatibleGate(gate.waterGateSide, gate.waterGateSide)
          && gate.coordinates.Equals(coordinates));
      if (!endGate)
      {
        Debug.Log($"PIPE.TryConnect thisPipe={id}.by_gate={startGate.id} otherPipe={node.GetInfo()} gate_not_found");
        return false;
      }
      startGate.SetDisabled();
      endGate.SetDisabled();
      startGate.gateConnected = endGate;
      endGate.gateConnected = startGate;
      PipeGroupQueue.PipeNodeJoin(node, this);
      Debug.Log($"PIPE.TryConnect thisPipe={id}.by_gate={startGate.id} otherPipe={node.id}.by_gate={endGate.id} connected");
      return true;
    }

    public void CheckGates()
    {
      if (!isEnabled)
        return;
      var enabled = false;
      foreach (var gate in waterGates)
      {
        var gateEnabled = gate.CheckInput();
        enabled = enabled || gateEnabled;
      }
      hasGatesEnabled = enabled;
      PipeGroupQueue.GroupRecalculeGates(group);
    }

    private bool IsFar(BlockObject block)
    {
      if (block == null)
        return true;
      var blockCoordinates = block.Coordinates;
      var distance = Vector3Int.Distance(blockCoordinates, coordinates);
      var far = distance != 1f;
      return far;
    }

    [OnEvent]
    public void OnBlockObjectSet(BlockObjectSetEvent blockObjectSetEvent)
    {
      if (IsFar(blockObjectSetEvent?.BlockObject))
        return;
      //group.CheckPipe(this);
    }

    [OnEvent]
    public void OnBlockObjectUnset(BlockObjectUnsetEvent blockObjectUnsetEvent)
    {
      if (IsFar(blockObjectUnsetEvent?.BlockObject))
        return;
      //group.CheckPipe(this);
    }

    public string GetInfo()
    {
      string info = $" Node[id={id}, group={group?.id}, coordinates={this.coordinates}, enabled={isEnabled}, gates={waterGates.Count}:\n";
      foreach (var gate in waterGates)
      {
        info += gate.GetInfo();
      }
      info += " ]\n";
      return info;
    }

    public string GetFragmentInfo()
    {
      if (group == null)
        return "no group";
      return group.GetInfo();
      //return GetInfo() + group.GetInfo();
    }
  }
}
