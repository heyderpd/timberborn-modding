using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class WaterService
  {

    public static readonly float maximumFlow = 1.00f;

    public static readonly float minimumFlow = 0.001f;

    public static readonly float minimumPressure = 0.05f;

    public static readonly float waterFactor = 0.36f; // common max cms = 0.7

    public static readonly float pumpHeight = 10.00f;

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
        if (!IsDelivery && context.gate.IsOnlyDelivery || context.forceDelivery)
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
      var pumpLevel = input.LowerLimit + pumpHeight;
      var pumpPressure = CalcPressure(input.WaterAvailable, pumpLevel) + 1;
      ModUtils.Log($"[WaterService.GetPumpPressure] pumpLevel={pumpLevel} pumpPressure={pumpPressure} output={output.WaterPressure} result={pumpPressure - output.WaterPressure}");
      ModUtils.Log($"[WaterService.GetPumpPressure] checkA={input.LowerLimit < output.LowerLimit} checkB={pumpLevel > output.LowerLimit}");
      if (input.LowerLimit < output.LowerLimit)
      {
        if (pumpLevel > output.LowerLimit)
          return pumpPressure - output.WaterPressure;
        else
          return 0f;
      }
      else
        return pumpPressure - output.WaterPressure;
    }

    private static float GetPressure(WaterGate input, WaterGate output)
    {
      return (input.IsWaterPump || output.IsWaterPump)
        ? GetPumpPressure(input, output)
        : GetCommomPressure(input, output);
    }

    private static float GetSubmergeLimit(WaterGate gate, float waterAdd)
    {
      ModUtils.Log($"[WaterService.GetSubmergeLimit] s01 waterAdd={waterAdd}");
      var waterLevel = gate.WaterLevel + waterAdd;
      ModUtils.Log($"[WaterService.GetSubmergeLimit] s02 waterLevel={waterLevel}");
      var waterExceded = Mathf.Max(waterLevel - gate.HigthLimit, 0f);
      ModUtils.Log($"[WaterService.GetSubmergeLimit] s03 waterExceded={waterExceded}");
      var waterLevelFixed = waterLevel - waterExceded;
      ModUtils.Log($"[WaterService.GetSubmergeLimit] s04 waterLevelFixed={waterLevelFixed}");
      var water = waterLevelFixed - gate.WaterLevel;
      ModUtils.Log($"[WaterService.GetSubmergeLimit] s05 water={water}");
      return water;
    }

    private static float GetPumpLimit(WaterGate gate, float water)
    {
      water = LimitWater(water, pumpRate);
      if (water == 0f)
        return 0f;
      return water * gate.PowerEfficiency;
    }

    public static float GetWaterTopLimit(WaterGate gate, float water)
    {
      ModUtils.Log($"[WaterService.GetWaterTopLimit] 03 IsValve={gate.IsValve} water={water}");
      if (gate.IsValve)
        return GetSubmergeLimit(gate, water);
      else if (gate.IsWaterPump)
        return GetPumpLimit(gate, water);
      else
        return LimitWater(water);
    }

    public static float GetWaterDelivered(WaterGate input, WaterGate output)
    {
      ModUtils.Log($"[WaterService.distributeWater] 01 input={input.WaterLevel} output={output.WaterLevel}");
      var waterAvg = Mathf.Abs(input.WaterLevel - output.WaterLevel) / 2;
      var water = GetWaterTopLimit(input, waterAvg);
      ModUtils.Log($"[WaterService.distributeWater] 02 waterAvg={waterAvg} water={water}");
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
      {
        foreach (var interaction in delivery.Interactions.Values)
        {
          if (interaction.pressure != 0f)
            continue;
          var pressure = GetPressure(delivery.gate, interaction.GetOposite(delivery).gate);
          interaction.setPressure(delivery, pressure);
          if (interaction.pressure != 0f)
            nonePressure = false;
        }
      }
      if (nonePressure)
      {
        ModUtils.Log($"[WaterService.distributeWater] aborted by nonePressure");
        return true;
      }
      // remove invalid deliveries and requesters
      var deliveryRemoved = 0;
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        if (delivery.pressureSum > 0f)
        {
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
      {
        foreach (var interaction in requester.Interactions.Values)
        {
          if (interaction.quota != 0f || interaction.pressure == 0f)
            continue;
          var quota = interaction.pressure / requester.pressureSum;
          interaction.setQuota(requester, quota);
          ModUtils.Log($"[WaterService.distributeWater] pressure={interaction.pressure} pressureSum={requester.pressureSum} quota={quota}  requester.quotaSum={requester.quotaSum} GetOposite.quotaSum={interaction.GetOposite(requester).quotaSum}");
        }
      }
      // set water oferted
      foreach (var delivery in DeliveryIterator(group.WaterGates))
      {
        foreach (var interaction in delivery.Interactions.Values)
        {
          if (interaction.waterOferted != 0f || delivery.quotaSum == 0f)
            continue;
          var water = GetWaterDelivered(delivery.gate, interaction.getRequester().gate);
          var quota = interaction.quota / delivery.quotaSum;
          ModUtils.Log($"[WaterService.distributeWater] 0F interaction={interaction.quota} quotaSum={delivery.quotaSum} quota={quota}");
          interaction.setWaterOferted(water * quota);
          interaction.setContamination(delivery.gate.ContaminationPercentage * quota);
        }
      }
      // discovery requesters water
      foreach (var requester in RequesterIterator(group.WaterGates))
      {
        requester.WaterMove = GetWaterTopLimit(requester.gate, requester.waterOfertedSum);
        ModUtils.Log($"[WaterService.GetWaterTopLimit] 07 WaterMove={requester.WaterMove}");
        requester.Contamination = requester.contaminationSum;
        requester.WaterUsed = requester.WaterMove > 0f
          ? (requester.WaterMove / requester.waterOfertedSum)
          : 0f;
        requester.AddPumpRequested(requester.WaterMove > 0f);
        requester.SendPumpActivateEvent(requester.WaterMove > 0f);
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
