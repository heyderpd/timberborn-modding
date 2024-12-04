using System;
using Bindito.Core;
using UnityEngine;
using Timberborn.TimeSystem;
using Timberborn.TickSystem;
using Timberborn.Animations;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldAnimator : TickableComponent
  {
    private NonlinearAnimationManager nonlinearAnimationManager;

    [SerializeField]
    private float AnimatedSpeed = 1f;

    private SpeedAnimator Animated;

    public event EventHandler OnAnimationAtMax;

    private float SpeedFactor = 0f;

    private bool SpeedChanged = false;

    public bool Active
    {
      get
      {
        return (Animated?.AtTopSpeed ?? false);
      }
      set
      {
        if (Animated == null)
          return;
        Animated.Active = value;
      }
    }

    public bool AtTopSpeed => Animated?.AtTopSpeed ?? false;

    [Inject]
    public void InjectDependencies(
      NonlinearAnimationManager _nonlinearAnimationManager
    )
    {
      nonlinearAnimationManager = _nonlinearAnimationManager;
    }

    public void Awake()
    {
      Animated = new SpeedAnimator(
        GetComponentInChildren<IAnimator>(true),
        nonlinearAnimationManager,
        AnimatedSpeed
      );
      Animated.Initialize();
    }

    public void OnSpeedChange(object sender, float _SpeedFactor)
    {
      if (SpeedFactor == _SpeedFactor)
        return;
      SpeedFactor = _SpeedFactor;
      Active = _SpeedFactor > 0f;
      SpeedChanged = true;
    }

    public override void Tick()
    {
      if (!SpeedChanged)
        return;
      SpeedChanged = false;
      Animated.Update(SpeedFactor);
      if (AtTopSpeed)
        this.OnAnimationAtMax?.Invoke(this, EventArgs.Empty);
    }
  }
}
