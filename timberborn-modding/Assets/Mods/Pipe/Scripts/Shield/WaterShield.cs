using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BlockSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.Persistence;
using UnityEngine.UIElements;
using System.Collections.Immutable;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShield : BaseComponent,
                               IInitializableEntity,
                               IDeletableEntity,
                               IFinishedStateListener
  {
    private WaterRadar waterRadar;

    private WaterShieldService waterShieldService;

    private BlockObject blockObject;

    private Vector3Int coordinate;

    private ImmutableArray<Vector3Int> shieldField;

    [SerializeField]
    private int BuildLength;

    [SerializeField]
    private int Size;

    [SerializeField]
    private int Height;

    [Inject]
    public void InjectDependencies(
      WaterRadar _waterRadar,
      WaterShieldService _waterShieldService
    )
    {
      waterRadar = _waterRadar;
      waterShieldService = _waterShieldService;
    }

    public void Awake()
    {
      ((Behaviour)this).enabled = false;
      blockObject = GetComponentFast<BlockObject>();
    }

    public void InitializeEntity() {
      shieldField = waterShieldService.DiscoveryShieldField(BuildLength, Size, Height, blockObject);
    }

    public void DeleteEntity() { }

    public void OnEnterFinishedState()
    {
      ((Behaviour)this).enabled = true;
      ActivateShield();
    }

    public void OnExitFinishedState()
    {
      ((Behaviour)this).enabled = false;
      DeactivateShield();
    }

    public void Save(IEntitySaver entitySaver) { }

    [BackwardCompatible(2023, 9, 22)]
    public void Load(IEntityLoader entityLoader) { }

    public void ActivateShield()
    {
      foreach (var coordinate in shieldField)
      {
        waterRadar.AddFullObstacle(coordinate);
      }
    }

    public void DeactivateShield()
    {
      foreach (var coordinate in shieldField)
      {
        waterRadar.RemoveFullObstacle(coordinate);
      }
    }
  }
}
