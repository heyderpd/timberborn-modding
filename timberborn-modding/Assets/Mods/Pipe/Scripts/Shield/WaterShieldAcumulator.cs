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
    private int SecondsToFullPower = 30;

    private float powerStep;

    private bool powerChanged = false;

    private float powerEfficiency => nodePowered?.PowerEfficiency ?? 0f;

    private bool unpowered => powerEfficiency == 0f;

    private bool powered => powerEfficiency == 1f;

    public bool MaxPower => acumulator >= 1f;

    public bool FailurePower => acumulator < 0.5f;

    public bool NonePower => acumulator == 0f;

    private PipeNodePowered nodePowered;

    public event EventHandler OnPowerOff;

    public event EventHandler<float> OnPowerChange;

    public event EventHandler OnPowerOn;

    public void Awake()
    {
      nodePowered = GetComponentFast<PipeNodePowered>();
      powerStep = (1f / SecondsToFullPower) * Time.fixedDeltaTime;
    }

    public void UpdateAcumulator()
    {
      Debug.Log($"WaterShieldAcumulator.UpdateAcumulator powerEfficiency={powerEfficiency}");
      var power = powered
        ? acumulator + (powerStep * powerEfficiency)
        : acumulator - powerStep;
      power = Mathf.Max(
        Mathf.Min(power, 1f),
      0f);
      powerChanged = acumulator != power;
      acumulator = power;
    }

    public void CheckPower()
    {
      if (NonePower || (unpowered && FailurePower))
        this.OnPowerOff?.Invoke(this, EventArgs.Empty);
      if (powerChanged)
      {
        powerChanged = false;
        this.OnPowerChange?.Invoke(this, acumulator);
      }
      if (powered && MaxPower)
        this.OnPowerOn?.Invoke(this, EventArgs.Empty);
    }

    public void OnEnterFinishedState()
    {
      nodePowered.EnablePowerConsumption();
    }

    public void OnExitFinishedState()
    {
      nodePowered.DisablePowerConsumption();
    }

    public override void Tick()
    {
      UpdateAcumulator();
      CheckPower();
    }
  }
}
