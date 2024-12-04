using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
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
                               IFinishedStateListener
  {
    private WaterRadar waterRadar;

    private WaterShieldService waterShieldService;

    private BlockObject blockObject;

    private WaterShieldAnimator animator;

    private WaterShieldAcumulator acumulator;

    private Vector3Int coordinate;

    private bool shieldLoading = false;

    private ImmutableArray<ShieldNode> shieldField;

    private Coroutine routine = null;

    private ShieldState State = ShieldState.DOWN;

    private bool hasShieldField => !shieldField.IsDefault && shieldField.Count() > 0;

    private bool canTurnOnShield => hasShieldField && State < ShieldState.TO_UP;

    private bool canTurnOffShield => hasShieldField && State > ShieldState.TO_DOWN;


    public event EventHandler<float> OnPowerUpdate;

    [SerializeField]
    private int Size;

    [SerializeField]
    private int Height;

    [SerializeField]
    private int SecondsToShieldUp;

    [SerializeField]
    private int SecondsToShieldDown;

    private float waitStepToShieldUp = 0.3f;

    private float waitStepToShieldDown = 0.3f;

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
      Debug.Log("WaterShield.Awake CALL");
      ((Behaviour)this).enabled = false;
      blockObject = GetComponentFast<BlockObject>();
      animator = GetComponentFast<WaterShieldAnimator>();
      acumulator = GetComponentFast<WaterShieldAcumulator>();
    }

    public void InitializeEntity()
    {
      Debug.Log("WaterShield.InitializeEntity CALL");
      acumulator.OnPowerOff += OnPowerOff;
      acumulator.OnPowerChange += animator.OnSpeedChange;
      acumulator.OnPowerOn += OnPowerOn;
    }

    public void OnEnterFinishedState()
    {
      Debug.Log($"WaterShield.OnEnterFinishedState CALL shieldLoading={shieldLoading}");
      ((Behaviour)this).enabled = true;
      if (shieldLoading)
        DiscoveryShieldField();
      else
        StartCoroutine(DiscoveryShieldField());
    }

    public void OnExitFinishedState()
    {
      Debug.Log("WaterShield.OnExitFinishedState CALL");
      ((Behaviour)this).enabled = false;
      ChangeShield(false);
    }

    public void Save(IEntitySaver entitySaver) {
    }

    [BackwardCompatible(2023, 9, 22)]
    public void Load(IEntityLoader entityLoader)
    {
      shieldLoading = true;
    }

    private IEnumerator DiscoveryShieldField()
    {
      Debug.Log("WaterShield.DiscoveryShieldField START");
      var totalLimit = Size * Size;
      var Coordinates = new List<ShieldNode>();
      foreach(var column in waterShieldService.DiscoveryShieldField(totalLimit, Height, blockObject))
      {
        Coordinates.Add(column);
        yield return null;
      }
      float count = Coordinates.Count();
      waitStepToShieldUp = (SecondsToShieldUp / count) * Time.fixedDeltaTime;
      waitStepToShieldDown = (SecondsToShieldDown / count) * Time.fixedDeltaTime;
      shieldField = Coordinates.ToImmutableArray();
      Debug.Log($"WaterShield.DiscoveryShieldField END shieldField={count} waitStepToShieldUp={waitStepToShieldUp} waitStepToShieldDown={waitStepToShieldDown}");
    }

    private IEnumerator ActivateRoutine(bool wait = true)
    {
      Debug.Log("WaterShield.ActivateRoutine CALL");
      State = ShieldState.TO_UP;
      foreach (var node in shieldField.Where(item => item.Inactive))
      {
        node.SetActive(waterRadar);
        if (wait)
          yield return new WaitForSeconds(waitStepToShieldUp);
      }
      State = ShieldState.UP;
      routine = null;
    }

    private IEnumerator DeactivateRoutine(bool wait = true)
    {
      Debug.Log("WaterShield.DeactivateRoutine CALL");
      State = ShieldState.TO_DOWN;
      foreach (var node in shieldField.Reverse().Where(item => item.Active))
      {
        node.SetInactive(waterRadar);
        if (wait)
          yield return new WaitForSeconds(waitStepToShieldDown);
      }
      State = ShieldState.DOWN;
      routine = null;
    }

    public void ChangeShield(bool activate)
    {
      Debug.Log("WaterShield.ChangeShield CALL");
      if (routine != null)
        StopCoroutine(routine);
      routine = StartCoroutine(activate
        ? ActivateRoutine()
        : DeactivateRoutine());
    }

    private void OnPowerOff(object sender, EventArgs evt)
    {
      Debug.Log("WaterShield.OnPowerOff TRY");
      if (canTurnOffShield)
        ChangeShield(false);
    }

    private void OnPowerOn(object sender, EventArgs evt)
    {
      Debug.Log("WaterShield.OnPowerFull TRY");
      if (canTurnOnShield)
        ChangeShield(true);
    }
  }
}
