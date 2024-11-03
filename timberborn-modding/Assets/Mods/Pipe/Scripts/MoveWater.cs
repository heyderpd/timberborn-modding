using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mods.Pipe.Scripts
{
  internal static class MoveWater
  {
    static float maximumWaterInFlow = 5.00f;

    static float maximumWaterOutFlow = 5.00f;

    static float minimumWaterFlow = 0.01f;

    static float minimumDelivereWaters = 0.01f;
    
    static float waterPerSecond = 1.00f;
    
    private static float LimitWater(float expectedWater, bool inflow)
    {
      float water = Mathf.Abs(expectedWater);
      float maxWater = inflow ? maximumWaterInFlow : maximumWaterOutFlow;
      water = Mathf.Min(water, maxWater);
      water = water >= minimumWaterFlow ? water : 0f;
      return water;
    }

    private static float GetWaterUp(WaterGate gate, float expectedWaterLevel)
    {
      if (expectedWaterLevel > gate.Floor)
        return expectedWaterLevel - gate.Floor;
      else
        return gate.Water;
    }

    private static float GetWaterLow(WaterGate gate, float expectedWaterLevel)
    {
      return expectedWaterLevel - gate.WaterLevel;
    }

    private static float GetDeliveryWater(WaterGate gate, float average)
    {
      return LimitWater(GetWaterUp(gate, average), true);
    }

    private static float GetReceiveryWater(WaterGate gate, float average)
    {
      return GetWaterLow(gate, average);
    }

    private static float getWaterLevel(List<WaterGate> Gates)
    {
      float waterSum = 0f;
      float average = Gates
        .Aggregate(
          0f,
          (float sum, WaterGate gate) =>
          {
            gate.UpdateWaters();
            waterSum += gate.Water;
            return sum + gate.WaterLevel;
          }
        );
      if (waterSum <= 0)
        return float.NaN;
      return average / Gates.Count;
    }

    private static float GetWaterPercentPerSecond()
    {
      return waterPerSecond;
      //return Time.fixedDeltaTime * waterPerSecond;
      //return Mathf.Min(Time.fixedDeltaTime, 1f) * waterPerSecond; // Time.fixedDeltaTime = 0.6
    }

    private static bool calculateFlow(List<WaterGate> Gates, float average)
    {
      float delivereWaters = 0f, receivertWaters = 0f;
      var (deliverers, receivers) = Gates
        .Aggregate(
          new Tuple<List<WaterGate>, List<WaterGate>>(new List<WaterGate>(), new List<WaterGate>()),
          (Tuple<List<WaterGate>, List<WaterGate>> lists, WaterGate gate) =>
          {
            var (deliverers, requesters) = lists;
            if (gate.WaterLevel > average)
            {
              deliverers.Add(gate);
              gate.DesiredWater = GetDeliveryWater(gate, average);
              delivereWaters += gate.DesiredWater;
            } else if (gate.WaterLevel < average)
            {
              requesters.Add(gate);
              gate.DesiredWater = GetReceiveryWater(gate, average);
              receivertWaters += gate.DesiredWater;
            } else
            {
              gate.DesiredWater = 0f;
            }
            return lists;
          }
        );
      if (deliverers.Count == 0 || receivers.Count == 0 || delivereWaters < minimumDelivereWaters)
        return false;
      var waterUsed = receivers
        .Aggregate(
          0f,
          (float waterUsed, WaterGate gate) =>
          {
            float water = LimitWater(
              delivereWaters * (gate.DesiredWater / receivertWaters),
              false
            );
            gate.DesiredWater = water;
            waterUsed += gate.DesiredWater;
            return waterUsed;
          }
        );
      float waterNotUsed = delivereWaters - waterUsed;
      deliverers
        .ForEach((WaterGate gate) =>
        {
          float water = gate.DesiredWater;
          if (waterNotUsed > 0f)
            water -= (waterNotUsed * (water / delivereWaters));
          gate.DesiredWater = water - gate.Water;
        });
      bool canFlow = Gates
        .Aggregate(
          true,
          (bool canFlow, WaterGate gate) =>
          {
            canFlow = gate.CheckFlowChanged(gate.DesiredWater) && canFlow;
            return canFlow;
          }
        );
      return canFlow;
    }

    private static void moveWater(List<WaterGate> Gates)
    {
      float waterPercentPerSecond = GetWaterPercentPerSecond();
      Gates
        .ForEach((WaterGate gate) =>
        {
          gate.MoveWater(gate.DesiredWater * waterPercentPerSecond);
        });
    }

    public static bool Do(List<WaterGate> Gates)
    {
      if (Gates.Count <= 1)
        return false;
      float average = getWaterLevel(Gates);
      if (float.IsNaN(average))
        return false;
      bool newFlow = calculateFlow(Gates, average);
      if (!newFlow)
        return false;
      moveWater(Gates);
      return true;
    }
  }
}
