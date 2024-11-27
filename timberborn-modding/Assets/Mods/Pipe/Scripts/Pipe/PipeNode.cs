using System.Linq;
using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BlockSystem;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  internal class PipeNode : BaseComponent,
                            IInitializableEntity,
                            IDeletableEntity,
                            IFinishedStateListener
  {
    public bool isEnabled { get; private set; } = false;

    public bool hasGatesEnabled { get; private set; } = false;

    [SerializeField]
    public bool canWorkAlone;

    [SerializeField]
    public bool canBeaver = false;

    private static int lastId = 0;

    public readonly int id = lastId++;

    private BlockObject blockObject;

    private BlockService blockService;

    private PipeGroupQueue pipeGroupQueue;

    private PipeBeaver Beaver;

    public PipeGroup group { get; private set; }

    public List<WaterGate> waterGates { get; private set; }

    public Vector3Int coordinates { get; private set; }

    public bool IsWaterPump => waterGates?.Any((WaterGate gate) => gate.IsWaterPump) ?? false;

    public bool CanWork => group?.HasMoreThanOnePipe ?? canWorkAlone;

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
      Beaver = GetComponentFast<PipeBeaver>();
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

    public (bool?, bool?) GetWaterPumpState()
    {
      if (!IsWaterPump)
        return (null, null);
      var gate = waterGates.FirstOrDefault((WaterGate gate) => gate.IsWaterPump);
      if (!gate)
        return (null, null);
      return (gate.Mode == WaterGateMode.ONLY_IN, gate.powered.Active);
    }

    public void ToggleWaterPumpMode()
    {
      if (!IsWaterPump || waterGates == null)
        return;
      var gate = waterGates.FirstOrDefault(gate => gate.IsWaterPump);
      if (!gate)
        return;
      gate.ToggleWaterPumpMode();
      pipeGroupQueue.Group_RecalculateGates(this);
    }

    public void ToggleWaterPumpPower()
    {
      if (!IsWaterPump || waterGates == null)
        return;
      var gate = waterGates.FirstOrDefault(gate => gate.IsWaterPump);
      if (!gate)
        return;
      gate.ToggleWaterPumpPower();
    }

    public void WildBeaverAppears()
    {
      if (waterGates == null || !canBeaver)
        return;
      Beaver?.WildBeaverAppears(waterGates.FirstOrDefault(gate => gate.Type == WaterGateType.TOP));
    }

    public void TestParticle()
    {
      if (waterGates == null)
        return;
      WildBeaverAppears();
      foreach (var gate in waterGates)
      {
        gate.TestParticle();
      }
    }

    public void DisablePowerConsumption()
    {
      foreach (var gate in waterGates)
      {
        gate.powered?.DisablePowerConsumption();
      }
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
        return false;
      if (!node.isEnabled)
        return false;
      var block = node.GetComponentFast<BlockObject>();
      if (block == null || block?.IsFinished == false)
        return false;
      WaterGate endGate = node.GetGate(coordinates);
      bool IsCompatibleGate = endGate
        ? WaterGateConfig.IsCompatibleGate(startGate.Type, endGate.Type)
        : false;
      if (!endGate || !IsCompatibleGate)
        return false;
      if (startGate.CheckConnection(endGate))
        return true;
      startGate.ConnectionBoth(endGate);
      pipeGroupQueue.Pipe_Join(node, this);
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
      if (!CanWork)
        hasGatesEnabled = false;
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

    public WaterGate GetGate(WaterGateType type)
    {
      WaterGate gate = waterGates
        .FirstOrDefault((WaterGate gate) => gate.Type == type);
      return gate;
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

    public float GetGateValue()
    {
      var gate = waterGates.FirstOrDefault();
      if (gate != null)
        return gate.HigthLimit;
      return -1f;
    }

    public string GetFragmentInfo()
    {
      string info = $"Pipe[\n";
      info += $"  id={id} enabled={isEnabled} cord={coordinates.ToString()}\n";
      info += $"  gates:\n";
      foreach (var gate in waterGates.ToList())
      {
        info += gate.GetInfo();
      }
      info += $"];\n";
      info += $"group:\n";
      info += group?.GetInfo() ?? "NONE";
      return info;
    }
  }
}
