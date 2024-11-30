using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mods.OldGopher.Pipe
{
  internal class PipeGroup
  {
    public bool isEnabled { get; private set; } = true;

    private static int lastId = 0;

    public readonly int id = lastId++;

    private TickCount nodesTick = new TickCount();

    public readonly HashSet<PipeNode> Pipes;

    public ImmutableArray<GateContext> WaterGates { get; private set; }

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
      Pipes = new HashSet<PipeNode>();
      WaterGates = new ImmutableArray<GateContext>();
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

    public bool CantWorkAlone()
    {
      if (Pipes.Count >= 2)
        return false;
      var pipe = Pipes.FirstOrDefault();
      var WorkAlone = pipe?.canWorkAlone ?? false;
      return !WorkAlone;
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
      WaterGates.Clear();
      ResetNodes();
      if (Pipes.Count == 0 || CantWorkAlone())
        return;
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
      if (!isEnabled || CantWorkAlone())
        return;
      PipeWaterService.MoveWater(this);
    }

    public string GetInfo()
    {
      try {
        string info = $"Group[\n";
        info += $"  id={id} enabled={isEnabled} requesters={Requesters} deliveries={Deliveries} interaction={Interaction}\n";
        info += $"  pipes={Pipes?.Count} gates={(WaterGates != null ? WaterGates.Length : -1)}\n";
        info += $"  gates:\n";
        if (WaterGates == null)
          return info + $"];\n";
        foreach (var context in WaterGates.ToList())
        {
          info += context.gate.GetInfo(false);
          info += $"    forceDelivery={context.forceDelivery} forceRequester={context.forceRequester}\n";
          info += $"    pressureSum={context.pressureSum} quotaSum={context.quotaSum}\n";
          info += $"    waterOfertedSum={context.waterOfertedSum} contaminationSum={context.contaminationSum}\n";
          info += $"    WaterUsed={context.WaterUsed} pumpRequested={context.pumpRequested}\n";
          info += $"    Contamination={context.Contamination} WaterMove={context.WaterMove}\n";
          info += $"  ];\n";
        }
        info += $"];\n";
        return info;
      } catch (Exception err) {
        ModUtils.Log($"#ERROR err={err}");
        return "";
      }
    }
  }
}
