using System;
using UnityEngine;
using Timberborn.Animations;
using Timberborn.TimeSystem;

namespace Mods.OldGopher.Pipe
{
  internal class SpeedAnimator
  {
    private float SpeedMax = 1f;

    private float SpeedStep = 0.1f;

    public float Speed { get; private set; } = 0f;

    public bool Active = false;

    public bool AtTopSpeed => Speed >= SpeedMax;

    private IAnimator Animator;

    private NonlinearAnimationManager nonlinearAnimationManager;

    public event EventHandler OnPowerOff;

    public event EventHandler OnPowerOn;

    public SpeedAnimator(
      IAnimator _Animator,
      NonlinearAnimationManager _nonlinearAnimationManager,
      float _SpeedMax
    )
    {
      Animator = _Animator;
      nonlinearAnimationManager = _nonlinearAnimationManager;
      SpeedMax = _SpeedMax;
    }

    private float CalculeSpeed(float SpeedFactor)
    {
      var _speed = Active
        ? Speed + SpeedStep
        : Speed - SpeedStep;
      _speed = Mathf.Max(
        Mathf.Min(_speed, SpeedMax * SpeedFactor),
      0f);
      return _speed;
    }

    public void Initialize()
    {
      Update(0f);
      Animator?.SetTime(0);
    }

    public void Update(float SpeedFactor)
    {
      if (Animator == null)
        return;
      Speed = CalculeSpeed(SpeedFactor);
      Animator.Speed = Speed * nonlinearAnimationManager.SpeedMultiplier;
      Animator.Enabled = Speed > 0f;
    }
  }
}
