using System;
using UnityEngine;
using Timberborn.BaseComponentSystem;
using Timberborn.TickSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldAcumulator : BaseComponent,
                                         IInitializableEntity,
                                         ITickableSingleton
  {
    private float acumulator = 0f;

    private float powerStep = 0.1f;

    private float powerEfficiency => nodePowered?.PowerEfficiency ?? 0f;

    public bool MaxPower => acumulator > 0f;

    private PipeNodePowered nodePowered;

    public event EventHandler OnPowerOff;

    public event EventHandler OnPowerDown;

    public event EventHandler<float> OnPowerUp;

    public event EventHandler OnPowerFull;

    public void Awake()
    {
      Debug.Log("WaterShieldAcumulator.Awake");
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

    public void InitializeEntity()
    {
      nodePowered?.EnablePowerConsumption();
    }

    public void Tick()
    {
      UpdateAcumulator();
      CheckPower();
    }
  }
}
