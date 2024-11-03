using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class WaterService
  {

    public static readonly float maximumFlow = 1.00f;

    public static readonly float minimumFlow = 0.001f;

    public static readonly float minimumPressure = 0.05f;

    public static readonly float waterFactor = 0.36f; // common max cms = 0.7

    public static readonly float pumpHeight = 8.00f;

    public static readonly float pumpRate = 0.4642f; // pump max cms = 0.3

    private static IEnumerable<GateContext> GatesIterator(bool IsDelivery, ImmutableArray<GateContext> Gates, GateContext reference, bool checkWater)
    {
      foreach (var context in Gates)
      {
        if (reference != null && reference == context)
          continue;
        if (checkWater && !context.gate.SuccessWhenCheckWater)
          continue;
        if (IsDelivery && (context.gate.IsOnlyRequester || context.forceRequester || (checkWater && context.gate.WaterAvailable <= 0f)))
          continue;
        if (!IsDelivery && (context.gate.IsOnlyDelivery || context.forceDelivery))
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

    public static float CalcPressure(WaterGate gate)
    {
      return CalcPressure(gate.WaterAvailable, gate.WaterLevel);
    }

    public static float CalcPressure(float WaterAvailable, float WaterLevel)
    {
      if (WaterAvailable <= 0f)
        return 0f;
      var pressure = WaterLevel * WaterLevel;
      return pressure;
    }

    private static float GetCommomPressure(WaterGate input, WaterGate output)
    {
      var pressureDiff = Mathf.Abs((input.WaterPressure - output.WaterPressure) / input.WaterPressure);
      if (minimumPressure > pressureDiff)
        return 0f;
      return input.WaterPressure - output.WaterPressure;
    }

    private static float GetPumpPressure(WaterGate input, WaterGate output)
    {
      var invalidPressure = (input.IsOnlyDelivery && output.IsOnlyDelivery) || (input.IsOnlyRequester && output.IsOnlyRequester);
      if (invalidPressure)
        return 0f;
      var pumpLevel = input.LowerLimit + pumpHeight;
      var pumpPressure = CalcPressure(input.WaterAvailable, pumpLevel) + 1;
      var pressureDiff = Mathf.Max(pumpPressure - output.WaterPressure, 0f);
      var pressureDirection = input.IsWaterPump
        ? input.IsOnlyDelivery ? +1 : -1
        : output.IsOnlyDelivery ? -1 : +1;
      if (input.LowerLimit >= output.LowerLimit)
        return pressureDirection * pressureDiff;
      else if (pumpLevel > output.LowerLimit)
        return pressureDirection * pressureDiff;
      else
        return 0f;
    }

    private static float GetPressure(WaterGate input, WaterGate output)
    {
      return (input.IsWaterPump || output.IsWaterPump)
        ? GetPumpPressure(input, output)
        : GetCommomPressure(input, output);
    }

    private static float GetSubmergeLimit(WaterGate gate, float waterAdd)
    {
      var waterLevel = gate.WaterLevel + waterAdd;
      var waterExceded = Mathf.Max(waterLevel - gate.HigthLimit, 0f);
      var waterLevelFixed = waterLevel - waterExceded;
      var water = waterLevelFixed - gate.WaterLevel;
      return water;
    }

    private static float GetPumpLimit(WaterGate gate, bool hasDelivery)
    {
      if (hasDelivery && gate.IsOnlyRequester)
        return 0f;
      if (!hasDelivery && gate.IsOnlyDelivery)
        return 0f;
      return LimitWater(gate.WaterAvailable, pumpRate);
    }

    public static float GetWaterTopLimit(WaterGate gate, float water, bool hasDelivery)
    {
      if (water <= 0f)
        return 0f;
      if (gate.IsValve)
        return GetSubmergeLimit(gate, water);
      else if (gate.IsWaterPump)
        return GetPumpLimit(gate, hasDelivery);
      else
        return LimitWater(water);
    }

    public static float GetWaterDelivered(WaterGate input, WaterGate output)
    {
      var waterAvg = Mathf.Abs(input.WaterLevel - output.WaterLevel) / 2;
      var water = GetWaterTopLimit(input, waterAvg, hasDelivery: true);
      return water;
    }

    public static float LimitWater(float water, float maxValue = 0f)
    {
      var waterAbs = Mathf.Abs(water);
      maxValue = maxValue == 0f ? maximumFlow : maxValue;
      waterAbs = Mathf.Min(waterAbs, maxValue);
      waterAbs = waterAbs > minimumFlow ? waterAbs : 0f;
      return waterAbs;
    }

    private static void discoveryDistribution(PipeGroup group)
    {
      group.NoDistribution = false;
      group.Interaction = 0;
      HashSet<WaterGate> _deliveries = new HashSet<WaterGate>();
      HashSet<WaterGate> _requesters = new HashSet<WaterGate>();
      foreach (var context in group.WaterGates)
      {
        context.Clear();
        context.gate.powered?.DisablePowerConsumption();
      }
      foreach (var delivery in DeliveryIterator(group.WaterGates, checkWater: false))
      {
        _deliveries.Add(delivery.gate);
        foreach (var requester in RequesterIterator(group.WaterGates, delivery, checkWater: false))
        {
          _requesters.Add(requester.gate);
          if (_deliveries.Contains(requester.gate) && _requesters.Contains(delivery.gate))
            continue;
          group.Interaction += 1;
          var quota = new GateContextInteraction(delivery, requester);
          requester.Interactions.Add(delivery, quota);
          delivery.Interactions.Add(requester, quota);
        }
      }
      group.Deliveries = _deliveries.Count;
      group.Requesters = _requesters.Count;
      _deliveries.Clear();
      _requesters.Clear();
    }

    private static bool distributeWater(PipeGroup group)
    {
      ModUtils.Log($"\n[WaterService.distributeWater] start group={group.id} WaterGates={group.WaterGates.Count()}");
      if (group.Deliveries == 0 || group.Requesters == 0)
      {
        ModUtils.Log($"[WaterService.distributeWater] aborted by deliveries={group.Deliveries} requesters={group.Requesters}");
        return true;
      }
      ModUtils.Log($"[WaterService.distributeWater.Reset] start");
      foreach (var context in group.WaterGates)
      {
        context.checkPumpRequested();
        context.Reset();
        context.gate.UpdateWaters();
      }
      ModUtils.Log($"[WaterService.distributeWater.Reset] end");
      // discovery pressures
      var nonePressure = true;
      foreach (var delivery in DeliveryIterator(group.WaterGates))
        foreach (var interaction in delivery.Interactions.Values)
        {
          ModUtils.Log($"[WaterService.pressures] loop gate={delivery.gate.id} iter");
          if (interaction.pressure != 0f)
            continue;
          var pressure = GetPressure(delivery.gate, interaction.GetOposite(delivery).gate);
          ModUtils.Log($"[WaterService.pressures] 01 gate={delivery.gate.id} gate={interaction.GetOposite(delivery).gate.id} pressure={pressure}");
          interaction.setPressure(delivery, pressure);
          ModUtils.Log($"[WaterService.pressures] 02 delivery={interaction.getDelivery().gate.id} delivery.pressureSum={interaction.getDelivery().pressureSum}");
          ModUtils.Log($"[WaterService.pressures] 03 requester={interaction.getRequester().gate.id} requester.pressureSum={interaction.getRequester().pressureSum}");
          if (interaction.pressure != 0f)
            nonePressure = false;
        }
      if (nonePressure)
      {
        ModUtils.Log($"[WaterService.distributeWater] aborted by nonePressure");
        return true;
      }
      // remove invalid deliveries 
      var deliveryRemoved = 0;
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        ModUtils.Log($"[WaterService.remove_delivery] loop gate={delivery.gate.id}");
        if (delivery.pressureSum <= 0f)
        {
          ModUtils.Log($"[WaterService.remove_delivery] gate={delivery.gate.id} removed");
          delivery.forceRequester = true;
          deliveryRemoved += 1;
        }
      }
      if (group.Deliveries - deliveryRemoved == 0)
      {
        ModUtils.Log($"[WaterService.distributeWater] aborted by deliveries={group.Deliveries - deliveryRemoved} after removed invalids");
        return true;
      }
      // set quota
      foreach (var requester in RequesterIterator(group.WaterGates))
        foreach (var interaction in requester.Interactions.Values.Where(interaction => interaction.isRequester(requester) && requester.pressureSum != 0f))
        {
          ModUtils.Log($"[WaterService.set_quota] loop gate={requester.gate.id} iter");
          ModUtils.Log($"[WaterService.set_quota] 01 interaction.pressure={interaction.pressure} interaction.pressureSum={requester.pressureSum}");
          var quota = interaction.pressure / requester.pressureSum;
          interaction.setQuota(quota);
        }
      // set water oferted
      foreach (var delivery in DeliveryIterator(group.WaterGates))
        foreach (var interaction in delivery.Interactions.Values.Where(interaction => interaction.isDelivery(delivery) && delivery.quotaSum != 0f))
        {
          ModUtils.Log($"[WaterService.set_water_offer] loop gate={delivery.gate.id} iter");
          var water = GetWaterDelivered(delivery.gate, interaction.getRequester().gate);
          ModUtils.Log($"[WaterService.set_water_offer] 01 water={water}");
          if (water == 0f)
            continue;
          var quota = interaction.quota / delivery.quotaSum;
          var waterQuota = water * quota;
          ModUtils.Log($"[WaterService.set_water_offer] 02 interaction.quota={interaction.quota} delivery.quotaSum={delivery.quotaSum} quota={quota} waterQuota={waterQuota}");
          if (delivery.gate.IsWaterPump)
            interaction.setWaterBlockedByPump(waterQuota);
          interaction.setWaterOferted(waterQuota * delivery.gate.PowerEfficiency);
          interaction.setContamination(delivery.gate.ContaminationPercentage * quota);
        }
      // discovery requesters water
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        ModUtils.Log($"[WaterService.set_req_water] loop gate={requester.gate.id}");
        var waterMove = GetWaterTopLimit(requester.gate, requester.waterOfertedSum, hasDelivery: false);
        ModUtils.Log($"[WaterService.set_req_water] 01 waterOfertedSum={requester.waterOfertedSum} waterMove={waterMove}");
        requester.WaterMove = waterMove * requester.gate.PowerEfficiency;
        requester.Contamination = requester.contaminationSum;
        requester.WaterUsed = requester.WaterMove > 0f
          ? (requester.WaterMove / requester.waterOfertedSum)
          : 0f;
        ModUtils.Log($"[WaterService.set_req_water] 02 waterBlockedByPumpSum={requester.waterBlockedByPumpSum}");
        var willMoveWaterWithPump = GetWaterTopLimit(requester.gate, requester.waterOfertedSum + requester.waterBlockedByPumpSum, hasDelivery: false);
        requester.AddPumpRequested(willMoveWaterWithPump);
        requester.SendPumpActivateEvent(willMoveWaterWithPump);
      }
      // discovery deliveries water
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        ModUtils.Log($"[WaterService.set_del_water] loop gate={delivery.gate.id}");
        if (delivery.quotaSum == 0f)
          continue;
        foreach (var interaction in delivery.Interactions.Values)
        {
          delivery.WaterMove += -(interaction.waterOferted * interaction.getRequester().WaterUsed);
        }
        delivery.Contamination = delivery.gate.ContaminationPercentage;
      }
      ModUtils.Log($"[WaterService.distributeWater] success");
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
      ModUtils.Log($"[WaterService.StopWater] start group={group.id}");
      foreach (var context in group.WaterGates)
      {
        context.gate.powered?.DisablePowerConsumption();
        context.gate.RemoveWaterParticles();
      }
      ModUtils.Log($"[WaterService.StopWater] end");
    }

    private static void moveWater(PipeGroup group)
    {
      var factor = Time.fixedDeltaTime * waterFactor;
      foreach (var context in group.WaterGates)
      {
        context.gate.MoveWater(context.WaterMove * factor, context.Contamination);
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
