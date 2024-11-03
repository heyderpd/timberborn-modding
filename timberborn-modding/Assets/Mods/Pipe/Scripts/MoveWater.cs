using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class MoveWater
  {
    static readonly float maximumFlow = 1.00f;

    static readonly float minimumFlow = 0.001f;
    
    static readonly float waterPercentPerSecond = 0.50f;
    
    private static float LimitWater(float expectedWater)
    {
      float water = Mathf.Abs(expectedWater);
      water = Mathf.Min(water, maximumFlow);
      water = water >= minimumFlow ? water : 0f;
      return water;
    }

    private static float GetDeliveryWater(WaterGate gate, float average)
    {
      var limited = LimitWater(gate.Water);
      return limited;
    }

    private static float GetReceiveryWater(WaterGate gate, float average)
    {
      var water = average - gate.WaterLevel;
      var limited = LimitWater(water);
      return limited;
    }

    private static Tuple<float, float> getWaterLevel(List<WaterGate> Gates)
    {
      float waterSum = 0f;
      float contamination = 0f;
      float average = Gates
        .Aggregate(
          0f,
          (float sum, WaterGate gate) =>
          {
            gate.UpdateWaters();
            waterSum += gate.Water;
            contamination += gate.ContaminationPercentage;
            return sum + gate.WaterLevel;
          }
        );
      OldGopherLog.Log($"[MoveWater.getWaterLevel] waterSum={waterSum}");
      if (waterSum <= minimumFlow)
        return new Tuple<float, float>(float.NaN, float.NaN);
      average = average / Gates.Count;
      contamination = contamination / Gates.Count;
      return new Tuple<float, float>(average, contamination);
    }

    private static bool calculateFlow(List<WaterGate> Gates, float average)
    {
      float delivereWaters = 0f, requestersWaters = 0f;
      var (deliverers, requesters) = Gates
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
              OldGopherLog.Log($"[MoveWater.calculateFlow] Pre deliverers gate.DesiredWater={gate.DesiredWater}");
            } else if (gate.WaterLevel < average)
            {
              requesters.Add(gate);
              gate.DesiredWater = GetReceiveryWater(gate, average);
              requestersWaters += gate.DesiredWater;
              OldGopherLog.Log($"[MoveWater.calculateFlow] Pre requesters gate.DesiredWater={gate.DesiredWater}");
            } else
            {
              gate.DesiredWater = 0f;
            }
            return lists;
          }
        );
      OldGopherLog.Log($"[MoveWater.calculateFlow] deliverers={deliverers.Count} receivers={requesters.Count} receivers={delivereWaters} receivers={requestersWaters} check={(deliverers.Count == 0 || requesters.Count == 0 || delivereWaters < minimumFlow || requestersWaters < minimumFlow)}");
      if (deliverers.Count == 0 || requesters.Count == 0 || delivereWaters < minimumFlow || requestersWaters < minimumFlow)
        return false;
      var waterUsed = requesters
        .Aggregate(
          0f,
          (float waterUsed, WaterGate gate) =>
          {
            float water = delivereWaters * (gate.DesiredWater / requestersWaters);
            gate.DesiredWater = Mathf.Min(water, gate.DesiredWater);
            OldGopherLog.Log($"[MoveWater.Do] Final requesters gate.DesiredWater={gate.DesiredWater}");
            waterUsed += gate.DesiredWater;
            return waterUsed;
          }
        );
      float waterUsedPercent = waterUsed / delivereWaters;
      OldGopherLog.Log($"[MoveWater.calculateFlow] delivereWaters={delivereWaters} waterUsed={waterUsed} waterUsedPercent={waterUsedPercent}");
      deliverers
        .ForEach((WaterGate gate) =>
          {
            float water = gate.DesiredWater * waterUsedPercent;
            OldGopherLog.Log($"[MoveWater.calculateFlow] Final deliverers gate.DesiredWater={gate.DesiredWater} water={-water}");
            gate.DesiredWater = -water;
          });
      bool canFlow = Gates
        .Aggregate(
          true,
          (bool canFlow, WaterGate gate) =>
          {
            canFlow = gate.FlowNotChanged(gate.DesiredWater) && canFlow;
            return canFlow;
          }
        );
      return canFlow;
    }

    private static void moveWater(List<WaterGate> Gates, float contamination)
    {
      Gates
        .ForEach((WaterGate gate) =>
        {
          gate.MoveWater(gate.DesiredWater * waterPercentPerSecond, contamination);
        });
    }

    public static bool Do(List<WaterGate> Gates)
    {
      if (Gates.Count <= 1)
        return false;
      var (average, contamination) = getWaterLevel(Gates);
      OldGopherLog.Log($"[MoveWater.calculateFlow] average={average} contamination={contamination}");
      if (float.IsNaN(average))
        return false;
      bool canFlow = calculateFlow(Gates, average);
      if (!canFlow)
        return false;
      moveWater(Gates, contamination);
      return true;
    }
  }
}
