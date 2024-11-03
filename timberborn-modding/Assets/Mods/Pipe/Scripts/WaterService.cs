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

    public static readonly float minimumFlow = 0.005f;

    public static readonly float minimumPressure = 0.05f;

    public static readonly float waterFactor = 0.36f * Time.fixedDeltaTime; // common max to 0.8cms

    public static readonly float pumpHeight = 6.50f;

    public static readonly float pumpPressure = 1.00f;

    public static readonly float pumpRate = 0.50f; // pump max to 0.4cms

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

    private static float CalcPressure(float FloorLevel, float WaterLevel)
    {
      var levelPressure = FloorLevel * FloorLevel;
      var waterPressure = WaterLevel > 0f
        ? WaterLevel * WaterLevel
        : 0f;
      var pressure = Mathf.Max(levelPressure, waterPressure);
      return pressure;
    }

    public static float CalcPressure(WaterGate gate)
    {
      return CalcPressure(gate.LowerLimit, gate.WaterLevel);
    }

    public static float CalcPumpPressure(float pumpLevel)
    {
      return CalcPressure(pumpLevel, 0f);
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
      var invalidPressure = input.IsOnlyRequester || output.IsOnlyDelivery;
      if (invalidPressure)
        return 0f;
      var pumpLevel = input.LowerLimit + pumpHeight;
      var outOfRange = pumpLevel < output.LowerLimit;
      if (outOfRange)
        return 0f;
      return pumpPressure;
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

    private static float GetPumpLimit(WaterGate pump, float water, bool hasDelivery)
    {
      if (hasDelivery && pump.IsOnlyRequester)
        return 0f;
      if (!hasDelivery && pump.IsOnlyDelivery)
        return 0f;
      return LimitWater(water, pumpRate);
    }

    public static float LimitRequesterWater(WaterGate output, float water)
    {
      if (water <= 0f)
        return 0f;
      if (output.IsValve)
        return GetSubmergeLimit(output, water);
      else if (output.IsWaterPump)
        return GetPumpLimit(output, water, hasDelivery: false);
      else
        return LimitWater(water);
    }

    public static float GetDeliveryWater(WaterGate input, WaterGate output)
    {
      if (input.WaterAvailable <= 0f)
        return 0f;
      if (input.IsValve)
        return 0f;
      else if (input.IsWaterPump || output.IsWaterPump)
        return GetPumpLimit(input, input.WaterAvailable, hasDelivery: true);
      else
      {
        var water = Mathf.Max(input.WaterLevel - output.WaterLevel, 0f);
        water = water > 0f ? water / 2 : 0f;
        return LimitWater(water);
      }
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
      if (group.Deliveries == 0 || group.Requesters == 0)
      {
        ModUtils.Log($"[WaterService.distributeWater] aborted by deliveries={group.Deliveries} requesters={group.Requesters}");
        return true;
      }
      foreach (var context in group.WaterGates)
      {
        context.checkPumpRequested();
        context.Reset();
        context.gate.UpdateWaters();
      }
      // discovery pressures
      var nonePressure = true;
      foreach (var delivery in DeliveryIterator(group.WaterGates))
        foreach (var interaction in delivery.Interactions.Values)
        {
          if (interaction.pressure != 0f)
            continue;
          var pressure = GetPressure(delivery.gate, interaction.GetOposite(delivery).gate);
          if (pressure <= 0f && interaction.GetOposite(delivery).gate.WaterAvailable <= 0f)
            continue;
          interaction.setPressure(delivery, pressure);
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
        if (delivery.pressureSum <= 0f)
        {
          delivery.forceRequester = true;
          deliveryRemoved += 1;
        }
      }
      if (group.Deliveries - deliveryRemoved == 0)
        return true;
      // set quota
      foreach (var requester in RequesterIterator(group.WaterGates))
        foreach (var interaction in requester.Interactions.Values.Where(interaction => interaction.isRequester(requester) && requester.pressureSum != 0f))
        {
          var quota = interaction.pressure / requester.pressureSum;
          interaction.setQuota(quota);
        }
      // set water oferted
      foreach (var delivery in DeliveryIterator(group.WaterGates))
        foreach (var interaction in delivery.Interactions.Values.Where(interaction => interaction.isDelivery(delivery) && delivery.quotaSum != 0f))
        {
          var water = GetDeliveryWater(delivery.gate, interaction.getRequester().gate);
          if (water == 0f)
            continue;
          var quota = interaction.quota / delivery.quotaSum;
          var waterQuota = water * quota;
          if (delivery.gate.IsWaterPump)
            interaction.setWaterBlockedByPump(waterQuota);
          interaction.setWaterOferted(waterQuota * delivery.gate.PowerEfficiency);
          interaction.setContamination(delivery.gate.ContaminationPercentage * quota);
        }
      // discovery requesters water
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        var waterMove = LimitRequesterWater(requester.gate, requester.waterOfertedSum);
        requester.WaterMove = waterMove * requester.gate.PowerEfficiency;
        requester.Contamination = requester.contaminationSum;
        requester.WaterUsed = requester.WaterMove > 0f
          ? (requester.WaterMove / requester.waterOfertedSum)
          : 0f;
        var willMoveWaterWithPump = LimitRequesterWater(requester.gate, requester.waterOfertedSum + requester.waterBlockedByPumpSum);
        requester.AddPumpRequested(willMoveWaterWithPump);
        requester.SendPumpActivateEvent(willMoveWaterWithPump);
      }
      // discovery deliveries water
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        if (delivery.quotaSum == 0f)
          continue;
        foreach (var interaction in delivery.Interactions.Values)
        {
          delivery.WaterMove += -(interaction.waterOferted * interaction.getRequester().WaterUsed);
        }
        delivery.Contamination = delivery.gate.ContaminationPercentage;
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
      foreach (var context in group.WaterGates)
      {
        context.gate.MoveWater(context.WaterMove * waterFactor, context.Contamination);
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
