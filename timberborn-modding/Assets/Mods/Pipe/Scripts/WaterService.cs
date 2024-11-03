using System.Linq;
using System.Collections.Generic;
using System;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class WaterService
  {
    public static readonly float maximumFlow = 1.00f;

    public static readonly float minimumFlow = 0.001f;

    public static readonly float waterPercentPerSecond = ModUtils.waterPower;

    private static float getWaterLevel(PipeGroup group)
    {
      float count = 0;
      float waterSum = 0f;
      float levelSum = group.WaterGates
        .Aggregate(
          0f,
          (float levelSum, GateContext context) =>
          {
            context.Reset();
            var success = context.gate.UpdateWaters();
            if (!success || context.gate.CantAffectWaterLevel)
              return levelSum;
            count += 1;
            waterSum += context.gate.Water;
            return levelSum + context.gate.WaterLevel;
          }
        );
      ModUtils.Log($"[WaterService.getWaterLevel] waterSum={waterSum} levelSum={levelSum} Gates.Count={group.WaterGates.Length} count={count}");
      var average = levelSum / count;
      if (float.IsNaN(average) || minimumFlow >= waterSum)
        return float.NaN;
      group.WaterAverage = average;
      ModUtils.Log($"[WaterService.getWaterLevel] average={average}");
      return average;
    }

    private static bool discoveryDistribution(PipeGroup group, float average)
    {
      var contamination = 0f;
      var Deliverers = new List<GateContext>();
      var Requesters = new List<GateContext>();
      foreach (var context in group.WaterGates) // oN
      {
        context.Reset();
        context.Contamination = context.gate.ContaminationPercentage;
        if (context.gate.CanDelivereryWater(average))
        {
          Deliverers.Add(context);
          context.DesiredWater = context.gate.GetDeliveryWater(average);
          context.DeliveryType = WaterGateFlow.IN;
        }
        else if (context.gate.CanRequestWater(average))
        {
          Requesters.Add(context);
          context.DesiredWater = context.gate.GetRequesterWater(average);
          context.DeliveryType = WaterGateFlow.OUT;
        }
        else
        {
          context.DesiredWater = 0f;
          context.DeliveryType = WaterGateFlow.STOP;
        }
      }
      ModUtils.Log($"[WaterService.discoveryDistribution] 01 deliverers={Deliverers.Count} requesters={Requesters.Count}");
      if (Deliverers.Count == 0 || Requesters.Count == 0)
      {
        ModUtils.Log($"[WaterService.discoveryDistribution] 02 aborted by deliverers or requesters");
        return true;
      }
      // reset requesters and sum desires in deliveries
      foreach (var delivery in Deliverers) // oN^2
      {
        foreach (var requester in delivery?.lowestGates)
        {
          if (delivery.IsEqual(requester))
            continue;
          requester.Reset();
          delivery.DeliveryWaterRequested += requester.context.DesiredWater;
        }
      }
      // discovery requester quota per delivery
      foreach (var delivery in Deliverers) // oN^2
      {
        foreach (var requester in delivery?.lowestGates)
        {
          if (delivery.IsEqual(requester))
            continue;
          requester.RequesterWaterQuota = delivery.DesiredWater * (requester.context.DesiredWater / delivery.DeliveryWaterRequested);
          requester.context.RequesterWaterDelivered += requester.RequesterWaterQuota;
        }
      }
      // set WaterMove on requesters
      foreach (var requester in Requesters) // oN
      {
        requester.WaterMove = WaterGate.LimitWater(requester.RequesterWaterDelivered);
        requester.RequesterWaterUnused = requester.RequesterWaterDelivered - requester.WaterMove;
        ModUtils.Log($"[WaterService.discoveryDistribution] 03 RequesterWaterDelivered={requester.RequesterWaterDelivered} WaterMove={requester.WaterMove} RequesterWaterUnused={requester.RequesterWaterUnused}");
      }
      // set WaterMove on deliverers
      foreach (var delivery in Deliverers) // oN^2
      {
        foreach (var requester in delivery?.lowestGates)
        {
          if (delivery.IsEqual(requester))
            continue;
          delivery.DeliveryWaterReturned += requester.context.RequesterWaterUnused * (requester.RequesterWaterQuota / delivery.DeliveryWaterRequested);
          ModUtils.Log($"[WaterService.discoveryDistribution] 04 RequesterWaterUnused={requester.context.RequesterWaterUnused} RequesterWaterQuota={requester.RequesterWaterQuota} DeliveryWaterRequested={delivery.DeliveryWaterRequested} addQuote={requester.context.RequesterWaterUnused * (requester.RequesterWaterQuota / delivery.DeliveryWaterRequested)}");
        }
        var DeliveryWaterMove = delivery.DesiredWater * (1 - (delivery.DeliveryWaterReturned / delivery.DeliveryWaterRequested));
        ModUtils.Log($"[WaterService.discoveryDistribution] 05 DesiredWater={delivery.DesiredWater} DeliveryWaterReturned={delivery.DeliveryWaterReturned} DeliveryWaterRequested={delivery.DeliveryWaterRequested} DeliveryWaterMove={DeliveryWaterMove} WaterMove={delivery.WaterMove}");
        delivery.WaterMove -= DeliveryWaterMove;
        if (delivery.WaterMove < 0)
          contamination += delivery.gate.ContaminationPercentage;
        ModUtils.Log($"[WaterService.discoveryDistribution] 06 WaterMove={delivery.WaterMove}");
      }
      // set contamination on requesters
      ModUtils.Log($"[WaterService.discoveryDistribution] 07 contamination={contamination} Count={Deliverers.Count}");
      contamination = contamination / Deliverers.Count;
      ModUtils.Log($"[WaterService.discoveryDistribution] 08 contamination={contamination}");
      foreach (var requester in Requesters) // oN
      {
        requester.Contamination = contamination;
      }
      return false;
    }

    private static bool checkFlowChanged(PipeGroup group)
    {
      var canFlow = true;
      foreach (var context in group.WaterGates)
      {
        canFlow = context.gate.FlowNotChanged(context.WaterMove) && canFlow;
      }
      ModUtils.Log($"[WaterService.CheckFlowNotChanged] canFlow={canFlow}");
      return !canFlow;
    }

    private static void StopWater(PipeGroup group)
    {
      foreach (var context in group.WaterGates)
      {
        context.gate.RemoveWaterParticles();
      }
    }

    private static void moveWater(PipeGroup group)
    {
      foreach (var context in group.WaterGates)
      {
        ModUtils.Log($"[WaterService.moveWater] WaterMove={context.WaterMove} Contamination={context.Contamination}");
        context.gate.MoveWater(context.WaterMove * waterPercentPerSecond, context.Contamination);
      }
    }

    private static bool _moveWater(PipeGroup group)
    {
      if (group.WaterGates.Length <= 1)
        return false;
      var average = getWaterLevel(group);
      if (float.IsNaN(average))
        return false;
      if (discoveryDistribution(group, average))
        return false;
      if (checkFlowChanged(group))
        return false;
      moveWater(group);
      return true;
    }

    public static bool MoveWater(PipeGroup group)
    {
      try
      {
        var success = _moveWater(group);
        if (!success)
          StopWater(group);
        return success;
      }
      catch (Exception err)
      {
        ModUtils.Log($"#ERROR [WaterService.MoveWater] err={err}");
        return false;
      }
    }
  }
}
