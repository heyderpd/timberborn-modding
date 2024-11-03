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
    }

    public void EnablePowerConsumption()
    {
      if (!mechanicalNode)
        return;
      mechanicalNode.UpdateInput(1);
    }

    public float PowerEfficiency
    {
      get
      {
        if (mechanicalNode?.ActiveAndPowered != true)
          return 0f;
        return mechanicalNode.PowerEfficiency;
      }
    }
  }
}
