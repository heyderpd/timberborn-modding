using System;
using System.Linq;
using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.TerrainSystem;
using Timberborn.Localization;

namespace Mods.Pipe.Scripts
{
  internal class WaterGate : BaseComponent,
                             IInitializableEntity,
                             IDeletableEntity
  {
    private static int lastId = 0;

    public readonly int id = lastId++;

    [SerializeField]
    private Vector3Int waterCoordinates;

    [SerializeField]
    public WaterGateSide Side;

    private WaterGateState State;

    private WaterGateFlow Flow = WaterGateFlow.STOP;
    
    public PipeNode pipeNode { get; private set; }

    public WaterGate gateConnected { get; private set; }

    public TickCount tick = new TickCount(5);

    private BlockObject blockObject;

    public Vector3Int coordinates { get; private set; }

    private ITerrainService terrainService;

    private BlockService blockService;

    private IWaterService waterService;

    private IThreadSafeWaterMap threadSafeWaterMap;

    private PipeGroupQueue pipeGroupQueue;

    public float Floor { get; private set; }

    public float FloorOffset { get; private set; }

    public float WaterLevel { get; private set; }

    public float Water { get; private set; }

    public float DesiredWater;

    public float ContaminationPercentage { get; private set; }

    public bool internalGateEnabled { get; private set; } = true;

    [Inject]
    public void InjectDependencies(
      ITerrainService _terrainService,
      IWaterService _waterService,
      IThreadSafeWaterMap _threadSafeWaterMap,
      BlockService _blockService,
      PipeGroupQueue _pipeGroupQueue
    )
    {
      terrainService = _terrainService;
      blockService = _blockService;
      waterService = _waterService;
      threadSafeWaterMap = _threadSafeWaterMap;
      pipeGroupQueue = _pipeGroupQueue;
    }

    public void Awake()
    {
      blockObject = GetComponentFast<BlockObject>();
      pipeNode = GetComponentFast<PipeNode>();
    }

    public void InitializeEntity()
    {
      coordinates = blockObject.Transform(waterCoordinates);
      FloorOffset = WaterGateConfig.getFloorShift(Side);
      Floor = (float)(coordinates.z) + FloorOffset;
    }

    public void DeleteEntity() { }

    public bool isEnabled
    {
      get
      {
        if (pipeNode == null)
          return false;
        return pipeNode.isEnabled && gateConnected == null && State == WaterGateState.EMPTY;
      }
    }

    private bool IsUnderwater()
    {
      try
      {
        if (threadSafeWaterMap == null || !isEnabled)
          return false;
        var underwater = threadSafeWaterMap.CellIsUnderwater(coordinates);
        return underwater;
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR [WaterGate.IsUnderwater] id={id} err={err}");
        return false;
      }
    }

    public bool UpdateWaters()
    {
      try
      {
        if (!isEnabled || !IsUnderwater())
        {
          WaterLevel = 0f;
          Water = 0f;
          ContaminationPercentage = 0f;
          return true;
        }
        WaterLevel = threadSafeWaterMap.WaterHeightOrFloor(coordinates);
        Water = Mathf.Max(WaterLevel - Floor, 0f);
        if (Water > 0f)
          ContaminationPercentage = threadSafeWaterMap.ColumnContamination(coordinates);
        else
          ContaminationPercentage = 0f;
        DesiredWater = Water;
        return true;
      } catch (Exception err)
      {
        Debug.Log($"#ERROR [WaterGate.UpdateWaters] id={id} err={err}");
        return false;
      }
    }

    public void SetDisabled()
    {
      internalGateEnabled = false;
    }

    public void SetConnection(WaterGate gate)
    {
      SetDisabled();
      gateConnected = gate;
    }

    public void UnsetConnection()
    {
      gateConnected = null;
      pipeGroupQueue.WaterGateCheckInput(this);
    }

    public void ReleaseConnection()
    {
      gateConnected?.UnsetConnection();
      gateConnected = null;
    }

    private bool notHasEmptySpace()
    {
      if (terrainService.Underground(coordinates))
      {
        return true;
      }
      var obstacle = blockService.GetObjectsWithComponentAt<WaterObstacle>(coordinates).FirstOrDefault();
      return obstacle != null;
    }

    public bool CheckInput(bool recalculate = true)
    {
      var oldState = State;
      _CheckInput();
      var changed = State == WaterGateState.CONNECTED || oldState != State;
      if (recalculate && changed)
        pipeGroupQueue.GroupRecalculateGates(pipeNode);
      return changed;
    }

    private void _CheckInput()
    {
      if (terrainService.Underground(coordinates))
      {
        Debug.Log($"[WATER.CheckInput] node={pipeNode?.id} gate={id} State=BLOCKED by underground");
        State = WaterGateState.BLOCKED;
        return;
      }
      var block = blockService.GetObjectsWithComponentAt<BlockObject>(coordinates).FirstOrDefault();
      if (block == null || block?.IsFinished == false)
      {
        Debug.Log($"[WATER.CheckInput] node={pipeNode?.id} gate={id} State=EMPTY by block=null IsFinished={block?.IsFinished}");
        State = WaterGateState.EMPTY;
        return;
      }
      var pipe = block.GetComponentFast<PipeNode>();
      var connected = pipeNode.TryConnect(this, pipe);
      if (connected)
      {
        Debug.Log($"[WATER.CheckInput] node={pipeNode?.id} gate={id} State=CONNECTED by connected=true");
        State = WaterGateState.CONNECTED;
        return;
      }
      var obstacle = block.GetComponentFast<WaterObstacle>(); 
      State = obstacle != null
        ? WaterGateState.BLOCKED
        : WaterGateState.EMPTY;
      Debug.Log($"[WATER.CheckInput] node={pipeNode?.id} gate={id} State={State} by WaterObstacle");
    }

    public bool FlowNotChanged(float water)
    {
      var newFlow = WaterGateFlow.STOP;
      if (water > 0f)
        newFlow = WaterGateFlow.OUT;
      if (water < 0f)
        newFlow = WaterGateFlow.IN;
      if (Flow == newFlow)
        return true;
      if (tick.Skip())
        return true;
      Flow = newFlow;
      return false;
    }

    public void MoveWater(float water)
    {
      if (!isEnabled || notHasEmptySpace())
        return;
      Debug.Log($"WATER.MoveWater pipe={pipeNode.id}");
      float waterAbs = Mathf.Abs(water);
      float contaminatedWater = waterAbs * ContaminationPercentage;
      float cleanWater = waterAbs - contaminatedWater;
      if (water > 0f)
        AddWater(cleanWater, contaminatedWater);
      if (water < 0f)
        RemoveWater(cleanWater, contaminatedWater);
    }

    private void AddWater(float cleanWater, float contaminatedWater)
    {
      try
      {
        if (cleanWater > 0f)
          waterService.AddCleanWater(coordinates, cleanWater);
        if (contaminatedWater > 0f)
          waterService.AddContaminatedWater(coordinates, contaminatedWater);
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR [WaterGate.AddWater] id={id} err={err}");
      }
    }

    private void RemoveWater(float cleanWater, float contaminatedWater)
    {
      try
      {
        if (!IsUnderwater())
          return;
        if (cleanWater > 0f)
          waterService.RemoveCleanWater(coordinates, cleanWater);
        if (contaminatedWater > 0f)
          waterService.RemoveContaminatedWater(coordinates, contaminatedWater);
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR [WaterGate.RemoveWater] id={id} err={err}");
      }
    }

    public string GetInfo()
    {
      string info = $"Gate[\n";
      info += $"  node={pipeNode?.id} gate={id} cord={coordinates.ToString()}\n ";
      info += $"  state={State} side={Side} flow={Flow}\n ";
      info += $"  gateConnected={gateConnected?.id} enabled={isEnabled}\n ";
      info += $"  Floor={Floor.ToString("0.00")} WaterLevel={WaterLevel.ToString("0.00")}\n ";
      info += $"  Water={Water.ToString("0.00")} DesiredWater={DesiredWater.ToString("0.00")}\n ";
      info += $"];\n";
      return info;
    }
  }
}
