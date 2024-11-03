using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class GateContextInteraction
  {
    public GateContext delivery;

    public GateContext requester;

    public float preQuota;

    public float quota;

    public float waterOferted;

    public float contamination;

    public GateContextInteraction(GateContext _delivery, GateContext _requester, float _preQuota)
    {
      delivery = _delivery;
      requester = _requester;
      preQuota = _preQuota;
      quota = 0f;
      waterOferted = 0f;
      contamination = 0f;
    }
  }

  internal class GateContext
  {
    public readonly WaterGate gate;

    public Dictionary<GateContext, GateContextInteraction> deliveryQuotas = new Dictionary<GateContext, GateContextInteraction>();

    public Dictionary<GateContext, GateContextInteraction> requesterQuotas = new Dictionary<GateContext, GateContextInteraction>();

    public float WaterUsed;

    public float WaterMove;

    public float Contamination;

    public bool turnedRequester;

    public bool stopedRequester;

    public GateContext(WaterGate _gate)
    {
      gate = _gate;
    }

    public void Reset()
    {
      gate.UpdateWaters();
      deliveryQuotas.Clear();
      requesterQuotas.Clear();
      WaterUsed = 0f;
      WaterMove = 0f;
      Contamination = gate.ContaminationPercentage;
      turnedRequester = false;
      stopedRequester = false;
    }
  }

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

    public float WaterAverage;

    private PipeGroupQueue pipeGroupQueue;

    public int PipesCount { get; private set; } = 0;

    public bool HasMoreThanOnePipe { get; private set; } = false;

    public TickCount tick = new TickCount(ModUtils.waterTick);

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

    public void recalculateGates()
    {
      if (Pipes.Count == 0)
      {
        WaterGates.Clear();
        return;
      }
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
      if (!isEnabled && tick.Skip())
        return;
      WaterService.MoveWater(this);
    }

    public string GetInfo()
    {
      string info = $"Group[\n";
      info += $"  id={id} enabled={isEnabled} average={WaterAverage.ToString("0.00")}\n";
      info += $"  pipes={Pipes.Count} gates={WaterGates.Length}\n";
      info += $"  gates:\n";
      foreach (var context in WaterGates.ToList())
      {
        info += context.gate.GetInfo(false);
        info += $"  turnedRequester={context.turnedRequester} stopedRequester={context.stopedRequester} \n";
        info += $"  waterMove={context.WaterMove} contamination={context.Contamination}\n";
        info += $"  ];\n";
      }
      info += $"];\n";
      return info;
    }
  }
}
