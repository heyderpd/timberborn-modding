using System;
using UnityEngine;

namespace Mods.Pipe.Scripts
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

  internal static class TimerControl
  {
    private static float lastTime = 0f;

    private static float timeLimit = 0.5f;

    private static float GetNow()
    {
      return Time.fixedTime * Time.fixedDeltaTime;
    }
    
    public static bool Skip()
    {
      try
      {
        float now = GetNow();
        if (lastTime == 0)
        {
          lastTime = now;
          return true;
        }
        float timeDiff = now - lastTime;
        if (timeDiff >= timeLimit)
        {
          lastTime = now;
          return false;
        }
        return true;
      }
      catch (Exception err)
      {
        Debug.Log($"#ERROR [TimerControl.Tick] err={err}");
        lastTime = GetNow();
        return true;
      }
    }
  }
}
