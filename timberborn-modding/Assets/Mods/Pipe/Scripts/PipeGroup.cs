using System;
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
      requester.pumpActivate += delivery.OnPumpActivateAdded;
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

    public int pumpRequested;

    public event EventHandler pumpActivate;

    public GateContext(WaterGate _gate)
    {
      gate = _gate;
    }

    public void Reset()
    {
      deliveryQuotas.Clear();
      requesterQuotas.Clear();
      WaterUsed = 0f;
      WaterMove = 0f;
      Contamination = 0f;
      turnedRequester = false;
      stopedRequester = false;
      pumpRequested = 0;
      pumpActivate = null;
      ModUtils.Log($"QUE ERRO");
      if (pumpActivate != null)
      {
        ModUtils.Log($"MERDA");
        foreach (EventHandler callback in pumpActivate.GetInvocationList())
        {
          ModUtils.Log($"ESS");
          pumpActivate -= callback;
        }
      }
      ModUtils.Log($"ESCROTO");
    }

    public void SendPumpActivateEvent(bool enabled)
    {
      ModUtils.Log($"[GateContext.SendPumpActivateEvent] call");
      if (enabled)
        this.pumpActivate?.Invoke(this, EventArgs.Empty);
    }

    public void AddPumpRequested(bool enabled)
    {
      ModUtils.Log($"[GateContext.AddPumpRequested] call");
      if (enabled)
        pumpRequested += 1;
    }

    public void OnPumpActivateAdded(object sender, EventArgs e)
    {
      ModUtils.Log($"[GateContext.OnPumpActivateAdded] call");
      AddPumpRequested(true);
    }

    private void SwitchPowerConsumption(bool enabled)
    {
      ModUtils.Log($"[GateContext.SwitchPowerConsumption] call");
      if (enabled)
        gate.powered?.EnablePowerConsumption();
      else
        gate.powered?.DisablePowerConsumption();
    }

    public void checkPumpRequested()
    {
      ModUtils.Log($"[GateContext.checkPumpRequested] call");
      SwitchPowerConsumption(pumpRequested > 0);
      pumpRequested = 0;
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

    public bool NoDistribution = true;

    public int Deliveries;

    public int Requesters;

    private PipeGroupQueue pipeGroupQueue;

    public int PipesCount { get; private set; } = 0;

    public bool HasMoreThanOnePipe { get; private set; } = false;

    public TickCount WaterTick = new TickCount(WaterService.waterTick);

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
      ModUtils.Log($"[PipeGroup.Destroy] group={id}");
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
      info += $"  id={id} enabled={isEnabled} requesters={Requesters} deliveries={Deliveries}\n";
      info += $"  pipes={Pipes.Count} gates={WaterGates.Length}\n";
      info += $"  gates:\n";
      foreach (var context in WaterGates.ToList())
      {
        info += context.gate.GetInfo(false);
        info += $"    turnedRequester={context.turnedRequester} stopedRequester={context.stopedRequester} \n";
        info += $"    waterMove={context.WaterMove} contamination={context.Contamination}\n";
        info += $"  ];\n";
      }
      info += $"];\n";
      return info;
    }
  }
}
