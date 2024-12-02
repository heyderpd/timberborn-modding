using System;
using UnityEngine;
using Timberborn.Animations;
using Timberborn.TimeSystem;

namespace Mods.OldGopher.Pipe
{
  internal class SpeedAnimator
  {
    private float MaxSpeed;

    private float MinSpeed = 0.001f;

    private float Speed = 0f;

    private float SpeedStep = 1.1f;

    public bool Active = false;

    public bool AtTopSpeed { get; private set; } = false;

    private IAnimator Animator;

    private NonlinearAnimationManager nonlinearAnimationManager;

    public event EventHandler OnPowerOff;

    public event EventHandler OnPowerOn;

    public SpeedAnimator(
      IAnimator _Animator,
      NonlinearAnimationManager _nonlinearAnimationManager,
      float _MaxSpeed = 1f
    )
    {
      Animator = _Animator;
      nonlinearAnimationManager = _nonlinearAnimationManager;
      MaxSpeed = _MaxSpeed;
    }

    private float CalculeSpeed(float SpeedFactor)
    {
      var _speed = Active
        ? Speed * SpeedStep * SpeedFactor
        : Speed / SpeedStep;
      _speed = Mathf.Min(
        Mathf.Max(_speed, MaxSpeed),
      0f);
      _speed = _speed > MinSpeed ? _speed : 0f;
      AtTopSpeed = _speed >= MaxSpeed;
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
