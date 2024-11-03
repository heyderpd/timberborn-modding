using System;
using System.Linq;
using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;

namespace Mods.Pipe.Scripts
{
  internal class WaterGate : BaseComponent,
                             IInitializableEntity,
                             IDeletableEntity
  {
    public bool internalGateEnabled { get; private set; } = true;

    private static int lastId = 0;

    public readonly int id = lastId++;

    [SerializeField]
    private Vector3Int waterCoordinates;

    [SerializeField]
    public WaterGateSide waterGateSide;

    public WaterGateConfig config = new WaterGateConfig();

    private PipeNode pipeNode;

    public WaterGate gateConnected;

    private BlockObject blockObject;

    private BlockService blockService;

    public Vector3Int coordinates { get; private set; }

    private IWaterService waterService;

    private IThreadSafeWaterMap threadSafeWaterMap;

    public float Water { get; private set; }

    public float CleanWater { get; private set; }

    public float ContaminatedWater { get; private set; }

    [Inject]
    public void InjectDependencies(
      IWaterService _waterService,
      IThreadSafeWaterMap _threadSafeWaterMap,
      BlockService _blockService
    )
    {
      waterService = _waterService;
      threadSafeWaterMap = _threadSafeWaterMap;
      blockService = _blockService;
    }

    public void Awake()
    {
      blockObject = GetComponentFast<BlockObject>();
      pipeNode = GetComponentFast<PipeNode>();
    }

    public void InitializeEntity()
    {
      coordinates = blockObject.Transform(waterCoordinates);
    }

    public void DeleteEntity() { }

    public bool isEnabled
    {
      get
      {
        if (pipeNode == null)
          return false;
        return internalGateEnabled && pipeNode.isEnabled;
      }
    }

    private bool IsUnderwater()
    {
      try
      {
        if (threadSafeWaterMap == null || !isEnabled || notHasEmptySpace())
          return false;
        var underwater = threadSafeWaterMap.CellIsUnderwater(coordinates);
        return underwater;
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR GATE.IsUnderwater id={id} err={err}");
        return false;
      }
    }

    private float GetWater()
    {
      try
      {
        if (!IsUnderwater())
          return 0f;
        float totalWaterAmount = threadSafeWaterMap.WaterHeightOrFloor(coordinates);
        var water = totalWaterAmount - (float)(coordinates.z);
        return Mathf.Max(water, 0f);
      } catch (Exception err)
      {
        Debug.Log($"#ERROR GATE.GetWater id={id} err={err}");
        return 0f;
      }
    }

    private float GetContaminatedWater()
    {
      try
      {
        if (!IsUnderwater() || Water == 0f)
          return 0f;
        float availableWater = Water;
        float contaminationPercentage = threadSafeWaterMap.ColumnContamination(coordinates);
        return availableWater * contaminationPercentage;
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR GATE.GetContaminatedWater id={id} err={err}");
        return 0f;
      }
    }

    public void UpdateAvailableWaters()
    {
      Debug.Log($"GATE.UpdateAvailableWaters id={id}");
      Water = GetWater();
      ContaminatedWater = GetContaminatedWater();
      CleanWater = Water - ContaminatedWater;
    }

    public void SetDisabled()
    {
      internalGateEnabled = false;
    }

    public void ReleaseConnection()
    {
      gateConnected = null;
      pipeNode.groupCheckGate(this);
    }

    private bool notHasEmptySpace()
    {
      var obstacle = blockService.GetObjectsWithComponentAt<WaterObstacle>(coordinates).FirstOrDefault();
      Debug.Log($"GATE.notHasEmptySpace pipeNode={pipeNode.id} gate={id} isObstacle={obstacle != null}");
      return obstacle != null;
    }

    public bool CheckClearInput()
    {
      var block = blockService.GetObjectsWithComponentAt<BlockObject>(coordinates).FirstOrDefault();
      if (block == null)
      {
        Debug.Log($"GATE.CheckClearInput pipeNode={pipeNode.id} gate={id} internalGateEnabled={true} by empty");
        return internalGateEnabled = true;
      }
      var pipe = block.GetComponentFast<PipeNode>();
      var connected = pipe != null ? pipeNode.TryConnect(this, pipe) : false;
      if (connected)
      {
        Debug.Log($"GATE.CheckClearInput pipeNode={pipeNode.id} gate={id} internalGateEnabled={false} by connected");
        return internalGateEnabled = false;
      }
      var obstacle = block.GetComponentFast<WaterObstacle>();
      Debug.Log($"GATE.CheckClearInput pipeNode={pipeNode.id} gate={id} internalGateEnabled={obstacle == null} by obstacle");
      return internalGateEnabled = obstacle == null;
    }

    public void AddWater(float cleanWater, float contaminatedWater)
    {
      try
      {
        //Debug.Log($"GATE.AddWater id={id} start");
        if (!isEnabled || notHasEmptySpace())
        {
          //Debug.Log($"GATE.AddWater id={id} isEnabled={isEnabled} nospace={notHasEmptySpace()} abort");
          return;
        }
        //Debug.Log($"GATE.AddWater water={cleanWater} conta={contaminatedWater} work");
        if (cleanWater > 0f)
        {
          waterService.AddCleanWater(coordinates, cleanWater);
        }
        if (contaminatedWater > 0f)
        {
          waterService.AddContaminatedWater(coordinates, contaminatedWater);
        }
        //Debug.Log($"GATE.AddWater id={id} end");
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR GATE.AddWater id={id} err={err}");
      }
    }

    public void RemoveWater(float cleanWater, float contaminatedWater)
    {
      try
      {
        //Debug.Log($"GATE.RemoveWater id={id} start");
        if (!isEnabled || !IsUnderwater())
        {
          //Debug.Log($"GATE.RemoveWater id={id} isEnabled={isEnabled} nospace={notHasEmptySpace()} abort");
          return;
        }
        //Debug.Log($"GATE.RemoveWater water={cleanWater} conta={contaminatedWater} work");
        if (cleanWater > 0f)
        {
          waterService.RemoveCleanWater(coordinates, cleanWater);
        }
        if (contaminatedWater > 0f)
        {
          waterService.RemoveContaminatedWater(coordinates, contaminatedWater);
        }
        //Debug.Log($"GATE.RemoveWater id={id} end");
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR GATE.RemoveWater id={id} err={err}");
      }
    }

    public void MoveWater(float cleanWater, float contaminatedWater)
    {
      bool isAddWater = cleanWater >= 0f && contaminatedWater >= 0f;
      cleanWater = Mathf.Abs(cleanWater);
      contaminatedWater = Mathf.Abs(contaminatedWater);
      if (isAddWater)
      {
        AddWater(cleanWater, contaminatedWater);
      } else
      {
        RemoveWater(cleanWater, contaminatedWater);
      }
    }

    public string GetInfo()
    {
      string info = $"  Gate[nodeId={pipeNode.id}, nodeId={id}, cord={coordinates.ToString()}, pos={waterCoordinates.ToString()}, flow={config.flow}, side={waterGateSide}, enabled={isEnabled}, water={Water.ToString("0.00")}];\n";
      return info;
    }
  }
}
