﻿using System;
using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using Timberborn.Persistence;
using Timberborn.CoreUI;

namespace Mods.OldGopher.Pipe
{
  internal class WaterGate : BaseComponent,
                             IInitializableEntity,
                             IDeletableEntity,
                             IPersistentEntity
  {
    private static int lastId = 0;

    public readonly int id = lastId++;

    [SerializeField]
    public WaterGateType Type;

    [SerializeField]
    public WaterGateMode Mode = WaterGateMode.BOTH;

    private static readonly ComponentKey WaterGateKey = new ComponentKey("WaterGate");

    private static readonly PropertyKey<bool> ModeConfigKey = new PropertyKey<bool>("WaterGateMode");

    private static readonly PropertyKey<bool> WaterPumpPowerConfigKey = new PropertyKey<bool>("WaterPumpPower");

    public WaterGateState State { get; private set; }

    private WaterGateFlow? Flow = null;

    public bool IsOnlyDelivery => Mode == WaterGateMode.ONLY_IN;
    
    public bool IsOnlyRequester => Mode == WaterGateMode.ONLY_OUT;

    public bool IsValve => Type == WaterGateType.VALVE;

    public bool IsWaterPump => Type == WaterGateType.WATERPUMP;

    public PipeNode pipeNode { get; private set; }

    public PipeStatus pipeStatus { get; private set; }
    
    public WaterGate gateConnected { get; private set; }

    public TickCount flowTick = new TickCount(20);

    public TickCount retryDetectionTick = new TickCount(90);

    private BlockObject blockObject;

    public Vector3Int coordinate { get; private set; }

    private IWaterService waterService;

    private IThreadSafeWaterMap threadSafeWaterMap;

    private BlockService blockService;

    private Colors colors;

    private WaterRadar waterRadar;

    private PipeGroupQueue pipeGroupQueue;

    public float LowerLimit { get; private set; }

    public float HigthLimit { get; private set; }

    public bool SuccessWhenCheckWater { get; private set; }

    public float WaterLevel { get; private set; }

    public float WaterDetected { get; private set; }

    public float WaterAvailable { get; private set; }

    public float WaterPressure { get; private set; }

    public float ContaminationPercentage { get; private set; }

    public PipeNodePowered powered { get; private set; } = null;

    private WaterParticle waterParticle;

    private event EventHandler<WaterAdditionEvent> WaterAdded;

    private PipeWaterPumpGear pumpGear;

    private event EventHandler<WaterAdditionEvent> pumpGearAnimation;

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
      pipeStatus = GetComponentFast<PipeStatus>();
      waterParticle = GameObjectFast.AddComponent<WaterParticle>();
      if (IsWaterPump)
      {
        powered = GetComponentFast<PipeNodePowered>();
        pumpGear = GetComponentFast<PipeWaterPumpGear>();
      }
    }

    public void InitializeEntity()
    {
      coordinate = blockObject.Transform(WaterGateConfig.getCoordinates(Type));
      LowerLimit = (float)(coordinate.z) + WaterGateConfig.getLowerLimitShift(Type);
      HigthLimit = (float)(coordinate.z) + WaterGateConfig.getHigthLimitShift(Type);
      waterParticle.Initialize(colors, this);
      WaterAdded += waterParticle.OnWaterAdded;
      if (IsWaterPump)
        pumpGearAnimation += pumpGear.OnAnimationEvent;
    }

    public void DeleteEntity() { }

    public void Save(IEntitySaver entitySaver)
    {
      if (!IsWaterPump)
        return;
      bool waterPumpMode = Mode == WaterGateMode.ONLY_IN;
      IObjectSaver component = entitySaver.GetComponent(WaterGateKey);
      component.Set(ModeConfigKey, waterPumpMode);
      component.Set(WaterPumpPowerConfigKey, powered?.Active ?? false);
    }

    [BackwardCompatible(2023, 9, 22)]
    public void Load(IEntityLoader entityLoader)
    {
      if (!IsWaterPump || !entityLoader.HasComponent(WaterGateKey))
        return;
      IObjectLoader component = entityLoader.GetComponent(WaterGateKey);
      bool waterPumpMode = component.Get(ModeConfigKey);
      Mode = waterPumpMode
        ? WaterGateMode.ONLY_IN
        : WaterGateMode.ONLY_OUT;
      if (powered && component.Has(WaterPumpPowerConfigKey))
      {
        bool waterPumpPower = component.Get(WaterPumpPowerConfigKey);
        powered.Active = waterPumpPower;
      }
    }

    public bool isEnabled
    {
      get
      {
        if (pipeNode == null)
          return false;
        return pipeNode.isEnabled && gateConnected == null && State == WaterGateState.EMPTY;
      }
    }

    public float PowerEfficiency
    {
      get
      {
        if (powered == null)
          return 1f;
        return powered.PowerEfficiency;
      }
    }

