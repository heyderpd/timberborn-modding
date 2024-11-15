using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class WaterGateConfig
  {
    public static Vector3Int getCoordinates(WaterGateType gate)
    {
      switch (gate)
      {
        case WaterGateType.BACK:
          return new Vector3Int(0, -1, 0);
        case WaterGateType.FRONT:
          return new Vector3Int(0, +1, 0);
        case WaterGateType.LEFT:
          return new Vector3Int(-1, 0, 0);
        case WaterGateType.RIGHT:
          return new Vector3Int(+1, 0, 0);
        case WaterGateType.BOTTON:
          return new Vector3Int(0, 0, -1);
        case WaterGateType.TOP:
          return new Vector3Int(0, 0, +1);
        case WaterGateType.VALVE:
          return new Vector3Int(0, 0,  0);
        case WaterGateType.WATERPUMP:
          return new Vector3Int(0, +1, 0);
        default:
          return new Vector3Int(0, 0, 0);
      }
    }

    public static float getLowerLimitShift(WaterGateType gate)
    {
      switch (gate)
      {
        case WaterGateType.VALVE:
        case WaterGateType.TOP:
          return 0f;
        case WaterGateType.BOTTON:
          return 1f;
        case WaterGateType.FRONT:
        case WaterGateType.BACK:
        case WaterGateType.LEFT:
        case WaterGateType.RIGHT:
          return 0.25f;
        case WaterGateType.WATERPUMP:
          return 0.10f;
        default:
          return 0f;
      }
    }

    public static float getHigthLimitShift(WaterGateType gate)
    {
      switch (gate)
      {
        case WaterGateType.VALVE:
        case WaterGateType.TOP:
          return 0.5f;
        case WaterGateType.BOTTON:
          return 1f;
        case WaterGateType.FRONT:
        case WaterGateType.BACK:
        case WaterGateType.LEFT:
        case WaterGateType.RIGHT:
          return 0.75f;
        case WaterGateType.WATERPUMP:
          return 0.90f;
        default:
          return 1f;
      }
    }

    public static bool IsCompatibleGate(WaterGateType gate, WaterGateType opposite)
    {
      switch (gate)
      {
        case WaterGateType.TOP:
          return opposite == WaterGateType.BOTTON;
        case WaterGateType.BOTTON:
          return opposite == WaterGateType.TOP;
        case WaterGateType.FRONT:
        case WaterGateType.BACK:
        case WaterGateType.LEFT:
        case WaterGateType.RIGHT:
          return opposite == WaterGateType.FRONT
            || opposite == WaterGateType.BACK
            || opposite == WaterGateType.LEFT
            || opposite == WaterGateType.RIGHT;
        case WaterGateType.VALVE:
        default:
          return false;
      }
    }

    public static string getParticleAttachmentId(WaterGateType gate)
    {
      switch (gate)
      {
        case WaterGateType.BACK:
          return "Water1.PipeBox";
        case WaterGateType.FRONT:
          return "Water2.PipeBox";
        case WaterGateType.LEFT:
          return "Water3.PipeBox";
        case WaterGateType.RIGHT:
          return "Water4.PipeBox";
        case WaterGateType.TOP:
          return "Water5.PipeBox";
        case WaterGateType.BOTTON:
          return "Water6.PipeBox";
        case WaterGateType.VALVE:
          return "Water7.PipeBox";
        case WaterGateType.WATERPUMP:
          return "Water8.PipeBox";
        default:
          return "";
      }
    }
  }
}
