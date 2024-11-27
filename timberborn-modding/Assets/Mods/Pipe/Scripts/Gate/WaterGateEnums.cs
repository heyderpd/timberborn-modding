using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal enum WaterGateState
  {
    EMPTY,
    BLOCKED,
    CONNECTED
  }

  internal enum WaterGateMode
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

  internal enum WaterGateType
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
}
