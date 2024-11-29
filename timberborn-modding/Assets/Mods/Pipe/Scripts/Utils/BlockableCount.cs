using System.Collections.Generic;

namespace Mods.OldGopher.Pipe
{
  internal class BlockableCount<T>
  {
    private readonly Dictionary<T, int> Blockers = new Dictionary<T, int>();

    public void Block(T reference)
    {
      if (Blockers.ContainsKey(reference))
        Blockers[reference] += 1;
      else
        Blockers.Add(reference, 1);
    }

    public void Unblock(T reference)
    {
      if (Blockers.TryGetValue(reference, out var value))
      {
        value -= 1;
        if (value > 0)
          Blockers[reference] = value;
        else
          Blockers.Remove(reference);
      }
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
