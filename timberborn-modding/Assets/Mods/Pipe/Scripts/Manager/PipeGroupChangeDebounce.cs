﻿using System.Collections.Generic;

namespace Mods.OldGopher.Pipe
{
  internal class PipeGroupChangeDebounce<T>
  {
    private PipeGroupChangeTypes type;

    public HashSet<T> Items { get; private set; } = new HashSet<T>();

    public int Count => Items.Count;

    public bool IsEmpty => Items.Count == 0;

    public PipeGroupChangeDebounce(PipeGroupChangeTypes _type)
    {
      type = _type;
    }

    public void Clear()
    {
      Items.Clear();
    }

    public void Store(PipeGroupChangeTypes _type, T item)
    {
      if (type != _type)
        return;
      if (item == null)
        return;
      Items.Add(item);
    }
  }
}
