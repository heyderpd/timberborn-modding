using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mods.Pipe.Scripts
{
  internal static class MoveWater
  {
    static float maximumWaterInFlow = 0.50f;

    static float maximumWaterOutFlow = 1.00f;

    static float minimumWaterFlow = 0.05f;

    static float minimumDelivereWaters = 0.05f;
    
    static float waterLostMaximumPercent = 0.90f;

    static float waterPerSecond = 0.50f;
    
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
      float average = Gates
        .Aggregate(
          0f,
          (float sum, WaterGate gate) =>
          {
            gate.UpdateWaters();
            return sum + gate.WaterLevel;
          }
        );
      return average / Gates.Count;
    }

    private static float GetWaterPercentPerSecond()
    {
      Debug.Log($"[MoveWater.GetWaterPercentPerSecond] fixedDeltaTime={Time.fixedDeltaTime} waterPerSecond={waterPerSecond} waterPercent={Time.fixedDeltaTime * waterPerSecond}");
      return Time.fixedDeltaTime * waterPerSecond;
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
      Debug.Log($"[MoveWater.calculateFlow] average={average} delivereWaters={delivereWaters} receivertWaters={receivertWaters} deliverers.Count={deliverers.Count} requesters.Count={receivers.Count}  deliveryWaterBreak={delivereWaters < minimumDelivereWaters}");
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
            Debug.Log($"[MoveWater.calculateFlow.receivers.loop] gate.id={gate.id} delivereWaters={delivereWaters} gate.DesiredWater={gate.DesiredWater} receivertWaters={receivertWaters} water={water} gate.Water={gate.Water} WaterDiff={water - gate.Water}");
            gate.DesiredWater = water;
            waterUsed += gate.DesiredWater;
            return waterUsed;
          }
        );
      float waterLost = delivereWaters - waterUsed;
      Debug.Log($"[MoveWater.calculateFlow.waterLost] waterLost={waterLost} delivereWaters={delivereWaters} waterUsed={waterUsed} lostPercent={waterLost / delivereWaters} waterLostBreak={waterLost / delivereWaters < waterLostMaximumPercent}");
      if (waterLost / delivereWaters > waterLostMaximumPercent)
        return false;
      deliverers
        .ForEach((WaterGate gate) =>
        {
          float water = gate.DesiredWater;
          if (waterLost > 0f)
            water -= (waterLost * (water / delivereWaters));
          Debug.Log($"[MoveWater.calculateFlow.deliverers.loop] gate.id={gate.id} gate.DesiredWater={gate.DesiredWater} waterLost={waterLost} Twater={water} delivereWaters={delivereWaters} gate.Water={gate.Water}  WaterDiff={water - gate.Water}");
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
      Debug.Log($"[MoveWater.calculateFlow] delivereWaters={delivereWaters} waterUsed={waterUsed} waterLost={waterLost} canFlow={canFlow}");
      return canFlow;
    }

    private static void moveWater(List<WaterGate> Gates)
    {
      float waterPercentPerSecond = GetWaterPercentPerSecond();
      Gates
        .ForEach((WaterGate gate) =>
        {
          Debug.Log($"[MoveWater.moveWater.loop] gate.id={gate.id} gate.WaterDiff={gate.DesiredWater} waterMoved={gate.DesiredWater * waterPercentPerSecond}");
          gate.MoveWater(gate.DesiredWater * waterPercentPerSecond);
        });
    }

    public static bool Do(List<WaterGate> Gates)
    {
      if (Gates.Count <= 1)
        return false;
      float average = getWaterLevel(Gates);
      Debug.Log($"[MoveWater.Do] Gates.Count={Gates.Count}");
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
