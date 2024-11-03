using System;
using System.Linq;
using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using Timberborn.CoreUI;
using UnityEngine.TestTools;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class WaterGate : BaseComponent,
                             IInitializableEntity,
                             IDeletableEntity
  {
    private static int lastId = 0;

    public readonly int id = lastId++;

    [SerializeField]
    public WaterGateSide Side;

    [SerializeField]
    public WaterGateType Type = WaterGateType.BOTH;

    [SerializeField]
    public bool StopWhenSubmerged = false;

    public WaterGateState State { get; private set; }

    private WaterGateFlow? Flow = null;
    
    public PipeNode pipeNode { get; private set; }

    public WaterGate gateConnected { get; private set; }

    public TickCount tick = new TickCount(5);

    private BlockObject blockObject;

    public Vector3Int coordinates { get; private set; }

    private IWaterService waterService;

    private IThreadSafeWaterMap threadSafeWaterMap;

    private BlockService blockService;

    private Colors colors;

    private WaterRadar waterRadar;

    private PipeGroupQueue pipeGroupQueue;

    public float LowerLimit { get; private set; }

    public float HigthLimit;

    public float WaterLevel { get; private set; }

    public float Water { get; private set; }

    public float DesiredWater;

    public float ContaminationPercentage { get; private set; }

    private WaterParticle waterParticle;

    private event EventHandler<WaterAddition> WaterAdded;

    [Inject]
    public void InjectDependencies(
      IWaterService _waterService,
      IThreadSafeWaterMap _threadSafeWaterMap,
      BlockService _blockService,
      Colors _colors,
      WaterRadar _waterRadar,
      PipeGroupQueue _pipeGroupQueue
    )
    {
      waterService = _waterService;
      threadSafeWaterMap = _threadSafeWaterMap;
      blockService = _blockService;
      colors = _colors;
      waterRadar = _waterRadar;
      pipeGroupQueue = _pipeGroupQueue;
    }

    public void Awake()
    {
      blockObject = GetComponentFast<BlockObject>();
      pipeNode = GetComponentFast<PipeNode>();
      waterParticle = GameObjectFast.AddComponent<WaterParticle>();
    }

    public void InitializeEntity()
    {
      coordinates = blockObject.Transform(WaterGateConfig.getCoordinates(Side));
      LowerLimit = (float)(coordinates.z) + WaterGateConfig.getLowerLimitShift(Side);
      HigthLimit = (float)(coordinates.z) + WaterGateConfig.getHigthLimitShift(Side);
      waterParticle.Initialize(colors, this);
      WaterAdded += waterParticle.OnWaterAdded;
      //SetWaterParticles(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
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
        ModUtils.Log($"#ERROR [WaterGate.IsUnderwater] id={id} err={err}");
        return false;
      }
    }

    public bool UpdateWaters()
    {
      try
      {
        if (!isEnabled)
        {
          WaterLevel = 0f;
          Water = 0f;
          ContaminationPercentage = 0f;
          return true;
        }
        WaterLevel = threadSafeWaterMap.WaterHeightOrFloor(coordinates);
        Water = Mathf.Max(WaterLevel - LowerLimit, 0f);
        if (Water > 0f)
          ContaminationPercentage = threadSafeWaterMap.ColumnContamination(coordinates);
        else
          ContaminationPercentage = 0f;
        DesiredWater = Water;
        return true;
      } catch (Exception err)
      {
        ModUtils.Log($"#ERROR [WaterGate.UpdateWaters] id={id} err={err}");
        WaterLevel = 0f;
        Water = 0f;
        ContaminationPercentage = 0f;
        return false;
      }
    }

    public bool CanDelivereryWater(float average)
    {
      if (Type == WaterGateType.ONLY_OUT)
        return false;
      return WaterLevel > average;
    }

    public bool CanRequestWater(float average)
    {
      if (Type == WaterGateType.ONLY_IN)
        return false;
      return WaterLevel < average;
    }

    private float LimitWater(float expectedWater)
    {
      float water = Mathf.Abs(expectedWater);
      water = Mathf.Min(water, WaterService.maximumFlow);
      water = water >= WaterService.minimumFlow ? water : 0f;
      return water;
    }

    public float GetDeliveryWater(float average)
    {
      var limited = LimitWater(Water);
      return limited;
    }

    public float GetRequesterWater(float average)
    {
      if (StopWhenSubmerged)
      {
        if (WaterLevel > HigthLimit && HigthLimit > average && LowerLimit > average)
          return 0f;
        var maximumWater = WaterLevel > LowerLimit
          ? HigthLimit - WaterLevel
          : HigthLimit - LowerLimit;
        maximumWater = Math.Max(maximumWater, 0f);
        return maximumWater;
      }
      var water = average - WaterLevel;
      var limited = LimitWater(water);
      return limited;
    }

    public bool CheckConnection(WaterGate gate)
    {
      return gateConnected == gate && gate.gateConnected == this;
    }

    public void Disconnection()
    {
      gateConnected = null;
    }

    public void ConnectionOneSide(WaterGate gate)
    {
      RemoveWaterParticles();
      gateConnected = gate;
      State = WaterGateState.CONNECTED;
    }

    public void ConnectionBoth(WaterGate gate)
    {
      ConnectionOneSide(gate);
      gate.ConnectionOneSide(this);
    }
    
    private bool notHasEmptySpace()
    {
      var obstacle = waterRadar.FindWaterObstacle(coordinates, Side);
      return obstacle == WaterObstacleType.BLOCK;
    }

    public bool CheckInput()
    {
      var oldState = State;
      State = _CheckInput();
      if (State != WaterGateState.CONNECTED)
        Disconnection();
      return oldState != State;
    }

    private WaterGateState _CheckInput()
    {
      var pipe = blockService.GetObjectsWithComponentAt<PipeNode>(coordinates).FirstOrDefault();
      ModUtils.Log($"[WATER.CheckInput] 01 node={pipe?.id} gate={id} finding_pipe");
      var connected = pipeNode.TryConnect(this, pipe);
      if (connected)
      {
        ModUtils.Log($"[WATER.CheckInput] 02 node={pipeNode?.id} gate={id} State=CONNECTED by connected=true");
        return WaterGateState.CONNECTED;
      }
      var obstacle = waterRadar.FindWaterObstacle(coordinates, Side);
      ModUtils.Log($"[WATER.CheckInput] 03 node={pipe?.id} gate={id} obstacle={obstacle}");
      return obstacle == WaterObstacleType.BLOCK
        ? WaterGateState.BLOCKED
        : WaterGateState.EMPTY;
    }

    public void ResetFlow()
    {
      Flow = null;
    }

    public bool FlowNotChanged(float water)
    {
      var newFlow = WaterGateFlow.STOP;
      if (water > 0f)
        newFlow = WaterGateFlow.OUT;
      if (water < 0f)
        newFlow = WaterGateFlow.IN;
      if (Flow != null && Flow == newFlow)
        return true;
      if (Flow != null && tick.Skip())
        return true;
      Flow = newFlow;
      return false;
    }

    public void RemoveWaterParticles()
    {
      SetWaterParticles(0f, 0f);
    }

    private void SetWaterParticles(float Water, float ContaminatedPercentage)
    {
      if (WaterLevel < HigthLimit)
        this.WaterAdded?.Invoke(this, new WaterAddition(Water, ContaminatedPercentage));
      else
        this.WaterAdded?.Invoke(this, new WaterAddition(0f, 0f));
    }

    public void MoveWater(float water, float contamination)
    {
      SetWaterParticles(water, contamination);
      if (!isEnabled || notHasEmptySpace())
        return;
      float waterAbs = Mathf.Abs(water);
      float contaminatedWater = waterAbs * contamination;
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
        ModUtils.Log($"#ERROR [WaterGate.AddWater] id={id} err={err}");
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
        ModUtils.Log($"#ERROR [WaterGate.RemoveWater] id={id} err={err}");
      }
    }

    public string GetInfo()
    {
      string info = $"Gate[node={pipeNode?.id}_gate={id} enabled={isEnabled} state={State}\n";
      info += $"\tgateConnected={gateConnected?.id} WaterLevel={WaterLevel.ToString("0.00")}\n";
      info += $"\twater={Water.ToString("0.00")} conta={ContaminationPercentage.ToString("0.00")} desire={DesiredWater.ToString("0.00")}];\n";
      return info;
    }
  }
}
