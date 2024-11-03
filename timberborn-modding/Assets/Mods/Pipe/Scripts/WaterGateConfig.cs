namespace Mods.Pipe.Scripts
{
  public static class WaterGateFlow
  {
    public static readonly int STOP = 0;
    public static readonly int IN = 1;
    public static readonly int OUT = 2;
  }

  public enum WaterGateSide
  {
    FRONT,
    BACK,
    LEFT,
    RIGHT,
    TOP,
    BOTTON
  }

  public class WaterGateConfig
  {
    public float flow { get; private set; } = WaterGateFlow.STOP;

    public void SetStop()
    {
      flow = WaterGateFlow.STOP;
    }

    public void SetIn()
    {
      flow = WaterGateFlow.IN;
    }

    public void SetOut()
    {
      flow = WaterGateFlow.OUT;
    }

    public bool IsCompatibleGate(WaterGateSide gate, WaterGateSide opposite)
    {
      switch (gate)
      {
        case WaterGateSide.FRONT:
          return opposite == WaterGateSide.BACK;
        case WaterGateSide.BACK:
          return opposite == WaterGateSide.FRONT;
        case WaterGateSide.LEFT:
          return opposite == WaterGateSide.RIGHT;
        case WaterGateSide.RIGHT:
          return opposite == WaterGateSide.LEFT;
        case WaterGateSide.TOP:
          return opposite == WaterGateSide.BOTTON;
        case WaterGateSide.BOTTON:
          return opposite == WaterGateSide.TOP;
        default:
          return false;
      }
    }
  }
}
