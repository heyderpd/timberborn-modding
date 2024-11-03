using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal static class WaterService
  {
    public static readonly float maximumFlow = 1.00f;

    public static readonly float minimumFlow = 0.001f;

    public static readonly float waterPercentPerSecond = ModUtils.waterPower;

    private static float getWaterLevel(List<WaterGate> Gates)
    {
      bool success = true;
      float waterSum = 0f;
      float average = Gates
        .Aggregate(
          0f,
          (float sum, WaterGate gate) =>
          {
            success = gate.UpdateWaters() && success;
            waterSum += gate.Water;
            return sum + gate.WaterLevel;
          }
        );
      //ModUtils.Log($"[MoveWater.getWaterLevel] waterSum={waterSum}");
      if (!success || waterSum <= minimumFlow)
        return float.NaN;
      return average / Gates.Count;
    }

    //TODO: improve deliverers and requesters to only discovery it when necessary
    private static Tuple<bool, float> calculateFlow(List<WaterGate> Gates, float average)
    {
      float delivereWaters = 0f, requestersWaters = 0f, contamination = 0f;
      var (deliverers, requesters) = Gates
        .Aggregate(
          new Tuple<List<WaterGate>, List<WaterGate>>(new List<WaterGate>(), new List<WaterGate>()),
          (Tuple<List<WaterGate>, List<WaterGate>> lists, WaterGate gate) =>
          {
            var (deliverers, requesters) = lists;
            if (gate.CanDelivereryWater(average))
            {
              deliverers.Add(gate);
              gate.DesiredWater = gate.GetDeliveryWater(average);
              delivereWaters += gate.DesiredWater;
              contamination += gate.ContaminationPercentage;
              //ModUtils.Log($"[MoveWater.calculateFlow] Pre deliverers gate.DesiredWater={gate.DesiredWater}");
            } else if (gate.CanRequestWater(average))
            {
              requesters.Add(gate);
              gate.DesiredWater = gate.GetRequesterWater(average);
              requestersWaters += gate.DesiredWater;
              //ModUtils.Log($"[MoveWater.calculateFlow] Pre requesters gate.DesiredWater={gate.DesiredWater}");
            } else
            {
              gate.DesiredWater = 0f;
            }
            return lists;
          }
        );
      //ModUtils.Log($"[MoveWater.calculateFlow] deliverers={deliverers.Count} receivers={requesters.Count} receivers={delivereWaters} receivers={requestersWaters} check={(deliverers.Count == 0 || requesters.Count == 0 || delivereWaters < minimumFlow || requestersWaters < minimumFlow)}");
      if (deliverers.Count == 0 || requesters.Count == 0 || delivereWaters < minimumFlow || requestersWaters < minimumFlow)
        return new Tuple<bool, float>(false, 0f);
      var waterUsed = requesters
        .Aggregate(
          0f,
          (float waterUsed, WaterGate gate) =>
          {
            float water = delivereWaters * (gate.DesiredWater / requestersWaters);
            gate.DesiredWater = Mathf.Min(water, gate.DesiredWater);
            //ModUtils.Log($"[MoveWater.Do] Final requesters gate.DesiredWater={gate.DesiredWater}");
            waterUsed += gate.DesiredWater;
            return waterUsed;
          }
        );
      float waterUsedPercent = waterUsed / delivereWaters;
      //ModUtils.Log($"[MoveWater.calculateFlow] delivereWaters={delivereWaters} waterUsed={waterUsed} waterUsedPercent={waterUsedPercent}");
      deliverers
        .ForEach((WaterGate gate) =>
          {
            float water = gate.DesiredWater * waterUsedPercent;
            //ModUtils.Log($"[MoveWater.calculateFlow] Final deliverers gate.DesiredWater={gate.DesiredWater} water={-water}");
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
      contamination = contamination / deliverers.Count;
      return new Tuple<bool, float>(canFlow, contamination);
    }

    private static void StopWater(List<WaterGate> Gates)
    {
      Gates
        .ForEach((WaterGate gate) =>
        {
          gate.RemoveWaterParticles();
        });
    }

    private static void moveWater(List<WaterGate> Gates, float contamination)
    {
      Gates
        .ForEach((WaterGate gate) =>
        {
          gate.MoveWater(gate.DesiredWater * waterPercentPerSecond, contamination);
        });
    }

    public static bool Do(PipeGroup group)
    {
      if (group.WaterGates.Count <= 1)
        return false;
      var average = getWaterLevel(group.WaterGates);
      if (float.IsNaN(average))
      {
        StopWater(group.WaterGates);
        return false;
      }
      group.WaterAverage = average;
      var (canFlow, contamination) = calculateFlow(group.WaterGates, average);
      if (!canFlow)
        return false;
      //ModUtils.Log($"[MoveWater.calculateFlow] average={average} contamination={contamination}");
      moveWater(group.WaterGates, contamination);
      return true;
    }
  }
}
