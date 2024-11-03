namespace Mods.OldGopher.Pipe.Scripts
{
  internal enum WaterGateState
  {
    EMPTY,
    BLOCKED,
    CONNECTED
  }

  internal enum WaterGateFlow
  {
    STOP,
    IN,
    OUT
  }

  internal enum WaterGateSide
  {
    FRONT,
    BACK,
    LEFT,
    RIGHT,
    TOP,
    BOTTON
  }

  public readonly struct WaterAddition
  {
    public float Water { get; }

    public float ContaminatedPercentage { get; }

    public WaterAddition(float _Water, float _ContaminatedPercentage)
    {
      Water = _Water;
      ContaminatedPercentage = _ContaminatedPercentage;
    }
  }

  internal static class WaterGateConfig
  {
    public static float getFloorShift(WaterGateSide gate)
    {
      switch (gate)
      {
        case WaterGateSide.TOP:
          return 0f;
        case WaterGateSide.BOTTON:
          return 1f;
        case WaterGateSide.FRONT:
        case WaterGateSide.BACK:
        case WaterGateSide.LEFT:
        case WaterGateSide.RIGHT:
        default:
          return 0.25f;
      }
    }

    public static float getCeilingShift(WaterGateSide gate)
    {
      switch (gate)
      {
        case WaterGateSide.TOP:
          return 0.5f;
        case WaterGateSide.BOTTON:
          return 1f;
        case WaterGateSide.FRONT:
        case WaterGateSide.BACK:
        case WaterGateSide.LEFT:
        case WaterGateSide.RIGHT:
        default:
          return 0.75f;
      }
    }

    public static bool IsCompatibleGate(WaterGateSide gate, WaterGateSide opposite)
    {
      switch (gate)
      {
        case WaterGateSide.TOP:
          return opposite == WaterGateSide.BOTTON;
        case WaterGateSide.BOTTON:
          return opposite == WaterGateSide.TOP;
        case WaterGateSide.FRONT:
        case WaterGateSide.BACK:
        case WaterGateSide.LEFT:
        case WaterGateSide.RIGHT:
          return opposite == WaterGateSide.FRONT
            || opposite == WaterGateSide.BACK
            || opposite == WaterGateSide.LEFT
            || opposite == WaterGateSide.RIGHT;
        default:
          return false;
      }
    }

    public static string getParticleAttachmentId(WaterGateSide gate)
    {
      switch (gate)
      {
        case WaterGateSide.BACK:
          return "Water1.PipeBox";
        case WaterGateSide.FRONT:
          return "Water2.PipeBox";
        case WaterGateSide.LEFT:
          return "Water3.PipeBox";
        case WaterGateSide.RIGHT:
          return "Water4.PipeBox";
        case WaterGateSide.TOP:
          return "Water5.PipeBox";
        case WaterGateSide.BOTTON:
          return "Water6.PipeBox";
        default:
          return "";
      }
    }
  }
}
