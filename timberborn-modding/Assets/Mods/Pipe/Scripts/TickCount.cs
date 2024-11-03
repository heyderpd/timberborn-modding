using System;
using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class OldGopherLog
  {
    public static readonly bool enabled = true;

    public static void Log(string text)
    {
      if (enabled)
        Debug.Log(text);
    }
  }

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
