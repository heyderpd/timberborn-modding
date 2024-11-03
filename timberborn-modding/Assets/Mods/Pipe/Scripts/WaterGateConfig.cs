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
  }
}
