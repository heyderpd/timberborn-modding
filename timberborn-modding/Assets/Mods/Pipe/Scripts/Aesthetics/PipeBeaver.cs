using System;
using UnityEngine;
using System.Collections;
using Bindito.Core;
using Timberborn.Animations;
using Timberborn.TimeSystem;
using Timberborn.BaseComponentSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeBeaver : BaseComponent
  {
    private NonlinearAnimationManager nonlinearAnimationManager;

    private WaterRadar waterRadar;

    private IAnimator animator;

    private static int randomChance = 1;

    [Inject]
    public void InjectDependencies(
      NonlinearAnimationManager _nonlinearAnimationManager,
      WaterRadar _waterRadar
    )
    {
      nonlinearAnimationManager = _nonlinearAnimationManager;
      waterRadar = _waterRadar;
    }

    public void Awake()
    {
      animator = GetComponentInChildren<IAnimator>();
    }

    static public bool GetRandomChance()
    {
      return UnityEngine.Random.value <= randomChance / 100f;
    }

    public void WildBeaverAppears(WaterGate gate = null)
    {
      try
      {
        if (animator == null)
          return;
        if (gate != null && waterRadar.IsBlocked(gate.coordinates))
          return;
        StartCoroutine(Animation());
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [PipeBeaver.WildBeaverAppears] err={err}");
      }
    }

    private IEnumerator Animation()
    {
      animator.Speed = nonlinearAnimationManager.SpeedMultiplier;
      animator.SetTime(0);
      animator.Enabled = true;
      yield return new WaitForSeconds(4);
      animator.Enabled = false;
      animator.SetTime(0);
    }
  }
}
