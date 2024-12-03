using System;
using UnityEngine;
using Timberborn.TickSystem;
using Timberborn.BlockSystem;

namespace Mods.OldGopher.Pipe
{
  internal class WaterShieldAcumulator : TickableComponent,
                                         IFinishedStateListener
  {
    private float acumulator = 0f;

    [SerializeField]
    private int powerTicks = 30;

    private float powerStep;

    private float powerEfficiency => nodePowered?.PowerEfficiency ?? 0f;

    public bool MaxPower => acumulator > 0f;

    private PipeNodePowered nodePowered;

    public event EventHandler OnPowerOff;

    public event EventHandler OnPowerDown;

    public event EventHandler<float> OnPowerUp;

    public event EventHandler OnPowerFull;

    public void Awake()
    {
      nodePowered = GetComponentFast<PipeNodePowered>();
      powerStep = (1f / powerTicks) * Time.fixedDeltaTime;
      Debug.Log($"WaterShieldAcumulator.Awake powerTicks={powerTicks} powerStep={powerStep}");
    }

    public void UpdateAcumulator()
    {
      var power = powerEfficiency > 0f
        ? acumulator + (powerStep * powerEfficiency)
        : acumulator - powerStep;
      power = Mathf.Max(
        Mathf.Min(power, 1f),
      0f);
      acumulator = power;
      Debug.Log($"WaterShieldAcumulator.UpdateAcumulator powerEfficiency={powerEfficiency} powerStep={powerStep} acumulator={acumulator}");
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

    public void OnEnterFinishedState()
    {
      Debug.Log("WaterShieldAcumulator.OnEnterFinishedState");
      nodePowered.EnablePowerConsumption();
    }

    public void OnExitFinishedState()
    {
      Debug.Log("WaterShieldAcumulator.OnExitFinishedState");
      nodePowered.DisablePowerConsumption();
    }

    public override void Tick()
    {
      UpdateAcumulator();
      CheckPower();
    }
  }
}
