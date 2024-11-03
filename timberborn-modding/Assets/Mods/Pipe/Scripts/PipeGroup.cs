using System.Linq;
using System.Collections.Generic;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeGroup
  {
    public bool isEnabled { get; private set; } = true;

    private static int lastId = 0;

    public readonly int id = lastId++;

    private TickCount nodesTick = new TickCount();

    public readonly HashSet<PipeNode> Pipes = new HashSet<PipeNode>();

    public List<WaterGate> WaterGates { get; private set; } = new List<WaterGate>();

    public float WaterAverage;

    private int PipesCount => Pipes.Count;

    private PipeGroupQueue pipeGroupQueue;

    public PipeGroup(
      PipeGroupQueue _pipeGroupQueue
    )
    {
      pipeGroupQueue = _pipeGroupQueue;
    }

    public bool Same(PipeGroup group)
    {
      return group != null && this.Equals(group);
    }

    public void PipeAdd(PipeNode node)
    {
      Pipes.Add(node);
      pipeGroupQueue.Group_RecalculateGates(this);
    }

    public void PipeRemove(PipeNode node)
    {
      Pipes.Remove(node);
      pipeGroupQueue.Group_RecalculateGates(this);
    }

    public void SetDisabled()
    {
      isEnabled = false;
    }

    public void Destroy()
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
      SetDisabled();
      pipeGroupQueue.Group_Remove(this);
      pipeGroupQueue.Group_RecalculateGates(group);
    }

    public void recalculateGates()
    {
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
      WaterService.Do(this);
    }

    public string GetInfo()
    {
      string info = $"Group[group={id} enabled={isEnabled} WaterAverage={WaterAverage.ToString("0.00")} nodes={Pipes.Count} gates={WaterGates.Count}:\n";
      foreach (var gate in WaterGates.ToList())
      {
        info += gate.GetInfo();
      }
      info += "]\n";
      return info;
    }
  }
}
