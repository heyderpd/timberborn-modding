using System;
using System.Linq;
using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.Animations;
using Timberborn.TimeSystem;
using Timberborn.TickSystem;
using Timberborn.EntitySystem;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldAnimator : BaseComponent,
                                       IInitializableEntity,
                                       ITickableSingleton
  {
    private NonlinearAnimationManager nonlinearAnimationManager;

    [SerializeField]
    private float AntennaSpeed;

    [SerializeField]
    private float CogSpeed;

    [SerializeField]
    private float BladeSpeed;

    private SpeedAnimator Antenna;

    private SpeedAnimator Cog;

    private SpeedAnimator Blade;

    public event EventHandler OnAnimationAtMax;

    private float SpeedFactor = 0f;

    public bool Active
    {
      get
      {
        return (Antenna?.AtTopSpeed ?? false) && (Cog?.AtTopSpeed ?? false) && (Blade?.AtTopSpeed ?? false);
      }
      set
      {
        if (Antenna == null || Cog == null || Blade == null)
          return;
        Antenna.Active = value;
        Cog.Active = value;
        Blade.Active = value;
      }
    }

    public bool AtTopSpeed => (Antenna?.AtTopSpeed ?? false) && (Cog?.AtTopSpeed ?? false) && (Blade?.AtTopSpeed ?? false);

    [Inject]
    public void InjectDependencies(
      NonlinearAnimationManager _nonlinearAnimationManager
    )
    {
      nonlinearAnimationManager = _nonlinearAnimationManager;
    }

    public void Awake()
    {
      Debug.Log("WaterShieldAcumulator.Awake");
      var animators = new List<IAnimator>();
      GetComponentsFast<IAnimator>(animators);
      Antenna = new SpeedAnimator(
        animators.FirstOrDefault(item => item.AnimationName == "#AntennaAnimated"),
        nonlinearAnimationManager,
        AntennaSpeed
      );
      Cog = new SpeedAnimator(
        animators.FirstOrDefault(item => item.AnimationName == "#CogAnimated"),
        nonlinearAnimationManager,
        CogSpeed
      );
      Blade = new SpeedAnimator(
        animators.FirstOrDefault(item => item.AnimationName == "#BladesAnimated"),
        nonlinearAnimationManager,
        BladeSpeed
      );
    }

    public void OnSpeedUp(object sender, float _SpeedFactor)
    {
      Active = true;
      SpeedFactor = _SpeedFactor;
    }

    public void OnSpeedDown(object sender, EventArgs evt)
    {
      Active = false;
    }

    public void InitializeEntity()
    {
      Antenna.Initialize();
      Cog.Initialize();
      Blade.Initialize();
    }

    public void Tick()
    {
      Antenna.Update(SpeedFactor);
      Cog.Update(SpeedFactor);
      Blade.Update(SpeedFactor);
      if (AtTopSpeed)
        this.OnAnimationAtMax?.Invoke(this, EventArgs.Empty);
    }
  }
}
