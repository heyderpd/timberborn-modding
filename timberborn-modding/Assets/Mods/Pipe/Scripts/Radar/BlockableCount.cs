using System.Collections.Generic;

namespace Mods.OldGopher.Pipe
{
  internal class BlockableCount<T>
  {
    private readonly Dictionary<T, int> Blockers = new Dictionary<T, int>();

    public void Clear()
    {
      Blockers.Clear();
    }

    public bool Block(T reference)
    {
      if (Blockers.ContainsKey(reference))
      {
        Blockers[reference] += 1;
        return false;
      }
      Blockers.Add(reference, 1);
      return true;
    }

    public bool Unblock(T reference)
    {
      if (Blockers.ContainsKey(reference))
      {
        Blockers[reference] -= 1;
        if (Blockers[reference] <= 0)
        {
          Blockers.Remove(reference);
          return true;
        }
      }
      return false;
    }

    public bool Contains(T reference)
    {
      if (Blockers.TryGetValue(reference, out var value))
      {
        return value > 0;
      }
      return false;
    }
  }
}
