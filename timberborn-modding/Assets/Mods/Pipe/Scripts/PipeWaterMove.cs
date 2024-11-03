using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Mods.Pipe.Scripts
{
  /*
  internal class PipeWaterMove : TickableComponent,
                                 IInitializableEntity,
                                 IFinishedStateListener,
                                 IPersistentEntity {

    private bool ready = false;

    private float _waterDirection;

    private float _waterDiffLimit = 0.01f;

    private float _waterCapacity = 0.50f;

    private float _debugWaterDiff;

    private List<PipeWaterInput> _waterInputs;

    private PipeWaterInput _waterInputA;

    private PipeWaterInput _waterInputB;

    private PipeWaterLink _waterLink;

    public void Awake()
    {
      _waterInputs = new List<PipeWaterInput>();
      GetComponentsFast<PipeWaterInput>(_waterInputs);
      _waterInputA = _waterInputs.First();
      _waterInputB = _waterInputs.Last();
      _waterLink = GetComponentFast<PipeWaterLink>();
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

    public bool WaterChangeDirection()
    {
      float waterA = _waterInputA.Direction * 10;
      float waterB = _waterInputB.Direction;
      var waterDirection = waterA + waterB;
      bool whateChanged = waterDirection != _waterDirection;
      _waterDirection = waterDirection;
      return whateChanged;
    }

    public override void Tick()
    {
      if (!ready)
      {
        return;
      }
      _waterInputA.UpdateAvailableWaters();
      _waterInputB.UpdateAvailableWaters();
      float waterA = _waterInputA.Water;
      float waterB = _waterInputB.Water;
      if (waterA <= 0 && waterB <= 0)
      {
        _waterInputA.StopWater();
        _waterInputB.StopWater();
        return;
      }
      float waterDiff = Mathf.Abs(waterA - waterB) / 2;
      _debugWaterDiff = waterDiff;
      if (waterDiff < _waterDiffLimit)
      {
        _waterInputA.StopWater();
        _waterInputB.StopWater();
        return;
      }
      if (waterA > waterB)
      {
        _waterInputA.OutWater();
        _waterInputB.InWater();
      }
      else
      {
        _waterInputA.InWater();
        _waterInputB.OutWater();
      }
      if (WaterChangeDirection())
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
      waterHigh.RemoveWater(availableWater, 0f);
      waterLower.AddWater(availableWater, 0f);
      /*float contaminatedWater = waterHigh.AvailableContaminatedWater;
      float cleanWater = availableWater - contaminatedWater;
      waterHigh.RemoveWater(cleanWater, contaminatedWater);
      waterLower.AddWater(cleanWater, contaminatedWater); * /
    }

    public string GetInfo()
    {
      if (_waterInputs == null)
        return "empty";
      string letters = "";
      letters += ", dir = " + _waterDirection;
      letters += ", debug = " + _debugWaterDiff;
      letters += ", inputs = ";
      foreach (var _waterInput in _waterInputs)
      {
        /*if (letters != "")
        {
          letters += ", ";
        } * /
        letters += ", ";
        letters += _waterInput.GetInfo();
      }
      return letters;
    }
  }
  */
}
