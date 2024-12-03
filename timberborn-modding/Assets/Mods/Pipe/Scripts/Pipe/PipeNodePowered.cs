using System;
using UnityEngine;
using Timberborn.MechanicalSystem;
using Timberborn.BaseComponentSystem;

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

    public void DisablePowerConsumption()
    {
      Debug.Log($"PipeNodePowered.EnablePowerConsumption try NodeInvalid={NodeInvalid} GraphValid={GraphValid} Active={Active}");
      if (NodeInvalid)
        return;
      mechanicalNode.Active = false;
      if (GraphValid)
      {
        mechanicalNode.UpdateInput(0);
        Debug.Log("PipeNodePowered.EnablePowerConsumption disabled");
      }
    }

    public void EnablePowerConsumption()
    {
      Debug.Log($"PipeNodePowered.EnablePowerConsumption try NodeInvalid={NodeInvalid} GraphValid={GraphValid} Active={Active}");
      if (NodeInvalid || !Active)
        return;
      mechanicalNode.Active = true;
      if (GraphValid)
      {
        mechanicalNode.UpdateInput(1);
        Debug.Log("PipeNodePowered.EnablePowerConsumption enabled");
      }
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
