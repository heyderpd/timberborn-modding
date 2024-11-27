using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;

namespace Mods.OldGopher.Pipe
{
  internal class GhostBlockObject
  {
    public readonly Vector3Int Coordinates;

    public readonly Vector3Int Size;

    public readonly Orientation Orientation;

    public readonly bool IsFlipped;

    public GhostBlockObject(BlockObject block)
    {
      Coordinates = block.Coordinates;
      Size = block.Blocks.Size;
      Orientation = block.Orientation;
      IsFlipped = block.FlipMode.IsFlipped;
    }
  }
}
