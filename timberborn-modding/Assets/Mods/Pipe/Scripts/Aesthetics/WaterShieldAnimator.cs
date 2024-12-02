using System.Linq;
using System.Collections.Generic;
using Bindito.Core;
using Timberborn.Animations;
using Timberborn.TimeSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.TickSystem;
using System;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldAnimator : BaseComponent,
                                       ITickableSingleton
  {
    private NonlinearAnimationManager nonlinearAnimationManager;

    private SpeedAnimator Antenna;

    private SpeedAnimator Cog;

    private SpeedAnimator Blade;

    public event EventHandler OnAnimationAtMax;

    [NonSerialized]
    public float SpeedFactor = 0f;

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
      var animators = new List<IAnimator>();
      GetComponentsFast<IAnimator>(animators);
      Antenna = new SpeedAnimator(
        animators.FirstOrDefault(item => item.AnimationName == "#AntennaAnimated"),
        nonlinearAnimationManager,
        0.4f
      );
      Cog = new SpeedAnimator(
        animators.FirstOrDefault(item => item.AnimationName == "#CogAnimated"),
        nonlinearAnimationManager,
        0.05f
      );
      Blade = new SpeedAnimator(
        animators.FirstOrDefault(item => item.AnimationName == "#BladesAnimated"),
        nonlinearAnimationManager,
        1.0f
      );
    }

    public void OnEnterFinishedState()
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

    public void OnSpeedUp(object sender, float _SpeedFactor)
    {
      Active = true;
      SpeedFactor = _SpeedFactor;
    }

    public void OnSpeedDown(object sender, EventArgs evt)
    {
      Active = false;
    }
  }
}
