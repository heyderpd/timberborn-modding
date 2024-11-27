using System;

namespace Mods.OldGopher.Pipe
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
}
