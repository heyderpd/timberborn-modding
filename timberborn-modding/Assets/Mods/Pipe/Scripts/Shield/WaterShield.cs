using System;
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
      Debug.Log("WaterShield.Awake");
      ((Behaviour)this).enabled = false;
      blockObject = GetComponentFast<BlockObject>();
      animator = GetComponentFast<WaterShieldAnimator>();
      acumulator = GetComponentFast<WaterShieldAcumulator>();
    }

    public void InitializeEntity()
    {
      Debug.Log("WaterShield.InitializeEntity");
      acumulator.OnPowerOff += OnPowerOff;
      acumulator.OnPowerDown += animator.OnSpeedDown;
      acumulator.OnPowerUp += animator.OnSpeedUp;
      acumulator.OnPowerFull += OnPowerFull;
      animator.OnAnimationAtMax += OnPowerFull;
      shieldField = waterShieldService.DiscoveryShieldField(Size, Height, blockObject);
    }

    public void DeleteEntity()
    {
      Debug.Log("WaterShield.DeleteEntity");
      ActivateShield(false);
    }

    public void OnEnterFinishedState()
    {
      Debug.Log("WaterShield.OnEnterFinishedState");
      ((Behaviour)this).enabled = true;
    }

    public void OnExitFinishedState()
    {
      Debug.Log("WaterShield.OnExitFinishedState");
      ((Behaviour)this).enabled = false;
    }

    public void Save(IEntitySaver entitySaver) { }

    [BackwardCompatible(2023, 9, 22)]
    public void Load(IEntityLoader entityLoader) { }

    private IEnumerator ActivateRoutine()
    {
      State = ShieldState.TO_UP;
      foreach (var node in shieldField.Where(item => !item.active))
      {
        waterRadar.AddFullObstacle(node.coordinate);
        node.active = true;
        yield return new WaitForSeconds(ShieldUpSpeed);
      }
      State = ShieldState.UP;
      routine = null;
    }

    private IEnumerator DeactivateRoutine()
    {
      State = ShieldState.TO_DOWN;
      foreach (var node in shieldField.Reverse().Where(item => item.active))
      {
        waterRadar.RemoveFullObstacle(node.coordinate);
        node.active = false;
        yield return new WaitForSeconds(ShieldDownSpeed);
      }
      State = ShieldState.DOWN;
      routine = null;
    }

    public void ActivateShield(bool activate)
    {
      if (routine != null)
        StopCoroutine(routine);
      routine = StartCoroutine(activate
        ? ActivateRoutine()
        : DeactivateRoutine());
    }

    private void OnPowerOff(object sender, EventArgs evt)
    {
      if (State > ShieldState.TO_DOWN)
        ActivateShield(false);
    }

    private void OnPowerFull(object sender, EventArgs evt)
    {
      if (!((Behaviour)this).enabled)
        return;
      if (State < ShieldState.TO_UP && acumulator.MaxPower && animator.AtTopSpeed)
        ActivateShield(true);
    }
  }
}
