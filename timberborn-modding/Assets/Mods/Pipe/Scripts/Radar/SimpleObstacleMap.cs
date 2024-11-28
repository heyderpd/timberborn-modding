using System.Linq;
using System.Collections.Immutable;
using Timberborn.BlockSystem;
using Timberborn.WaterObjects;
using Timberborn.PrefabSystem;

namespace Mods.OldGopher.Pipe
{
  internal static class SimpleObstacleMap
  {
    private static ImmutableArray<string> invalidBuilds = ImmutableArray.Create("floodgate", "levee", "sluice");

    public static bool Exist(BlockObject blockMiddle)
    {
      if (blockMiddle  == null)
        return false;
      var prafabName = blockMiddle?.GetComponentFast<Prefab>()?.Name.ToLower() ?? "";
      if (prafabName != "" && invalidBuilds.FirstOrDefault(name => prafabName.Contains(name)) != null)
        return true;
      if (blockMiddle?.GetComponentFast<WaterObstacle>() != null)
        return true;
      return false;
    }
  }
}
