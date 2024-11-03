using System.Linq;
using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.Persistence;
using Timberborn.EntitySystem;
using Timberborn.BlockSystem;
using Timberborn.BaseComponentSystem;

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
      ModUtils.Log($"[PIPE.InitializeEntity] pipe={id}");
      coordinates = blockObject.Coordinates;
    }

    public void DeleteEntity()
    {
      ModUtils.Log($"[PIPE.DeleteEntity] pipe={id}");
      isEnabled = false;
      pipeGroupQueue.Pipe_Remove(this);
    }

    public void Save(IEntitySaver entitySaver) { }

    public void Load(IEntityLoader entityLoader) { }

    public void OnEnterFinishedState()
    {
      ((Behaviour)this).enabled = true;
      pipeGroupQueue.Pipe_Create(this);
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
      ResetFlow();
    }

    public void Disconnection()
    {
      foreach (var gate in waterGates)
      {
        gate.Disconnection();
      }
    }

    public bool TryConnect(WaterGate startGate, PipeNode node)
    {
      if (node == null)
      {
        ModUtils.Log($"[PIPE.TryConnect] pipe={id} thisGroup={group?.id} node_is_null");
        return false;
      }
      if (!node.isEnabled)
      {
        ModUtils.Log($"[PIPE.TryConnect] pipe={id} thisGroup={group?.id} otherGroup={node.group?.id} node_disabled");
        return false;
      }
      var block = node.GetComponentFast<BlockObject>();
      if (block == null || block?.IsFinished == false)
      {
        ModUtils.Log($"[PIPE.TryConnect] pipe={id} thisGroup={group?.id} otherGroup={node.group?.id} blockIsNull={block == null} block_not_finished");
        return false;
      }
      WaterGate endGate = node.GetGate(coordinates);
      bool IsCompatibleGate = endGate
        ? WaterGateConfig.IsCompatibleGate(startGate.Side, endGate.Side)
        : false;
      if (!endGate || !IsCompatibleGate)
      {
        ModUtils.Log($"[PIPE.TryConnect] thisPipe={id}.by_gate={startGate.id} coordinates={coordinates} endGate={endGate?.id} IsCompatibleGate={IsCompatibleGate} startGate={startGate.GetInfo()} otherPipe={node.GetInfo()} NOT_MATCH");
        return false;
      }
      ModUtils.Log($"[PIPE.TryConnect] thisPipe={id}.by_gate={startGate.id} startGate={startGate.GetInfo()} endGate={endGate.GetInfo()} MATCH");
      if (startGate.CheckConnection(endGate))
      {
        ModUtils.Log($"[PIPE.TryConnect] thisPipe={id}.by_gate={startGate.id} otherPipe={node.id}.by_gate={endGate.id} same_connection");
        return true;
      }
      startGate.ConnectionBoth(endGate);
      pipeGroupQueue.Pipe_Join(node, this);
      ModUtils.Log($"[PIPE.TryConnect] thisPipe={id}.by_gate={startGate.id} otherPipe={node.id}.by_gate={endGate.id} connected");
      return true;
    }

    public bool CheckGates(bool recalculate = true)
    {
      if (!isEnabled)
        return false;
      var _hasChanges = false;
      var _hasGatesEnabled = false;
      foreach (var gate in waterGates)
      {
        var gateChanged = gate.CheckInput();
        _hasChanges = _hasChanges || gateChanged;
        _hasGatesEnabled = _hasGatesEnabled || gate.isEnabled;
      }
      hasGatesEnabled = _hasGatesEnabled;
      if (recalculate && _hasChanges)
        pipeGroupQueue.Group_RecalculateGates(this);
      return _hasChanges;
    }

    public void ResetFlow()
    {
      foreach (var gate in waterGates)
      {
        gate.ResetFlow();
      }
    }

    public WaterGate GetGate(Vector3Int coordinates)
    {
      if (!isEnabled || coordinates == null)
        return null;
      WaterGate gate = waterGates
        .FirstOrDefault((WaterGate gate) =>
          ModUtils.IsEqual(gate.coordinates, coordinates));
      return gate;
    }

    public WaterGate GetGate(BlockObject block)
    {
      if (!isEnabled || block?.IsFinished == false || ModUtils.IsFar(coordinates, block.Coordinates))
        return null;
      return GetGate(block.Coordinates);
    }

    public string GetInfo()
    {
      string info = $"Node[node={id} enabled={isEnabled}:\n";
      foreach (var gate in waterGates)
      {
        info += gate.GetInfo();
      }
      info += "]\n";
      return info;
    }

    public void SetGateValue(float value)
    {
      var gate = waterGates.FirstOrDefault();
      if (gate != null)
        gate.HigthLimit = value;
    }

    public float GetGateValue()
    {
      var gate = waterGates.FirstOrDefault();
      if (gate != null)
        return gate.HigthLimit;
      return -1f;
    }

    public List<GateVisualElement> GetFragmentInfo()
    {
      return waterGates
        .Select((WaterGate gate) => new GateVisualElement(gate))
        .ToList();
      /*var gate = waterGates.FirstOrDefault();
      return $"gate.HigthLimit={gate?.Water.ToString("0.00")}";
      string info = GetInfo();
      info += group?.GetInfo() ?? "NO_GROUP";
      return info;*/
    }
  }
}
