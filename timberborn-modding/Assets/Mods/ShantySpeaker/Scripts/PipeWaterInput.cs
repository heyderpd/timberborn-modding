using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using Timberborn.WaterBuildings;
using UnityEngine;

namespace Mods.ShantySpeaker.Scripts {
  internal class PipeWaterInput : BaseComponent {

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
        float num = _threadSafeWaterMap.WaterHeightOrFloor(_waterCoordinatesTransformed);
        return num - (float)(_waterCoordinatesTransformed.z);
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
    
    public void RemoveCleanWater(float waterAmount)
    {
      _waterService.RemoveCleanWater(_waterCoordinatesTransformed, waterAmount);
    }
  }
}
