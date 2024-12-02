using System.Linq;
using System.Collections;
using System.Collections.Immutable;
using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BlockSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.Persistence;

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
    private int Size;

    [SerializeField]
    private int Height;

    [SerializeField]
    private float Speed;

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
      shieldField = waterShieldService.DiscoveryShieldField(Size, Height, blockObject);
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
    }

    public void Save(IEntitySaver entitySaver) { }

    [BackwardCompatible(2023, 9, 22)]
    public void Load(IEntityLoader entityLoader) { }

    public IEnumerator _ActivateShield()
    {
      Debug.Log($"ActivateShield start shieldField={shieldField.Count()}");
      var origin = blockObject.Coordinates;
      foreach (var coordinate in shieldField)
      {
        Debug.Log($"ActivateShield.yield coordinate={coordinate}");
        waterRadar.AddFullObstacle(coordinate);
        //waterRadar.AddDirectionLimiter(origin, coordinate);
        yield return new WaitForSeconds(0.5f);
      }
      Debug.Log($"ActivateShield end");
    }

    public void ActivateShield()
    {
      StartCoroutine(_ActivateShield());
    }
  }
}
