using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.SingletonSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using System.Linq;
using Moq;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeNode : BaseComponent,
                            IInitializableEntity,
                            IDeletableEntity,
                            IFinishedStateListener,
                            IPersistentEntity
  {
    public bool isEnabled { get; private set; } = false;

    public bool hasGatesEnabled { get; private set; } = false;

    private static int lastId = 0;

    public readonly int id = lastId++;

    private BlockObject blockObject;

    private BlockService blockService;

    private PipeGroupQueue pipeGroupQueue;

    public PipeGroup group { get; private set; }

    public List<WaterGate> waterGates { get; private set; }

    public Vector3Int coordinates { get; private set; }

    [Inject]
    public void InjectDependencies(
      BlockService _blockService,
      PipeGroupQueue _pipeGroupQueue
    )
    {
      blockService = _blockService;
      pipeGroupQueue = _pipeGroupQueue;
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
      OldGopherLog.Log($"[PIPE.InitializeEntity] pipe={id}");
      coordinates = blockObject.Coordinates;
    }

    public void DeleteEntity()
    {
      OldGopherLog.Log($"[PIPE.DeleteEntity] pipe={id}");
      isEnabled = false;
      pipeGroupQueue.PipeNodeRemove(group, this);
    }

    public void Save(IEntitySaver entitySaver) { }

    public void Load(IEntityLoader entityLoader) { }

    public void OnEnterFinishedState()
    {
      ((Behaviour)this).enabled = true;
      pipeGroupQueue.PipeNodeCreate(this);
    }

    public void OnExitFinishedState()
    {
      ((Behaviour)this).enabled = false;
    }

    public void SetEnabled()
    {
      isEnabled = true;
    }

    public void SetGroup(PipeGroup _group)
    {
      group = _group;
      group.PipeAdd(this);
    }

    public void ReleaseConnections()
    {
      foreach (var gate in waterGates)
      {
        gate.ReleaseConnection();
      }
    }

    public bool TryConnect(WaterGate startGate, PipeNode node)
    {
      if (node == null)
      {
        OldGopherLog.Log($"[PIPE.TryConnect] pipe={id} thisGroup={group?.id} node_is_null");
        return false;
      }
      if (group.Same(node?.group))
      {
        OldGopherLog.Log($"[PIPE.TryConnect] pipe={id} thisGroup={group?.id} otherGroup={node.group?.id} is_same_group");
        return true;
      }
      if (!node.isEnabled)
      {
        OldGopherLog.Log($"[PIPE.TryConnect] pipe={id} thisGroup={group?.id} otherGroup={node.group?.id} node_disabled");
        return false;
      }
      WaterGate endGate = node.waterGates
        .Find((WaterGate gate) => 
          WaterGateConfig.IsCompatibleGate(gate.Side, gate.Side)
          && gate.coordinates.Equals(coordinates));
      if (!endGate)
      {
        OldGopherLog.Log($"[PIPE.TryConnect] thisPipe={id}.by_gate={startGate.id} otherPipe={node.GetInfo()} gate_not_found");
        return false;
      }
      startGate.SetConnection(endGate);
      endGate.SetConnection(startGate);
      pipeGroupQueue.PipeNodeJoin(node, this);
      OldGopherLog.Log($"[PIPE.TryConnect] thisPipe={id}.by_gate={startGate.id} otherPipe={node.id}.by_gate={endGate.id} connected");
      return true;
    }

    public void CheckGates(bool recalculate = true)
    {
      if (!isEnabled)
        return;
      var _hasChanges = false;
      var _hasGatesEnabled = false;
      foreach (var gate in waterGates)
      {
        var gateChanged = gate.CheckInput(recalculate: false);
        _hasChanges = _hasChanges || gateChanged;
        _hasGatesEnabled = _hasGatesEnabled || gate.isEnabled;
      }
      hasGatesEnabled = _hasGatesEnabled;
      if (recalculate && _hasChanges)
        pipeGroupQueue.GroupRecalculateGates(this);
    }

    private bool IsFar(BlockObject block)
    {
      var blockCoordinates = block.Coordinates;
      var distance = Vector3Int.Distance(blockCoordinates, coordinates);
      var far = distance != 1f;
      return far;
    }

    public void WaterGateCheckInput(BlockObject block)
    {
      if (!isEnabled || block == null || block?.IsFinished == false || IsFar(block))
        return;
      WaterGate gate = waterGates
        .FirstOrDefault((WaterGate gate) =>
          gate.coordinates.Equals(block.Coordinates));
      if (gate == null)
        return;
      gate.CheckInput();
    }

    public string GetInfo()
    {
      string info = $" Node[id={id}, group={group?.id}, coordinates={coordinates}, enabled={isEnabled}, gates={waterGates.Count}:\n";
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
    }
  }
}
