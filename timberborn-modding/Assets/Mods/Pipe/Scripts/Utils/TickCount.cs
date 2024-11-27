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
  internal class TickCount
  {
    private int ticks = 0;

    private int maxTicks;

    public TickCount(int maxTicks = 0)
    {
      SetMaxTicks(maxTicks);
    }

    public void SetMaxTicks(int _maxTicks)
    {
      maxTicks = _maxTicks;
    }

    public bool Skip()
    {
      if (maxTicks == 0)
        return false;
      ticks += 1;
      if (ticks >= maxTicks)
      {
        ticks = 0;
        return false;
      }
      return true;
    }
  }
}