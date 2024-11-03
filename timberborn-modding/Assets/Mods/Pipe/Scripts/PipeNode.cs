using System.Collections.Generic;
using System.Linq;
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

    private PipeGroup group = new PipeGroup();

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
      group.Add(this);
    }

    public void InitializeEntity()
    {
      coordinates = blockObject.Coordinates;
      CheckNear();
    }

    public void DeleteEntity()
    {
      isEnabled = false;
      foreach (var _gate in waterGates)
      {
        var otherGate = _gate.gateConnected;
        otherGate?.ReleaseConnection();
      }
      group?.SetChanged();
      group?.Remove(this);
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
      group.Tick();
    }

    public void SetGroup(PipeGroup _group)
    {
      group = _group;
      group.Add(this);
    }

    public bool TryConnect(WaterGate startGate, PipeNode node)
    {
      if (group.Same(node.group))
      {
        Debug.Log($"PIPE.TryConnect pipe={id} thisGroup={group.id} otherGroup={node.group.id} not_same_group");
        return true;
      }
      WaterGate endGate = node.waterGates
        .Find((WaterGate gate) => gate.coordinates.Equals(coordinates));
      if (!endGate)
      {
        Debug.Log($"PIPE.TryConnect thisPipe={id}.by_gate={startGate.id} otherPipe={node.GetInfo()} gate_not_found");
        return false;
      }
      startGate.gateConnected = endGate;
      endGate.gateConnected = startGate;
      group.UnionTo(node.group);
      startGate.SetDisabled();
      endGate.SetDisabled();
      Debug.Log($"PIPE.TryConnect thisPipe={id}.by_gate={startGate.id} otherPipe={node.id}.by_gate={endGate.id} connected");
      return true;
    }

    public void groupCheckGate(WaterGate gate)
    {
      group.CheckGate(gate);
    }

    public void CheckNear()
    {
      Debug.Log($"PIPE.CheckNear group={group.id} pipe={id} EXECUTE");
      if (!isEnabled)
        return;
      var enabled = false;
      foreach (var gate in waterGates)
      {
        var gateEnabled = gate.CheckClearInput();
        enabled = enabled || gateEnabled;
      }
      hasGatesEnabled = enabled;
      group.SetChanged();
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
      string info = $" Node[id={id}, group={group?.id}, coordinates={this.coordinates}, enabled={isEnabled} gates={waterGates.Count}:\n";
      foreach (var gate in waterGates)
      {
        info += gate.GetInfo();
      }
      info += " ]\n";
      return info;
    }

    public string GetFragmentInfo()
    {
      return group.GetInfo();
    }
  }
}
