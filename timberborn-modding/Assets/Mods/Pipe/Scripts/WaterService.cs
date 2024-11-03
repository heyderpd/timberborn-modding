using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class WaterService
  {
    public static readonly float pumpPressure = 1.00f;

    public static readonly float maximumFlow = 1.00f;

    public static readonly float minimumFlow = 0.001f;

    public static readonly float waterPercentPerSecond = ModUtils.waterPower;

    private static IEnumerable<GateContext> GatesIterator(bool IsDelivery, ImmutableArray<GateContext> Gates, GateContext reference)
    {
      foreach (var context in Gates)
      {
        if (reference != null && reference == context)
          continue;
        if (!context.gate.SuccessWhenCheckWater)
          continue;
        if (IsDelivery && (context.gate.IsOnlyRequester || context.turnedRequester || context.gate.WaterAvailable <= 0f))
          continue;
        if (!IsDelivery && (context.gate.IsOnlyDelivery || context.stopedRequester))
          continue;
        yield return context;
      }
    }

    private static IEnumerable<GateContext> DeliveryIterator(ImmutableArray<GateContext> Gates, GateContext reference = null)
    {
      return GatesIterator(true, Gates, reference);
    }

    private static IEnumerable<GateContext> RequesterIterator(ImmutableArray<GateContext> Gates, GateContext reference = null)
    {
      return GatesIterator(false, Gates, reference);
    }

    private static float GetNewWaterLevel(WaterGate input, WaterGate output)
    {
      ModUtils.Log($"[WaterService.GetNewWaterLevel] TMP 01 input={input.id} requesters={output.id}");
      ModUtils.Log($"[WaterService.GetNewWaterLevel] TMP 02 output.LowerLimit={output.LowerLimit} input.WaterLevel={input.WaterLevel} check={output.LowerLimit > input.WaterLevel}");
      if (output.LowerLimit > input.WaterLevel)
        return 0f;
      var diffLevel = input.WaterLevel - output.WaterLevel;
      ModUtils.Log($"[WaterService.GetNewWaterLevel] TMP 03 input.WaterLevel={input.WaterLevel} output.WaterLevel={output.WaterLevel} diffLevel={diffLevel} check={diffLevel <= 0f}");
      if (diffLevel <= 0f)
        return 0f;
      ModUtils.Log($"[WaterService.GetNewWaterLevel] TMP 04 result={output.WaterLevel + (diffLevel / 2)}");
      return output.WaterLevel + (diffLevel / 2);
    }

    private static float GetSubmergeLimit(WaterGate gate, float water)
    {
      var newWaterLevel = gate.WaterLevel + water;
      if (newWaterLevel < gate.HigthLimit)
        return newWaterLevel - gate.WaterLevel;
      else
        return 0f;
    }

    public static float LimitWater(float expectedWater)
    {
      float water = Mathf.Abs(expectedWater);
      ModUtils.Log($"[WaterService.LimitWater] TMP 02 water={water}");
      water = Mathf.Min(water, maximumFlow);
      ModUtils.Log($"[WaterService.LimitWater] TMP 03 water={water}");
      water = water > minimumFlow ? water : 0f;
      ModUtils.Log($"[WaterService.LimitWater] TMP 04 water={water}");
      return water;
    }

    private static bool discoveryDistribution(PipeGroup group)
    {
      foreach (var context in group.WaterGates)
      {
        context.Reset();
      }
      // check has minimal condiction
      var deliveries = DeliveryIterator(group.WaterGates).Count();
      var requesters = RequesterIterator(group.WaterGates).Count();
      if (deliveries == 0 || requesters == 0)
      {
        ModUtils.Log($"[WaterService.discoveryDistribution] 01 aborted by deliveries={deliveries} requesters={requesters}");
        return true;
      }
      // discovery pre quota
      var noneQuota = true;
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        foreach (var requester in RequesterIterator(group.WaterGates, delivery))
        {
          var waterPreQuota = 0f;
          if (delivery.gate.IsWaterPump)
          {
            if (requester.gate.IsWaterPump)
              waterPreQuota = pumpPressure * 2;
            else
              waterPreQuota = pumpPressure;
          } else
            waterPreQuota = GetNewWaterLevel(delivery.gate, requester.gate);
          if (waterPreQuota <= 0f)
            continue;
          noneQuota = false;
          var quota = new GateContextInteraction(delivery, requester, waterPreQuota);
          requester.deliveryQuotas.Add(delivery, quota);
          delivery.requesterQuotas.Add(requester, quota);
        }
      }
      if (noneQuota)
      {
        ModUtils.Log($"[WaterService.discoveryDistribution] 02 aborted by noneQuota={noneQuota}");
        return true;
      }
      // balance pre quota
      var deliveryRemoved = new HashSet<GateContext>();
      var requesterRemoved = new HashSet<GateContext>();
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        var preQuotaSum = delivery.deliveryQuotas.Values.Sum(Quota => Quota.preQuota);
        if (preQuotaSum <= 0f)
          continue;
        if (preQuotaSum >= delivery.gate.WaterDetected)
        {
          delivery.turnedRequester = true;
          deliveryRemoved.Add(delivery);
        } else
          requesterRemoved.Add(delivery);
      }
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        var preQuotaSum = requester.deliveryQuotas.Values.Sum(Quota => Quota.preQuota);
        if (preQuotaSum > 0f)
          continue;
        requester.stopedRequester = true;
        requesterRemoved.Add(requester);
      }
      ModUtils.Log($"[WaterService.discoveryDistribution] 03 deliveryRemoved={deliveryRemoved.Count} requesterRemoved={requesterRemoved.Count}");
      // check has minimal condiction after changes
      deliveries = DeliveryIterator(group.WaterGates).Count();
      requesters = RequesterIterator(group.WaterGates).Count();
      if (deliveries == 0 || requesters == 0)
      {
        ModUtils.Log($"[WaterService.discoveryDistribution] 04 aborted by deliveries={deliveries} requesters={requesters}");
        return true;
      }
      foreach (var delivery in deliveryRemoved)
      {
        ModUtils.Log($"[WaterService.discoveryDistribution] 04a REMOVE delivery={delivery.gate.id}");
      }
      foreach (var requester in requesterRemoved)
      {
        ModUtils.Log($"[WaterService.discoveryDistribution] 04b REMOVE requester={requester.gate.id}");
      }
      // delivery turned requester will be removed from delivery
      // delivery stay delivery will be removed from requester
      foreach (var context in group.WaterGates)
      {
        context.deliveryQuotas.Values
          .Where(Quota => deliveryRemoved.Contains(Quota.delivery))
          .Select(Quota => Quota.delivery)
          .ToList()
          .ForEach(remove => context.deliveryQuotas.Remove(remove));
        context.requesterQuotas.Values
          .Where(Quota => requesterRemoved.Contains(Quota.delivery))
          .Select(Quota => Quota.delivery)
          .ToList()
          .ForEach(remove => context.requesterQuotas.Remove(remove));
      }
      // discovery real quota
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        var preQuotaSum = requester.deliveryQuotas.Values.Sum(Quota => Quota.preQuota);
        foreach (var Quota in requester.deliveryQuotas.Values)
        {
          Quota.quota = Quota.preQuota / preQuotaSum;
        }
      }
      // discovery delivery oferted water and contamination for requesters
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        var quotaSum = delivery.requesterQuotas.Values.Sum(Quota => Quota.quota);
        foreach (var Quota in delivery.requesterQuotas.Values)
        {
          var quota = (Quota.quota / quotaSum);
          Quota.waterOferted = delivery.gate.WaterAvailable * quota;
          Quota.contamination = delivery.gate.ContaminationPercentage * quota;
        }
      }
      // discovery requester final water and contamination
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        var waterSum = requester.deliveryQuotas.Values.Sum(Quota => Quota.waterOferted);
        ModUtils.Log($"[WaterService.LimitWater] TMP 01 requester={requester.gate.id}");
        var waterMove = LimitWater(waterSum);
        if (requester.gate.IsValve)
          waterMove = GetSubmergeLimit(requester.gate, waterMove);
        requester.WaterUsed = waterMove / waterSum;
        requester.WaterMove = waterMove;
        var contamination = requester.deliveryQuotas.Values.Sum(Quota => Quota.contamination);
        requester.Contamination = contamination;
      }
      // discovery delivery final water
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        var waterUsedSum = delivery.requesterQuotas.Keys.Sum(Quota => Quota.WaterUsed);
        var waterUsed = waterUsedSum / delivery.requesterQuotas.Count;
        delivery.WaterMove = -(delivery.gate.WaterAvailable * waterUsed);
      }
      ModUtils.Log($"[WaterService.discoveryDistribution] 05 success");
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
      if (discoveryDistribution(group))
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
