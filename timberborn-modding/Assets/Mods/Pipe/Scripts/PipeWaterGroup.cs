using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WaterSystem;
using Timberborn.BlockSystem;
using UnityEngine;
using System.Collections.Generic;
using Timberborn.MechanicalSystem;
using Timberborn.WaterBuildings;
using System.Linq;
using Timberborn.TickSystem;
using System;
using Timberborn.Common;
using UnityEngine.Windows;
using Timberborn.TimbermeshDTO;

namespace Mods.Pipe.Scripts
{
  internal class PipeWaterGroup
  {
    private int ticks = 0;

    private bool changed = true;

    private readonly HashSet<PipeWaterLink> _nodes = new HashSet<PipeWaterLink>();

    private List<PipeWaterInput> waterInputs = new List<PipeWaterInput>();

    public bool Same(PipeWaterGroup group)
    {
      return this == group;
    }

    public void Add(PipeWaterLink node)
    {
      _nodes.Add(node);
      changed = true;
      tryDiscoveryInputs(); // remove
    }

    public void Remove(PipeWaterLink node)
    {
      _nodes.Remove(node);
      changed = true;
      tryDiscoveryInputs(); // remove
    }

    public void Union(PipeWaterGroup group)
    {
      if (Same(group))
      {
        return;
      }
      foreach (var node in _nodes)
      {
        node.SetGroup(group);
      }
    }

    public List<PipeWaterLink> GetNodes()
    {
      return _nodes.ToList();
    }

    public void tryDiscoveryInputs()
    {
      if (_nodes.Count == 0)
      {
        return;
      }
      //waterInputs.Clear(); // uncomment
      foreach (var link in _nodes)
      {
        foreach (var input in link.waterInputs)
        {
          waterInputs.Add(input);
        }
      }
      return;
      /* // --- dead code
      if (!changed)
      {
        return;
      }
      waterInputs = _nodes.ToList()
        .Where((PipeWaterLink link) => link.inputsEnabled)
        .Aggregate(
          new List<PipeWaterInput>(),
          (List<PipeWaterInput> list, PipeWaterLink link) =>
          {
            list.AddRange(
              link.waterInputs
                .Where((PipeWaterInput input) => input.inputEnabled)
            );
            return list;
          }
        )
        .ToList();
      changed = false;*/
    }

    private void tryWaterMove()
    {
      /*//if (waterInputs.Count <= 1)
      if (waterInputs.Count == 0)
      {
        return;
      }
      float average = 0f;
      foreach (var input in waterInputs)
      {
        input.UpdateAvailableWaters();
        average += input.Water;
      }
      average = average / waterInputs.Count;
      foreach (var input in waterInputs)
      {
        input.MoveWater(average - input.Water, 0f);
      }*/
    }

    public void Tick()
    {
      tryDiscoveryInputs();
      tryWaterMove();
      /*
      tryDiscoveryInputs();
      ticks += 1;
      if (ticks < _nodes.Count)
      {
        return;
      }
      ticks = 0;
      tryWaterMove();*/
    }

    public string GetInfo()
    {
      if (waterInputs.Count == 0)
        return "empty";
      string info = "";
      foreach (var input in waterInputs)
      {
        info += ";\n";
        info = input.GetInfo();
      }
      return info;
    }
  }
}
