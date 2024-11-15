using Timberborn.BaseComponentSystem;
using Timberborn.MechanicalSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeNodePowered : BaseComponent
  {
    private MechanicalNode mechanicalNode;

    public bool Active = true;

    private bool GraphValid => mechanicalNode?.Graph?.Valid ?? false;

    private bool NodeInvalid => mechanicalNode == null;

    private bool NodeActiveAndPowered => mechanicalNode?.ActiveAndPowered ?? false;

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
      if (NodeInvalid)
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
