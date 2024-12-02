using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterSourceSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.TickSystem;
using System.Numerics;
using Timberborn.NeedSystem;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldAcumulator : BaseComponent,
                                         ITickableSingleton
  {
    public float acumulator { get; private set; } = 0f;

    private float powerStep = 0.1f;

    private float powerEfficiency => nodePowered?.PowerEfficiency ?? 0f;

    public bool MaxPower { get; private set; } = false;

    public PipeNodePowered nodePowered;

    public event EventHandler OnPowerOff;

    public event EventHandler<float> OnPowerUp;

    public event EventHandler OnPowerDown;

    public event EventHandler OnPowerFull;

    public void Awake()
    {
      nodePowered = GetComponentFast<PipeNodePowered>();
    }

    public void UpdateAcumulator()
    {
      var power = powerEfficiency > 0f
        ? acumulator * (1f + (powerStep * powerEfficiency))
        : acumulator * (1f - powerStep);
      power = Mathf.Min(
        Mathf.Max(power, 1f),
      0f);
      acumulator = power;
      MaxPower = acumulator > 0f;
    }

    public void CheckPower()
    {
      if (acumulator == 0f)
        this.OnPowerOff?.Invoke(this, EventArgs.Empty);
      if (powerEfficiency < 0f)
        this.OnPowerDown?.Invoke(this, EventArgs.Empty);
      else
        this.OnPowerUp?.Invoke(this, acumulator);
      if (acumulator == 1f)
        this.OnPowerFull?.Invoke(this, EventArgs.Empty);
    }

    public void Tick()
    {
      UpdateAcumulator();
      CheckPower();
    }
  }
}
