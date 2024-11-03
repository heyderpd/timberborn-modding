using System;
using Bindito.Core;
//using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Coordinates;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using Timberborn.WaterBuildings;
using UnityEngine;

namespace Mods.ShantySpeaker.Scripts
{

  internal class PipeBuilding : TickableComponent,
                                IInitializableEntity,
                                IFinishedStateListener,
                                IPersistentEntity
  {

    private bool ready = false;

    private PipeWaterInput _waterInput;

    private WaterOutput _waterOutput;

    public void Awake()
    {
      _waterInput = GetComponentFast<PipeWaterInput>();
      _waterOutput = GetComponentFast<WaterOutput>();
      ((Behaviour)this).enabled = false;
    }

    public void InitializeEntity()
    {
      ready = true;
    }

    public void DeleteEntity()
    {
      ready = false;
    }

    public override void Tick()
    {
      if (!ready)
      {
        return;
      }
      float water = _waterInput.AvailableWater;
      if (water <= 0)
      {
        return;
      }
      _waterInput.RemoveCleanWater(water);
      _waterOutput.AddCleanWater(water);
    }

    public void Save(IEntitySaver entitySaver) { }

    public void Load(IEntityLoader entityLoader) { }

    public void OnEnterFinishedState()
    {
      ((Behaviour)this).enabled = true;
    }

    public void OnExitFinishedState()
    {
      ((Behaviour)this).enabled = false;
    }
  }
}
