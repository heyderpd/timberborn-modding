using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class WaterService
  {

    public static readonly float maximumFlow = 1.00f;

    public static readonly float minimumFlow = 0.001f;

    public static readonly float waterFactor = 0.36f; // max cms = 0.7

    public static readonly float pumpPressure = 10.00f;

    public static readonly float pumpRate = 0.4642f; // 0.3714f; // pump cms = 0.3

    public static readonly int waterTick = 0; // 3 ficou ondulando muito, 2 teve um pouco de ondulacao e tudo usava 2f

    private static IEnumerable<GateContext> GatesIterator(bool IsDelivery, ImmutableArray<GateContext> Gates, GateContext reference, bool checkWater)
    {
      foreach (var context in Gates)
      {
        if (reference != null && reference == context)
          continue;
        if (checkWater && !context.gate.SuccessWhenCheckWater)
          continue;
        if (IsDelivery && (context.gate.IsOnlyRequester || context.turnedRequester || (checkWater && context.gate.WaterAvailable <= 0f)))
          continue;
        if (!IsDelivery && (context.gate.IsOnlyDelivery || context.stopedRequester))
          continue;
        yield return context;
      }
    }

    private static IEnumerable<GateContext> DeliveryIterator(ImmutableArray<GateContext> Gates, bool checkWater = true)
    {
      return GatesIterator(true, Gates, null, checkWater);
    }

    private static IEnumerable<GateContext> RequesterIterator(ImmutableArray<GateContext> Gates, GateContext reference = null, bool checkWater = true)
    {
      return GatesIterator(false, Gates, reference, checkWater);
    }

    private static float GetPumpLevel(WaterGate input, WaterGate output)
    {
      var pumpUpLevel = input.LowerLimit + pumpPressure;
      if (input.LowerLimit < output.LowerLimit)
      {
        if (pumpUpLevel > output.LowerLimit)
          return pumpUpLevel;
        else
          return 0f;
      }
      else
        return pumpUpLevel;
    }

    private static float GetCommomLevel(WaterGate input, WaterGate output)
    {
      if (output.LowerLimit > input.WaterLevel)
        return 0f;
      var diffLevel = input.WaterLevel - output.WaterLevel;
      if (diffLevel <= 0f)
        return 0f;
      return output.WaterLevel + (diffLevel / 2);
    }

    private static float GetNewWaterLevel(WaterGate input, WaterGate output)
    {
      if (input.IsWaterPump || output.IsWaterPump)
        return GetPumpLevel(input, output);
      else
        return GetCommomLevel(input, output);
    }

    private static float GetSubmergeLimit(WaterGate gate, float water)
    {
      var newWaterLevel = gate.WaterLevel + water;
      if (newWaterLevel < gate.HigthLimit)
        return newWaterLevel - gate.WaterLevel;
      else
        return 0f;
    }

    private static float GetPumpLimit(WaterGate gate, float water)
    {
      ModUtils.Log($"[WaterService.GetPumpLimit] 01 water={water} pumpRate={pumpRate} PowerEfficiency={gate.PowerEfficiency}");
      water = LimitWater(water, pumpRate);
      ModUtils.Log($"[WaterService.GetPumpLimit] 02 water={water}");
      if (water <= 0f)
        return 0f;
      ModUtils.Log($"[WaterService.GetPumpLimit] 03 result={water * gate.PowerEfficiency}");
      return water * gate.PowerEfficiency;
    }

    public static float LimitWater(float expectedWater, float maxValue = 0f)
    {
      maxValue = maxValue == 0f ? maximumFlow : maxValue;
      float water = Mathf.Abs(expectedWater);
      water = Mathf.Min(water, maxValue);
      water = water > minimumFlow ? water : 0f;
      return water;
    }

    private static void discoveryDistribution(PipeGroup group)
    {
      group.NoDistribution = false;
      group.Deliveries = 0;
      group.Requesters = 0;
      foreach (var context in group.WaterGates)
      {
        context.Reset();
        context.gate.powered?.DisablePowerConsumption();
      }
      foreach (var delivery in DeliveryIterator(group.WaterGates, checkWater: false))
      {
        group.Deliveries += 1;
        foreach (var requester in RequesterIterator(group.WaterGates, delivery, checkWater: false))
        {
          group.Requesters += 1;
          var quota = new GateContextInteraction(delivery, requester, 0f);
          requester.deliveryQuotas.Add(delivery, quota);
          delivery.requesterQuotas.Add(requester, quota);
        }
      }
    }

    private static bool distributeWater(PipeGroup group)
    {
      if (group.Deliveries == 0 || group.Requesters == 0)
      {
        ModUtils.Log($"[WaterService.distributeWater] 00 aborted by deliveries={group.Deliveries} requesters={group.Requesters}");
        return true;
      }
      if (group.WaterTick.Skip())
      {
        ModUtils.Log($"[WaterService.distributeWater] 01 skip real distribution");
        return true;
      }
      foreach (var context in group.WaterGates)
      {
        context.checkPumpRequested();
        context.gate.UpdateWaters();
      }
      // discovery pre quota
      var noneQuota = true;
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        foreach (var Quota in requester.deliveryQuotas.Values)
        {
          var preQuotaSum = GetNewWaterLevel(Quota.delivery.gate, requester.gate);
          if (preQuotaSum > 0f)
          {
            Quota.preQuota = preQuotaSum;
            noneQuota = false;
          }
        }
      }
      if (noneQuota)
      {
        ModUtils.Log($"[WaterService.distributeWater] 02 aborted by noneQuota={noneQuota}");
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
      ModUtils.Log($"[WaterService.distributeWater] 03 deliveryRemoved={deliveryRemoved.Count} requesterRemoved={requesterRemoved.Count}");
      // check has minimal condiction after changes
      if (group.Deliveries - deliveryRemoved.Count == 0 || group.Deliveries - requesterRemoved.Count == 0)
      {
        ModUtils.Log($"[WaterService.distributeWater] 04 aborted by deliveries={group.Deliveries - deliveryRemoved.Count} requesters={group.Deliveries - requesterRemoved.Count}");
        return true;
      }
      // delivery turned requester will be removed from delivery
      // delivery stay delivery will be removed from requester
      foreach (var context in group.WaterGates)
      {
        foreach (var Quota in context.deliveryQuotas.Values.Where(Quota => deliveryRemoved.Contains(Quota.delivery)))
        {
          Quota.preQuota = 0f;
        }
        foreach (var Quota in context.requesterQuotas.Values.Where(Quota => requesterRemoved.Contains(Quota.requester)))
        {
          Quota.preQuota = 0f;
        }
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
          var WaterAvailable = delivery.gate.WaterAvailable * quota;
          if (delivery.gate.IsWaterPump)
            WaterAvailable = GetPumpLimit(delivery.gate, WaterAvailable);
          Quota.waterOferted = WaterAvailable;
          Quota.contamination = delivery.gate.ContaminationPercentage * quota;
        }
      }
      // discovery requester final water and contamination
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        var waterSum = requester.deliveryQuotas.Values.Sum(Quota => Quota.waterOferted);
        ModUtils.Log($"[WaterService.LimitWater] TMP 01 requester={requester.gate.id} waterSum={waterSum}");
        var waterMove = LimitWater(waterSum);
        if (requester.gate.IsValve)
          waterMove = GetSubmergeLimit(requester.gate, waterMove);
        if (requester.gate.IsWaterPump)
        {
          requester.AddPumpRequested(waterMove > 0f);
          waterMove = GetPumpLimit(requester.gate, waterMove);
        }
        requester.SendPumpActivateEvent(waterMove > 0f);
        ModUtils.Log($"[WaterService.LimitWater] TMP 02 requester={requester.gate.id} waterMove={waterMove}");
        requester.WaterUsed = waterMove / waterSum;
        requester.WaterMove = waterMove;
        requester.Contamination = requester.deliveryQuotas.Values.Sum(Quota => Quota.contamination);
      }
      // discovery delivery final water
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        var waterUsedSum = delivery.requesterQuotas.Keys.Sum(Quota => Quota.WaterUsed);
        var waterUsed = waterUsedSum / delivery.requesterQuotas.Count;
        ModUtils.Log($"[WaterService.LimitWater] delivery final water 01 waterUsedSum={waterUsedSum} Count={delivery.requesterQuotas.Count} waterUsed={waterUsed}");
        delivery.WaterMove = -(delivery.gate.WaterAvailable * waterUsed);
        delivery.Contamination = delivery.gate.ContaminationPercentage;
      }
      ModUtils.Log($"[WaterService.distributeWater] 05 success");
      return false;
    }

    private static bool checkFlowChanged(PipeGroup group)
    {
      var canFlow = true;
      foreach (var context in group.WaterGates)
      {
        canFlow = context.gate.FlowNotChanged(context.WaterMove) && canFlow;
      }
      ModUtils.Log($"[WaterService.distributeWater] canFlow={canFlow}");
      return !canFlow;
    }

    private static void StopWater(PipeGroup group)
    {
      foreach (var context in group.WaterGates)
      {
        context.gate.powered?.DisablePowerConsumption();
        context.gate.RemoveWaterParticles();
      }
    }

    private static void moveWater(PipeGroup group)
    {
      var factor = Time.fixedDeltaTime * waterFactor;
      foreach (var context in group.WaterGates)
      {
        context.gate.MoveWater(context.WaterMove * context.gate.PowerEfficiency * factor, context.Contamination);
      }
    }

    private static bool _moveWater(PipeGroup group)
    {
      if (group.WaterGates.Length <= 1)
        return false;
      if (group.NoDistribution)
        discoveryDistribution(group);
      if (distributeWater(group))
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
