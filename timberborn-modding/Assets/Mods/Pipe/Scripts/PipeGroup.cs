using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeGroup
  {
    public bool isEnabled { get; private set; } = true;

    private static int lastId = 0;

    public readonly int id = lastId++;

    private TickCount nodesTick = new TickCount();

    public readonly HashSet<PipeNode> Pipes = new HashSet<PipeNode>();

    public ImmutableArray<GateContext> WaterGates { get; private set; } = new ImmutableArray<GateContext>();

    public ImmutableArray<GateContext> InputPumps { get; private set; } = new ImmutableArray<GateContext>();

    public ImmutableArray<GateContext> OutputPumps { get; private set; } = new ImmutableArray<GateContext>();

    public bool NoDistribution = true;

    public int Deliveries;

    public int Requesters;

    public int Interaction;

    private PipeGroupQueue pipeGroupQueue;

    public int PipesCount { get; private set; } = 0;

    public bool HasMoreThanOnePipe { get; private set; } = false;

    public PipeGroup(
      PipeGroupQueue _pipeGroupQueue
    )
    {
      pipeGroupQueue = _pipeGroupQueue;
    }

    public void UpdatePipeCount()
    {
      PipesCount = Pipes.Count;
      HasMoreThanOnePipe = Pipes.Count > 0;
    }

    public bool Same(PipeGroup group)
    {
      return group != null && this.Equals(group);
    }

    public void PipeAdd(PipeNode node)
    {
      Pipes.Add(node);
      UpdatePipeCount();
      pipeGroupQueue.Group_RecalculateGates(this);
    }

    public void PipeRemove(PipeNode node)
    {
      Pipes.Remove(node);
      UpdatePipeCount();
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

    public void ResetNodes()
    {
      foreach (var node in Pipes)
      {
        node.DisablePowerConsumption();
      }
    }

    public void recalculateGates()
    {
      if (Pipes.Count == 0)
      {
        WaterGates.Clear();
        return;
      }
      ResetNodes();
      WaterGates = Pipes.ToList()
        .Where(node => node.hasGatesEnabled)
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
        .Select(gate => new GateContext(gate))
        .ToImmutableArray();
      NoDistribution = true;
    }

    public void DoMoveWater()
    {
      if (!isEnabled)
        return;
      WaterService.MoveWater(this);
    }

    public string GetInfo()
    {
      string info = $"Group[\n";
      info += $"  id={id} enabled={isEnabled} requesters={Requesters} deliveries={Deliveries} interaction={Interaction}\n";
      info += $"  pipes={Pipes.Count} gates={WaterGates.Length}\n";
      info += $"  gates:\n";
      foreach (var context in WaterGates.ToList())
      {
        info += context.gate.GetInfo(false);
        info += $"    pressureSum={context.pressureSum} quotaSum={context.quotaSum}\n";
        info += $"    waterOfertedSum={context.waterOfertedSum} contaminationSum={context.contaminationSum}\n";
        info += $"    WaterUsed={context.WaterUsed} pumpRequested={context.pumpRequested}\n";
        info += $"    Contamination={context.Contamination} WaterMove={context.WaterMove}\n";
        info += $"  ];\n";
      }
      info += $"];\n";
      return info;
    }
  }
}
