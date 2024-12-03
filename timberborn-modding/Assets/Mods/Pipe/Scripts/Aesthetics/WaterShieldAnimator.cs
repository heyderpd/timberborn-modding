using System;
using System.Linq;
using System.Collections.Generic;
using Bindito.Core;
using UnityEngine;
using Timberborn.TimeSystem;
using Timberborn.TickSystem;
using Timberborn.BlockSystem;
using Timberborn.Animations;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldAnimator : TickableComponent,
                                       IFinishedStateListener
  {
    private NonlinearAnimationManager nonlinearAnimationManager;

    [SerializeField]
    private float AntennaSpeed = 0.4f;

    [SerializeField]
    private float CogSpeed = 0.05f;

    [SerializeField]
    private float BladeSpeed = 1.0f;

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
      Debug.Log("WaterShieldAnimator.Awake");
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

    public void OnEnterFinishedState()
    {
      Debug.Log("WaterShieldAnimator.OnEnterFinishedState");
      Antenna.Initialize();
      Cog.Initialize();
      Blade.Initialize();
    }

    public void OnExitFinishedState() { }

    public override void Tick()
    {
      Antenna.Update(SpeedFactor);
      Cog.Update(SpeedFactor);
      Blade.Update(SpeedFactor);
      if (AtTopSpeed)
        this.OnAnimationAtMax?.Invoke(this, EventArgs.Empty);
    }
  }
}
