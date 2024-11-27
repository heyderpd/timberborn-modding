using System;
using System.Collections.Generic;

namespace Mods.OldGopher.Pipe
{
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
