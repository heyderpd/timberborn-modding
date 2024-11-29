using System.Collections.Generic;
using System;
using Timberborn.BaseComponentSystem;
using Timberborn.Navigation;
using UnityEngine;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Timberborn.BlockSystem;

namespace Mods.OldGopher.Pipe
{
  public class BlockableCount<T>
  {
    private readonly Dictionary<T, int> Blockers = new Dictionary<T, int>();

    public event EventHandler<T> Blocked;

    public event EventHandler<T> Unblocked;

    public event EventHandler BuildingBlocked;

    public event EventHandler BuildingUnblocked;

    private void BlockEvent(object sender, T reference)
    {
      if (Blockers.ContainsKey(reference))
        Blockers[reference] += 1;
      else
        Blockers.Add(reference, 1);
      if (Blockers.TryGetValue(reference, out var value))
      {
        if (value == 1)
          BuildingBlocked?.Invoke(this, EventArgs.Empty);
      }
    }

    private void UnblockEvent(object sender, T reference)
    {
      if (Blockers.TryGetValue(reference, out var value))
      {
        value -= 1;
        if (value > 0)
          Blockers[reference] = value;
        else
        {
          Blockers.Remove(reference);
          BuildingUnblocked?.Invoke(this, EventArgs.Empty);
        }
      }
    }

    public void Block(T reference)
    {
      this.Blocked?.Invoke(this, reference);
    }

    public void Unblock(T reference)
    {
      this.Unblocked?.Invoke(this, reference);
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
