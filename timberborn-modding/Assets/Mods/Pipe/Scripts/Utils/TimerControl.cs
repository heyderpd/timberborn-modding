using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.Coordinates;

namespace Mods.OldGopher.Pipe
{
  internal static class TimerControl
  {
    private static float nextTime = 0f;

    private static float fixedDeltaTime = Time.fixedDeltaTime; // = 0.6

    public static bool Skip()
    {
      float now = Time.fixedTime;
      if (now < nextTime)
        return true;
      nextTime = now + fixedDeltaTime;
      return false;
    }
  }
}