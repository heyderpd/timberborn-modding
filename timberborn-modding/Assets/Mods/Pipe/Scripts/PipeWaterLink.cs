using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using UnityEngine;
using System.Collections.Generic;
using Timberborn.MechanicalSystem;
using Timberborn.WaterBuildings;
using System.Linq;
using Timberborn.SingletonSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;

namespace Mods.Pipe.Scripts
{
  internal class PipeWaterLink : TickableComponent,
                                 IInitializableEntity,
                                 IFinishedStateListener,
                                 IPersistentEntity
  {
    private EventBus _eventBus;

    private PipeWaterGroup group = new PipeWaterGroup();

    public List<PipeWaterInput> waterInputs { get; private set; }

    public bool inputsEnabled { get; private set; }

    [Inject]
    public void InjectDependencies(EventBus eventBus)
    {
      _eventBus = eventBus;
    }

    public void Awake()
    {
      ((Behaviour)this).enabled = false;
      waterInputs = new List<PipeWaterInput>();
      GetComponentsFast<PipeWaterInput>(waterInputs);
      CheckNear();
      group.Add(this);
    }

    public void InitializeEntity() { }

    public void DeleteEntity() { }

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

    public override void Tick()
    {
      group.Tick();
    }

    public void SetGroup(PipeWaterGroup _group)
    {
      group.Remove(this);
      group = _group;
      group.Add(this);
    }

    public void Connect(PipeWaterLink _link)
    {
      group.Union(_link.group);
    }

    public void OnDeleteNode()
    {
      group.Remove(this);
      group = null;
    }

    public void CheckNear()
    {
      var enabled = true;
      foreach (var input in waterInputs)
      {
        input.CheckClearInput();
        enabled = enabled && input.inputEnabled;
      }
      inputsEnabled = enabled;
      inputsEnabled = true;
    }

    /*[OnEvent]
    public void OnBlockObjectSet(BlockObjectSetEvent blockObjectSetEvent)
    {
      // blockObjectSetEvent.BlockObject.Coordinates; usar para verificar somente os realmente proximos
      CheckNear();
    }

    [OnEvent]
    public void OnBlockObjectUnset(BlockObjectUnsetEvent blockObjectUnsetEvent)
    {
      // blockObjectSetEvent.BlockObject.Coordinates; usar para verificar somente os realmente proximos
      CheckNear();
    }*/

    public string GetInfo()
    {
      return group.GetInfo();
    }
  }
}
