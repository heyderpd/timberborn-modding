using UnityEngine;

namespace Mods.OldGopher.Pipe
{
  internal class ShieldNode
  {
    public Vector3Int coordinate { get; private set; }

    public bool active = false;

    public ShieldNode(Vector3Int _coordinate)
    {
      coordinate = _coordinate;
    }
  }
}
