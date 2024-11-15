using UnityEngine;

namespace Mods.OldGopher.Pipe.Scripts
{
  public readonly struct WaterAddition
  {
    public float Water { get; }

    public float ContaminatedPercentage { get; }

    public WaterAddition(float _Water, float _ContaminatedPercentage)
    {
      Water = _Water;
      ContaminatedPercentage = _ContaminatedPercentage;
    }
  }
}
