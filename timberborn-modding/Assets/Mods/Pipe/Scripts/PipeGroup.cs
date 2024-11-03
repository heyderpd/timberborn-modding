using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Timberborn.Common;
using UnityEngine.Diagnostics;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class GateInteractionContext
  {
    public readonly GateContext context;

    public float RequesterWaterQuota;

    public GateInteractionContext(GateContext _gate)
    {
      context = _gate;
    }

    public void Reset()
    {
      RequesterWaterQuota = 0f;
    }
  }

  internal class GateContext
  {
    public readonly WaterGate gate;

    public ImmutableArray<GateContext>? higherGates;

    public ImmutableArray<GateInteractionContext>? lowestGates;

    public WaterGateFlow DeliveryType;

    public float DesiredWater;

    public float DeliveryWaterRequested;

    public float DeliveryWaterReturned;

    public float RequesterWaterDelivered;

    public float RequesterWaterUnused;

    public float WaterMove;

    public float Contamination;
    
    public GateContext(WaterGate _gate)
    {
      gate = _gate;
    }

    public void Reset()
    {
      DesiredWater = 0f;
      DeliveryWaterRequested = 0f;
      DeliveryWaterReturned = 0f;
      RequesterWaterDelivered = 0f;
      RequesterWaterUnused = 0f;
      WaterMove = 0f;
      Contamination = 0f;
    }

    public bool IsEqual(GateInteractionContext reference)
    {
      return this == reference.context;
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
      // discovery pumps
      var OutputPumps = WaterGates.Where(context => context.gate.IsWaterPump);
      var InputPumps = OutputPumps.Where(context => context.gate.IsInput).Select(gate => new GateInteractionContext(gate));
      OutputPumps = OutputPumps.Where(context => !context.gate.IsInput);
      // group by floor
      var Floors = new Dictionary<float ,List<GateContext>>();
      foreach (var context in WaterGates)
      {
        if (context.gate.IsWaterPump)
          continue;
        var floor = context.gate.LowerLimit;
        var exist = Floors.TryGetValue(floor, out List<GateContext> Gates);
        if (!exist)
          Gates = new List<GateContext>();
        else
          Floors.Remove(floor);
        Gates.Add(context);
        Floors.Add(floor, Gates);
      }
      // link higher to lower floor
      var OrderedFloors = Floors.OrderByDescending(item => item.Key);
      var Highers = new HashSet<GateInteractionContext>(InputPumps);
      foreach (var (floor, Gates) in OrderedFloors)
      {
        ModUtils.Log($"[PipeGroup.recalculateGates] 01 floor={floor} Highers={Highers.Count}");
        var FloorGates = Gates.Select(gate => new GateInteractionContext(gate)).ToImmutableArray().AddRange(Highers);
        ModUtils.Log($"[PipeGroup.recalculateGates] 02 floor={floor} FloorGates={FloorGates.Length}");
        foreach (var context in Gates)
        {
          context.lowestGates = FloorGates;
        }
        Highers.AddRange(FloorGates);
      }
      // link lower to higher floor
      var Lowers = new HashSet<GateContext>();
      foreach (var (floor, Gates) in OrderedFloors.Reverse())
      {
        ModUtils.Log($"[PipeGroup.recalculateGates] 01 floor={floor} Lowers={Lowers.Count}");
        var FloorGates = Gates.ToImmutableArray().AddRange(Lowers);
        ModUtils.Log($"[PipeGroup.recalculateGates] 02 floor={floor} FloorGates={FloorGates.Length}");
        foreach (var context in Gates)
        {
          context.higherGates = FloorGates;
        }
        Lowers.AddRange(FloorGates);
      }
      if (OutputPumps.Count() > 0 && Lowers.Count > 0)
      {
        var ImmutableLowers = Lowers.ToImmutableArray();
        foreach (var pump in OutputPumps)
        {
          pump.higherGates = ImmutableLowers;
        }
      }
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
        info += $"    type={context.DeliveryType} lowests={context.lowestGates?.Length}\n";
        info += $"    initialDesire={context.DesiredWater.ToString("0.00")}\n";
        info += $"    deliveryRequested={context.DeliveryWaterRequested.ToString("0.00")} deliveryReturned={context.DeliveryWaterReturned.ToString("0.00")}\n";
        info += $"    requesterDelivered={context.RequesterWaterUnused.ToString("0.00")} requesterUnused={context.RequesterWaterDelivered.ToString("0.00")}\n";
        info += $"    waterMove={context.WaterMove.ToString("0.00")} contamination={context.Contamination.ToString("0.00")}\n";
        info += $"  ];\n";
      }
      info += $"];\n";
      return info;
    }
  }
}
