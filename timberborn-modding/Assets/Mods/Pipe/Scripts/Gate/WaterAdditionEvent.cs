namespace Mods.OldGopher.Pipe
{
  public readonly struct WaterAdditionEvent
  {
    public float Water { get; }

    public float ContaminatedPercentage { get; }

    public WaterAdditionEvent(float _Water, float _ContaminatedPercentage)
    {
      Water = _Water;
      ContaminatedPercentage = _ContaminatedPercentage;
    }
  }
}
