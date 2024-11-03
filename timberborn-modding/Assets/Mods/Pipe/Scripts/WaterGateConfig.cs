using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal enum WaterGateState
  {
    EMPTY,
    BLOCKED,
    CONNECTED
  }

  internal enum WaterGateType
  {
    ONLY_IN,
    ONLY_OUT,
    BOTH
  }

  internal enum WaterGateFlow
  {
    STOP,
    IN,
    OUT
  }

  internal enum WaterGateSide
  {
    BACK,
    FRONT,
    LEFT,
    RIGHT,
    BOTTON,
    TOP,
    VALVE,
    WATERPUMP
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
    public static Vector3Int getCoordinates(WaterGateSide gate)
    {
      switch (gate)
      {
        case WaterGateSide.BACK:
          return new Vector3Int(0, -1, 0);
        case WaterGateSide.FRONT:
          return new Vector3Int(0, +1, 0);
        case WaterGateSide.LEFT:
          return new Vector3Int(-1, 0, 0);
        case WaterGateSide.RIGHT:
          return new Vector3Int(+1, 0, 0);
        case WaterGateSide.BOTTON:
          return new Vector3Int(0, 0, -1);
        case WaterGateSide.TOP:
          return new Vector3Int(0, 0, +1);
        case WaterGateSide.VALVE:
          return new Vector3Int(0, 0,  0);
        case WaterGateSide.WATERPUMP:
          return new Vector3Int(0, +1, 0);
        default:
          return new Vector3Int(0, 0, 0);
      }
    }

    public static float getLowerLimitShift(WaterGateSide gate)
    {
      switch (gate)
      {
        case WaterGateSide.VALVE:
        case WaterGateSide.TOP:
          return 0f;
        case WaterGateSide.BOTTON:
          return 1f;
        case WaterGateSide.FRONT:
        case WaterGateSide.BACK:
        case WaterGateSide.LEFT:
        case WaterGateSide.RIGHT:
          return 0.25f;
        case WaterGateSide.WATERPUMP:
          return 0.10f;
        default:
          return 0f;
      }
    }

    public static float getHigthLimitShift(WaterGateSide gate)
    {
      switch (gate)
      {
        case WaterGateSide.VALVE:
        case WaterGateSide.TOP:
          return 0.5f;
        case WaterGateSide.BOTTON:
          return 1f;
        case WaterGateSide.FRONT:
        case WaterGateSide.BACK:
        case WaterGateSide.LEFT:
        case WaterGateSide.RIGHT:
          return 0.75f;
        case WaterGateSide.WATERPUMP:
          return 0.90f;
        default:
          return 1f;
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
        case WaterGateSide.VALVE:
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
        case WaterGateSide.VALVE:
          return "Water7.PipeBox";
        case WaterGateSide.WATERPUMP:
          return "Water8.PipeBox";
        default:
          return "";
      }
    }
  }
}
