using System;
using Timberborn.BaseComponentSystem;
using Timberborn.MechanicalSystem;
using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class PipeNodePowered : BaseComponent
  {
    private MechanicalNode mechanicalNode;

    private bool GraphValid => mechanicalNode?.Graph?.Valid ?? false;

    private bool NodeInvalid => mechanicalNode == null;

    private bool NodeActiveAndPowered => mechanicalNode?.ActiveAndPowered ?? false;

    [NonSerialized]
    public bool Active = true;

    public void Awake()
    {
      mechanicalNode = GetComponentFast<MechanicalNode>();
    }

    public void InitializeEntity()
    {
      DisablePowerConsumption();
    }

    public void DisablePowerConsumption()
    {
      if (NodeInvalid)
        return;
      mechanicalNode.Active = false;
      if (GraphValid)
        mechanicalNode.UpdateInput(0);
    }

    public void EnablePowerConsumption()
    {
      if (NodeInvalid || !Active)
        return;
      mechanicalNode.Active = true;
      if (GraphValid)
        mechanicalNode.UpdateInput(1);
    }

    public float PowerEfficiency
    {
      get
      {
        if (!Active || NodeInvalid || !NodeActiveAndPowered)
          return 0f;
        return mechanicalNode.PowerEfficiency;
      }
    }
  }
}
