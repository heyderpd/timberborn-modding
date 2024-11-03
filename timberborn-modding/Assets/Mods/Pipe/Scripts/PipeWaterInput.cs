using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using UnityEngine;
using Timberborn.CoreUI;
using Timberborn.MechanicalSystem;
using System.Linq;
using Timberborn.SingletonSystem;

namespace Mods.Pipe.Scripts
{
  internal class PipeWaterInput : BaseComponent
  {

    [SerializeField]
    private string _positionLabel; // Front Back , Left Right , Top Botton

    [SerializeField]
    private Vector3Int _waterCoordinates;

    public int direction { get; private set; } = 0; // 0 = stop , 1 = enter , 2 = exit

    private PipeWaterLink _pipeWaterLink;

    private BlockObject _blockObject;

    private BlockService _blockService;

    private Vector3Int _waterCoordinatesTransformed;

    private IWaterService _waterService;

    private IThreadSafeWaterMap _threadSafeWaterMap;

    public float Water { get; private set; }

    public float CleanWater { get; private set; }

    public float ContaminatedWater { get; private set; }

    public bool inputEnabled { get; private set; } = false;

    public bool IsUnderwater
    {
      get
      {
        if (_threadSafeWaterMap == null)
          return false;
        return _threadSafeWaterMap.CellIsUnderwater(_waterCoordinatesTransformed);
      }
    }

    private float _AvailableWater
    {
      get
      {
        if (_threadSafeWaterMap == null)
          return 0f;
        float totalWaterAmount = _threadSafeWaterMap.WaterHeightOrFloor(_waterCoordinatesTransformed);
        return totalWaterAmount - (float)(_waterCoordinatesTransformed.z);
      }
    }

    private float _AvailableContaminatedWater
    {
      get
      {
        if (_threadSafeWaterMap == null)
          return 0f;
        float availableWater = this.Water;
        float contaminationPercentage = _threadSafeWaterMap.ColumnContamination(_waterCoordinatesTransformed);
        return availableWater * contaminationPercentage;
      }
    }

    public void UpdateAvailableWaters()
    {
      Water = _AvailableWater;
      ContaminatedWater = _AvailableContaminatedWater;
      CleanWater = Water - ContaminatedWater;
    }

    [Inject]
    public void InjectDependencies(
      IWaterService waterService,
      IThreadSafeWaterMap threadSafeWaterMap,
      BlockService blockService
    )
    {
      _waterService = waterService;
      _threadSafeWaterMap = threadSafeWaterMap;
      _blockService = blockService;
    }

    public void Awake()
    {
      _blockObject = GetComponentFast<BlockObject>();
      _pipeWaterLink = GetComponentFast<PipeWaterLink>();
    }

    public void Start()
    {
      _waterCoordinatesTransformed = _blockObject.Transform(_waterCoordinates);
    }

    public void CheckClearInput()
    {
      var Pipelink = _blockService.GetObjectsWithComponentAt<PipeWaterLink>(Vector3Int.FloorToInt(_waterCoordinatesTransformed)).ToList().First();
      if (Pipelink)
      {
        inputEnabled = false;
        Pipelink.Connect(_pipeWaterLink);
        inputEnabled = true;
        return;
      }
      var block = _blockService.GetObjectsWithComponentAt<BlockObject>(Vector3Int.FloorToInt(_waterCoordinatesTransformed)).ToList().First();
      inputEnabled = !block.Solid;
      inputEnabled = true;
    }

    public void StopWater()
    {
      direction = 0;
    }

    public void InWater()
    {
      direction = 1;
    }

    public void OutWater()
    {
      direction = 2;
    }

    public void AddWater(float cleanWater, float contaminatedWater)
    {
      if (!inputEnabled)
      {
        return;
      }
      if (cleanWater > 0f)
      {
        _waterService.AddCleanWater(_waterCoordinatesTransformed, cleanWater);
      }
      if (contaminatedWater > 0f)
      {
        _waterService.AddContaminatedWater(_waterCoordinatesTransformed, contaminatedWater);
      }
    }

    public void RemoveWater(float cleanWater, float contaminatedWater)
    {
      if (!inputEnabled)
      {
        return;
      }
      if (cleanWater > 0f)
      {
        _waterService.RemoveCleanWater(_waterCoordinatesTransformed, cleanWater);
      }
      if (contaminatedWater > 0f)
      {
        _waterService.RemoveContaminatedWater(_waterCoordinatesTransformed, contaminatedWater);
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
      }
      else
      {
        RemoveWater(cleanWater, contaminatedWater);
      }
    }

    public string GetInfo()
    {
      string info = "{ ";
      info += "label = " + _positionLabel;
      info += "enabled = " + inputEnabled;
      info += "water = " + Water.ToString("0.00");
      info += " }";
      return info;
    }
  }
}
