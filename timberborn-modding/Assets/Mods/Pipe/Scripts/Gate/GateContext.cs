using System;
using System.Collections.Generic;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class GateContextInteraction
  {
    public GateContext leftSide;

    public GateContext rightSide;

    public GateContext pressureDirection { get; private set; }

    public float pressure { get; private set; }

    public void setPressure(GateContext reference, float value)
    {
      pressure = MathF.Abs(value);
      var oposite = GetOposite(reference);
      if (value >= 0f)
      {
        reference.pressureSum += value;
        oposite.pressureSum -= value;
        pressureDirection = oposite;
      }
      else
      {
        reference.pressureSum += value;
        oposite.pressureSum -= value;
        pressureDirection = reference;
      }
    }

    public float quota { get; private set; }

    public void setQuota(float value)
    {
      quota = MathF.Abs(value);
      getDelivery().quotaSum += quota;
    }

    public float waterOferted { get; private set; }

    public void setWaterOferted(float value)
    {
      waterOferted = value;
      getRequester().waterOfertedSum += waterOferted;
    }

    public float waterBlockedByPump { get; private set; }

    public void setWaterBlockedByPump(float value)
    {
      waterBlockedByPump = value;
      getRequester().waterBlockedByPumpSum += waterBlockedByPump;
    }

    public float contamination { get; private set; }

    public void setContamination(float value)
    {
      contamination = value;
      getRequester().contaminationSum += contamination;
    }

    public GateContextInteraction(GateContext _leftSide, GateContext _rightSide)
    {
      leftSide = _leftSide;
      rightSide = _rightSide;
      leftSide.pumpActivate += rightSide.OnPumpActivateAdded;
      rightSide.pumpActivate += leftSide.OnPumpActivateAdded;
      Reset();
    }

    public void Reset()
    {
      pressureDirection = null;
      pressure = 0f;
      quota = 0f;
      waterOferted = 0f;
      waterBlockedByPump = 0f;
      contamination = 0f;
    }

    public GateContext GetOposite(GateContext context)
    {
      if (context == leftSide)
        return rightSide;
      else
        return leftSide;
    }

    public bool isDelivery(GateContext context)
    {
      return pressureDirection != context;
    }

    public bool isRequester(GateContext context)
    {
      return !isDelivery(context);
    }

    public GateContext getDelivery()
    {
      return isDelivery(leftSide) ? leftSide : rightSide;
    }

    public GateContext getRequester()
    {
      return isRequester(leftSide) ? leftSide : rightSide;
    }
  }

  internal class GateContext
  {
    public readonly WaterGate gate;

    public Dictionary<GateContext, GateContextInteraction> Interactions = new Dictionary<GateContext, GateContextInteraction>();

    public float pressureSum;

    public float quotaSum;

    public float waterOfertedSum;

    public float waterBlockedByPumpSum;

    public float contaminationSum;

    public float WaterUsed;

    public float WaterMove;

    public float Contamination;

    public int pumpRequested;

    public bool forceRequester;

    public bool forceDelivery;

    public event EventHandler pumpActivate;

    public GateContext(WaterGate _gate)
    {
      gate = _gate;
    }

    public void Reset()
    {
      pressureSum = 0f;
      quotaSum = 0f;
      waterOfertedSum = 0f;
      waterBlockedByPumpSum = 0f;
      contaminationSum = 0f;
      WaterUsed = 0f;
      WaterMove = 0f;
      Contamination = 0f;
      pumpRequested = 0;
      forceRequester = false;
      forceDelivery = false;
      foreach (var interaction in Interactions.Values)
      {
        interaction.Reset();
      }
    }

    public void Clear()
    {
      Interactions.Clear();
    }

    public void AddPumpRequested(float water)
    {
      ModUtils.Log($"[context.AddPumpRequested] 01 water={water} IsWaterPump={gate.IsWaterPump} pumpRequested={pumpRequested}");
      if (water != 0f && gate.IsWaterPump)
        pumpRequested += 1;
      ModUtils.Log($"[context.AddPumpRequested] 02 water={water} IsWaterPump={gate.IsWaterPump} pumpRequested={pumpRequested}");
    }

    public void SendPumpActivateEvent(float water)
    {
      ModUtils.Log($"[context.SendPumpActivateEvent] 01 water={water}");
      if (water != 0f)
        this.pumpActivate?.Invoke(this, EventArgs.Empty);
    }

    public void OnPumpActivateAdded(object sender, EventArgs e)
    {
      ModUtils.Log($"[context.OnPumpActivateAdded] 01 IsWaterPump={gate.IsWaterPump}");
      if (!gate.IsWaterPump)
        return;
      var context = (GateContext)sender;
      ModUtils.Log($"[context.OnPumpActivateAdded] 02 hasContext={context != null}");
      if (Interactions.TryGetValue(context, out var oposite))
      {
        ModUtils.Log($"[context.OnPumpActivateAdded] 03 waterOferted={oposite?.waterBlockedByPump ?? 0f}");
        AddPumpRequested(oposite?.waterBlockedByPump ?? 0f);
      }
    }

    private void SwitchPowerConsumption(bool enabled)
    {
      if (enabled)
        gate.powered?.EnablePowerConsumption();
      else
        gate.powered?.DisablePowerConsumption();
    }

    public void checkPumpRequested()
    {
      SwitchPowerConsumption(pumpRequested > 0);
      pumpRequested = 0;
    }
  }
}
