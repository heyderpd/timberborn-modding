using System;
using Bindito.Core;
using Timberborn.Animations;
using Timberborn.TimeSystem;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeWaterPumpGear : BaseComponent
  {
    private NonlinearAnimationManager nonlinearAnimationManager;

    private IAnimator animator;

    [Inject]
    public void InjectDependencies(
      NonlinearAnimationManager _nonlinearAnimationManager
    )
    {
      nonlinearAnimationManager = _nonlinearAnimationManager;
    }

    public void Awake()
    {
      animator = GetComponentInChildren<IAnimator>();
      animator.Speed = nonlinearAnimationManager.SpeedMultiplier;
      animator.SetTime(0);
      animator.Enabled = false;
    }

    public void OnAnimationEvent(object sender, WaterAdditionEvent _event)
    {
      try
      {
        if (animator == null)
          return;
        if (_event.Water != 0f)
          animator.Enabled = true;
        else
          animator.Enabled = false;
        if (_event.Water >= 0f)
          animator.Speed = - nonlinearAnimationManager.SpeedMultiplier;
        else
          animator.Speed = nonlinearAnimationManager.SpeedMultiplier;
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [PipeBeaver.PipeWaterPumpGear] err={err}");
      }
    }
  }
}
