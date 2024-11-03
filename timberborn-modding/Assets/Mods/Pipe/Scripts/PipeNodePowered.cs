using Timberborn.BaseComponentSystem;
using Timberborn.MechanicalSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeNodePowered : BaseComponent
  {
    private MechanicalNode mechanicalNode;

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
      ModUtils.Log($"[PipeNodePowered] 01 DisablePowerConsumption will Disable : checkA={NodeInvalid} checkB={GraphValid}");
      if (NodeInvalid)
        return;
      mechanicalNode.Active = false;
      if (GraphValid)
        mechanicalNode.UpdateInput(0);
      ModUtils.Log($"[PipeNodePowered] 02 DisablePowerConsumption is Disabled");
    }

    public void EnablePowerConsumption()
    {
      ModUtils.Log($"[PipeNodePowered] 01 EnablePowerConsumption will Enable : checkA={NodeInvalid} checkB={GraphValid}");
      if (NodeInvalid)
        return;
      mechanicalNode.Active = true;
      if (GraphValid)
        mechanicalNode.UpdateInput(1);
      ModUtils.Log($"[PipeNodePowered] 02 EnablePowerConsumption is Enabled");
    }

    public float PowerEfficiency
    {
      get
      {
        ModUtils.Log($"[PipeNodePowered] 01 PowerEfficiency will get value : checkA={NodeInvalid} checkB={NodeActiveAndPowered}");
        if (NodeInvalid || !NodeActiveAndPowered)
          return 0f;
        ModUtils.Log($"[PipeNodePowered] 02 PowerEfficiency={mechanicalNode.PowerEfficiency}");
        return mechanicalNode.PowerEfficiency;
      }
    }
  }
}
