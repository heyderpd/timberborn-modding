using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mods.Pipe.Scripts
{
  internal class PipeGroup
  {
    public bool isEnabled { get; private set; } = true;

    private static int lastId = 0;

    public readonly int id = lastId++;

    private TickCount nodesTick = new TickCount();

    public readonly HashSet<PipeNode> Pipes = new HashSet<PipeNode>();

    private List<WaterGate> WaterGates = new List<WaterGate>();

    private int PipesCount => Pipes.Count;

    public bool Same(PipeGroup group)
    {
      return group != null && this.Equals(group);
    }

    public void PipeAdd(PipeNode node)
    {
      Pipes.Add(node);
    }

    public void PipeRemove(PipeNode node)
    {
      Pipes.Remove(node);
    }

    public void SetDisabled()
    {
      isEnabled = false;
    }

    public void Clear()
    {
      SetDisabled();
      Pipes.Clear();
      WaterGates.Clear();
    }

    public void UnionTo(PipeGroup group)
    {
      if (Same(group))
        return;
      foreach (var pipe in Pipes)
      {
        pipe.SetGroup(group);
      }
      Clear();
    }

    public void recalculeGates()
    {
      Debug.Log($"GROUP.discovery group={id} start");
      if (Pipes.Count == 0)
      {
        WaterGates.Clear();
        return;
      }
      WaterGates = Pipes.ToList()
        .Where((PipeNode node) => node.hasGatesEnabled)
        .Aggregate(
          new List<WaterGate>(),
          (List<WaterGate> list, PipeNode node) =>
          {
            list.AddRange(
              node.waterGates
                .Where((WaterGate input) => input.isEnabled)
            );
            return list;
          }
        )
        .ToList();
    }

    private bool Skip()
    {
      if (Pipes.Count <= 1)
        return false;
      int count = Pipes.Where((PipeNode node) => node.isEnabled).Count();
      if (count <= 1)
        return false;
      nodesTick.SetMaxTicks(count);
      return nodesTick.Skip();
    }

    public void DoMoveWater()
    {
      if (!isEnabled)
        return;
      MoveWater.Do(WaterGates);
    }

    public string GetInfo()
    {
      string info = $"Group[id={id}, enabled={isEnabled} nodes={Pipes.Count}:\n";
      info += $"gates={WaterGates.Count}:\n";
      foreach (var gate in WaterGates.ToList())
      {
        info += gate.GetInfo();
      }
      info += "]\n";
      return info;
    }
  }
}
