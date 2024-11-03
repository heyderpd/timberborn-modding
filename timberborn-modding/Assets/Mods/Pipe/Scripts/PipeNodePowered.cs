using Timberborn.BaseComponentSystem;
using Timberborn.MechanicalSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeNodePowered : BaseComponent
  {
    private MechanicalNode mechanicalNode;

    private TickCount Tick = new TickCount(2);

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
      if (!mechanicalNode || Tick.Skip())
        return;
      mechanicalNode.UpdateInput(0);
      ModUtils.Log($"[PipeNodePowered.EnablePowerConsumption] DISABLED");
    }

    public void EnablePowerConsumption()
    {
      if (!mechanicalNode)
        return;
      mechanicalNode.UpdateInput(1);
      ModUtils.Log($"[PipeNodePowered.EnablePowerConsumption] ENABLED");
    }

    public float PowerEfficiency
    {
      get
      {
        ModUtils.Log($"[PipeNodePowered.PowerEfficiency] 01 mechanicalBuilding={mechanicalNode?.ActiveAndPowered}");
        if (mechanicalNode?.ActiveAndPowered != true)
          return 0f;
        ModUtils.Log($"[PipeNodePowered.PowerEfficiency] 02 PowerEfficiency={mechanicalNode.PowerEfficiency}");
        return mechanicalNode.PowerEfficiency;
      }
    }
  }
}
