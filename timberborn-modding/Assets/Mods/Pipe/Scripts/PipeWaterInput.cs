using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using UnityEngine;

namespace Mods.Pipe.Scripts
{
  internal class PipeWaterInput : BaseComponent
  {

    [SerializeField]
    private string _inputCoordinates; // Front Back , Left Right , Top Botton

    [SerializeField]
    private Vector3Int _waterCoordinates;

    private BlockObject _blockObject;

    private Vector3Int _waterCoordinatesTransformed;

    private IWaterService _waterService;

    private IThreadSafeWaterMap _threadSafeWaterMap;

    public bool IsUnderwater
    {
      get
      {
        return _threadSafeWaterMap.CellIsUnderwater(_waterCoordinatesTransformed);
      }
    }

    public float AvailableWater
    {
      get
      {
        float totalWaterAmount = _threadSafeWaterMap.WaterHeightOrFloor(_waterCoordinatesTransformed);
        return totalWaterAmount - (float)(_waterCoordinatesTransformed.z);
      }
    }

    public float AvailableContaminatedWater
    {
      get
      {
        float availableWater = this.AvailableWater;
        float contaminationPercentage = _threadSafeWaterMap.ColumnContamination(_waterCoordinatesTransformed);
        return availableWater * contaminationPercentage;
      }
    }

    [Inject]
    public void InjectDependencies(IWaterService waterService, IThreadSafeWaterMap threadSafeWaterMap)
    {
      _waterService = waterService;
      _threadSafeWaterMap = threadSafeWaterMap;
    }

    public void Awake()
    {
      _blockObject = GetComponentFast<BlockObject>();
    }

    public void Start()
    {
      _waterCoordinatesTransformed = _blockObject.Transform(_waterCoordinates);
    }

    public void RemoveWater(float cleanWater, float contaminatedWater)
    {
      if (cleanWater > 0f)
      {
        _waterService.RemoveCleanWater(_waterCoordinatesTransformed, cleanWater);
      }
      if (contaminatedWater > 0f)
      {
        _waterService.RemoveContaminatedWater(_waterCoordinatesTransformed, contaminatedWater);
      }
    }

    public void AddWater(float cleanWater, float contaminatedWater)
    {
      if (cleanWater > 0f)
      {
        _waterService.AddCleanWater(_waterCoordinatesTransformed, cleanWater);
      }
      if (contaminatedWater > 0f)
      {
        _waterService.AddContaminatedWater(_waterCoordinatesTransformed, contaminatedWater);
      }
    }
  }
}
