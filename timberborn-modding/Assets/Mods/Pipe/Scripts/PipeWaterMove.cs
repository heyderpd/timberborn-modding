using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Mods.Pipe.Scripts
{

  internal class PipeBuilding : TickableComponent,
                                IInitializableEntity,
                                IFinishedStateListener,
                                IPersistentEntity {

    private bool ready = false;

    private float _waterDiffLimit = 0.02f;

    private float _waterCapacity = 0.25f;

    private List<PipeWaterInput> _waterInputs;

    private PipeWaterInput _waterInputA;

    private PipeWaterInput _waterInputB;

    public void Awake()
    {
      List<PipeWaterInput> _waterInputs = new List<PipeWaterInput>();
      GetComponentsFast<PipeWaterInput>(_waterInputs);
      _waterInputA = _waterInputs.First();
      _waterInputB = _waterInputs.Last();
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
      float waterA = _waterInputA.AvailableWater;
      float waterB = _waterInputB.AvailableWater;
      if (waterA <= 0 && waterB <= 0)
      {
        return;
      }
      float waterDiff = Mathf.Abs(waterA - waterB) / 2;
      if (waterDiff < _waterDiffLimit)
      {
        return;
      }
      waterDiff = Mathf.Min(waterDiff, _waterCapacity);
      if (waterA > waterB)
      {
        MoveWater(_waterInputA, _waterInputB, waterDiff);
      } else
      {
        MoveWater(_waterInputB, _waterInputA, waterDiff);
      }
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

    public void MoveWater(PipeWaterInput waterHigh, PipeWaterInput waterLower, float availableWater)
    {
      float contaminatedWater = waterHigh.AvailableContaminatedWater;
      float cleanWater = availableWater - contaminatedWater;
      waterHigh.RemoveWater(cleanWater, contaminatedWater);
      waterLower.AddWater(cleanWater, contaminatedWater);
    }
  }
}
