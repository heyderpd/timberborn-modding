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

    private BlockObject blockObject;

    private Vector3Int coordinate;

    [Inject]
    public void InjectDependencies(
      WaterRadar _waterRadar
    )
    {
      waterRadar = _waterRadar;
    }

    public void Awake()
    {
      ((Behaviour)this).enabled = false;
      blockObject = GetComponentFast<BlockObject>();
    }

    public void InitializeEntity() { }

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

    private void Shield(bool activate)
    {
      var center = new Vector3Int(-4, -4, 0);
      var size = 2;//12
      for (var z = 0; z < size; z++)
      {
        for (var y = 0; y < size; y++)
        {
          for (var x = 0; x < size; x++)
          {
            var reference = center + new Vector3Int(x, y, z);
            coordinate = blockObject.Transform(reference);
            if (activate)
              waterRadar.AddFullObstacle(coordinate);
            else
              waterRadar.RemoveFullObstacle(coordinate);
          }
        }
      }
    }

    public void ActivateShield() {
      Shield(true);
    }

    public void DeactivateShield()
    {
      Shield(false);
    }
  }
}