    private bool IsUnderwater()
    {
      try
      {
        if (threadSafeWaterMap == null || !isEnabled)
          return false;
        var underwater = threadSafeWaterMap.CellIsUnderwater(coordinate);
        return underwater;
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [WaterGate.IsUnderwater] id={id} err={err}");
        return false;
      }
    }

    public void UpdateWaters()
    {
      try
      {
        if (!isEnabled)
        {
          SuccessWhenCheckWater = false;
          return;
        }
        WaterLevel = waterRadar.IsOutOfMap(coordinate)
          ? 0f
          : threadSafeWaterMap.WaterHeightOrFloor(coordinate);
        WaterDetected = Mathf.Max(WaterLevel - LowerLimit, 0f);
        WaterAvailable = PipeWaterService.LimitWater(WaterDetected);
        WaterPressure = PipeWaterService.CalcPressure(LowerLimit, WaterLevel);
        ContaminationPercentage = WaterAvailable > 0f
          ? threadSafeWaterMap.ColumnContamination(coordinate)
          : 0f;
        SuccessWhenCheckWater = true;
      } catch (Exception err)
      {
        ModUtils.Log($"#ERROR [WaterGate.UpdateWaters] id={id} err={err}");
        SuccessWhenCheckWater = false;
      }
    }

    public void ToggleWaterPumpMode()
    {
      if (!IsWaterPump)
        return;
      Mode = Mode == WaterGateMode.ONLY_OUT
        ? WaterGateMode.ONLY_IN
        : WaterGateMode.ONLY_OUT;
    }

    public void ToggleWaterPumpPower()
    {
      if (!IsWaterPump || !powered)
        return;
      powered.Active = !powered.Active;
    }

    public void TestParticle()
    {
      SetWaterParticles(
        UnityEngine.Random.Range(0f, PipeWaterService.waterFactor),
        UnityEngine.Random.Range(0f, PipeWaterService.waterFactor)
      );
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
      return _CheckInput(tryConnect: false) == WaterGateState.BLOCKED;
    }

    public void retryDetection()
    {
      if (!isEnabled || retryDetectionTick.Skip())
        return;
      pipeGroupQueue.Gate_Check(this);
    }

    public bool CheckInput()
    {
      var oldState = State;
      State = _CheckInput();
      if (State != WaterGateState.CONNECTED)
        Disconnection();
      pipeStatus.SetBlocked(this, State == WaterGateState.BLOCKED);
      return oldState != State;
    }

    private WaterGateState _CheckInput(bool tryConnect = true)
    {
      var (pipe, obstacle) = waterRadar.FindWaterObstacle(coordinate);
      if (pipe == pipeNode)
        return WaterGateState.EMPTY;
      if (tryConnect && pipeNode.TryConnect(this, pipe))
        return WaterGateState.CONNECTED;
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
      if (Flow != null && flowTick.Skip())
        return true;
      ModUtils.Log($"[WaterGate.FlowNotChanged] water={water} newFlow={newFlow} Flow={Flow}");
      Flow = newFlow;
      return false;
    }

    public void RemoveWaterParticles()
    {
      SetWaterParticles(0f, 0f);
    }

    private void SetWaterParticles(float Water, float ContaminatedPercentage)
    {
      this.pumpGearAnimation?.Invoke(this, new WaterAdditionEvent(Water, 0f));
      if (WaterLevel < HigthLimit)
        this.WaterAdded?.Invoke(this, new WaterAdditionEvent(Water, ContaminatedPercentage));
      else
        this.WaterAdded?.Invoke(this, new WaterAdditionEvent(0f, 0f));
    }

    public void MoveWater(float water, float contamination)
    {
      retryDetection();
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
          waterService.AddCleanWater(coordinate, cleanWater);
        if (contaminatedWater > 0f)
          waterService.AddContaminatedWater(coordinate, contaminatedWater);
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
          waterService.RemoveCleanWater(coordinate, cleanWater);
        if (contaminatedWater > 0f)
          waterService.RemoveContaminatedWater(coordinate, contaminatedWater);
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [WaterGate.RemoveWater] id={id} err={err}");
      }
    }

    public string GetInfo(bool close = true)
    {
      string info = $"  Gate[\n";
      info += $"    id={id} pipe={pipeNode?.id} enabled={isEnabled}\n";
      info += $"    connected={gateConnected?.id} success={SuccessWhenCheckWater}\n";
      info += $"    type={Type} mode={Mode} state={State} flow={Flow}\n";
      info += $"    IsOnlyRequester={IsOnlyRequester} IsOnlyDelivery={IsOnlyDelivery}\n";
      info += $"    lower={LowerLimit.ToString("0.00")} higth={HigthLimit.ToString("0.00")} level={WaterLevel.ToString("0.00")}\n";
      info += $"    detected={WaterDetected.ToString("0.00")} available={WaterAvailable.ToString("0.00")} conta={ContaminationPercentage.ToString("0.00")} pressure={WaterPressure.ToString("0.00")}\n";
      if (close)
        info += $"  ];\n";
      return info;
    }
  }
}
