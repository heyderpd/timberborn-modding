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

    private TickCount tick = new TickCount();

    private TickCount waterTick = new TickCount(10);

    private readonly PipeGroupQueue changes;

    private readonly HashSet<PipeNode> pipeNodes = new HashSet<PipeNode>();

    private List<WaterGate> waterGates = new List<WaterGate>();

    public PipeGroup()
    {
      changes = new PipeGroupQueue(this);
    }

    public bool Same(PipeGroup group)
    {
      return group != null && this.Equals(group);
    }

    public void SetChanged()
    {
      changes.AddChanged();
    }

    public void Add(PipeNode node)
    {
      changes.AddNode(node);
    }

    public void Remove(PipeNode node)
    {
      changes.RemoveNode(node);
    }

    public void CheckPipe(PipeNode node)
    {
      changes.CheckPipe(node);
    }

    public void CheckGate(WaterGate gate)
    {
      changes.CheckGate(gate);
    }

    public void Clear()
    {
      isEnabled = false;
      pipeNodes.Clear();
      waterGates.Clear();
    }

    public void UnionTo(PipeGroup group)
    {
      if (Same(group))
      {
        return;
      }
      changes.MigrateAndClear(group);
    }

    private void discoveryInputs()
    {
      Debug.Log($"GROUP.discovery group={id} start");
      if (pipeNodes.Count == 0)
      {
        waterGates.Clear();
        return;
      }
      waterGates = pipeNodes.ToList()
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

    private void tryMoveWater()
    {
      if (waterGates.Count <= 1)// || waterTick.Skip())
      {
        return;
      }
      float minWater = 0.02f;
      float average = 0f;
      foreach (var gate in waterGates)
      {
        if (!gate.isEnabled)
          continue;
        gate.UpdateAvailableWaters();
        average += gate.Water;
      }
      average = average / waterGates.Count;
      foreach (var gate in waterGates)
      {
        var water = average - gate.Water;
        if (Mathf.Abs(water) < minWater)
          continue;
        Debug.Log($"GROUP.movewater count={waterGates.Count} average={average} gate.id={gate.id} gate.Water={gate.Water} water={water}");
        gate.MoveWater(water, 0f);
      }
    }

    private bool SkipTick()
    {
      if (pipeNodes.Count <= 1)
      {
        return false;
      }
      var count = pipeNodes.Where((PipeNode node) => node.isEnabled).Count();
      tick.SetMaxTicks(count);
      return tick.Skip();
    }

    public void Tick()
    {
      if (!isEnabled || SkipTick())
      {
        return;
      }
      var changed = changes.ConsumeChanges(pipeNodes);
      if (changed)
        discoveryInputs();
      tryMoveWater();
    }

    public string GetInfo()
    {
      string info = $"Group[id={id}, enabled={isEnabled} nodes={pipeNodes.Count}:\n";
      /*foreach (var node in pipeNodes.ToList())
      {
        info += node.GetInfo();
      }*/
      info += $"gates={waterGates.Count}:\n";
      foreach (var gate in waterGates.ToList())
      {
        info += gate.GetInfo();
      }
      info += "]\n";
      return info;
    }
  }
}
