using System.Linq;
using System.Collections;
using System.Collections.Immutable;
using Bindito.Core;
using UnityEngine;
using Timberborn.EntitySystem;
using Timberborn.BlockSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.Persistence;
using System;
using UnityEngine.UI;

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

    private WaterShieldAnimator animator;

    private WaterShieldAcumulator acumulator;

    private Vector3Int coordinate;

    private ImmutableArray<ShieldNode> shieldField;

    private Coroutine routine = null;

    private ShieldState State = ShieldState.DOWN;

    public event EventHandler<float> OnPowerUpdate;

    [SerializeField]
    private int Size;

    [SerializeField]
    private int Height;

    [SerializeField]
    private float ShieldUpSpeed;

    [SerializeField]
    private float ShieldDownSpeed;

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
      animator = GetComponentFast<WaterShieldAnimator>();
      acumulator = GetComponentFast<WaterShieldAcumulator>();
      acumulator.OnPowerOff += OnPowerOff;
      acumulator.OnPowerDown += animator.OnSpeedDown;
      acumulator.OnPowerUp += animator.OnSpeedUp;
      acumulator.OnPowerFull += OnPowerFull;
      animator.OnAnimationAtMax += OnPowerFull;
    }

    public void InitializeEntity() {
      shieldField = waterShieldService.DiscoveryShieldField(Size, Height, blockObject);
    }

    public void DeleteEntity() { }

    public void OnEnterFinishedState()
    {
      ((Behaviour)this).enabled = true;
    }

    public void OnExitFinishedState()
    {
      ((Behaviour)this).enabled = false;
    }

    public void Save(IEntitySaver entitySaver) { }

    [BackwardCompatible(2023, 9, 22)]
    public void Load(IEntityLoader entityLoader) { }

    public IEnumerator ActivateShieldRoutine(bool activate)
    {
      Debug.Log($"ActivateShield start activate={activate} shieldField={shieldField.Count()}");
      State = activate ? ShieldState.TO_UP : ShieldState.TO_DOWN;
      var origin = blockObject.Coordinates;
      foreach (var node in shieldField)
      {
        Debug.Log($"ActivateShield.yield coordinate={coordinate} node.active={node.active}");
        if ((activate && node.active) || (!activate && !node.active))
          continue;
        if (activate)
          waterRadar.AddFullObstacle(node.coordinate);
        else
          waterRadar.RemoveFullObstacle(node.coordinate);
        node.active = activate;
        yield return new WaitForSeconds(activate ? ShieldUpSpeed : ShieldDownSpeed);
      }
      State = activate ? ShieldState.UP : ShieldState.DOWN;
      routine = null;
      Debug.Log($"ActivateShield end");
    }

    public void ActivateShield(bool activate)
    {
      if (routine != null)
        StopCoroutine(routine);
      routine = StartCoroutine(ActivateShieldRoutine(activate));
    }

    public void OnPowerOff(object sender, EventArgs evt)
    {
      if (State > ShieldState.TO_DOWN)
        ActivateShield(false);
    }

    public void OnPowerFull(object sender, EventArgs evt)
    {
      if (State < ShieldState.TO_UP && acumulator.MaxPower && animator.AtTopSpeed)
        ActivateShield(true);
    }
  }
}
