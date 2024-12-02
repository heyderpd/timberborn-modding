using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bindito.Core;
using UnityEngine;
using Timberborn.BlockSystem;
using Timberborn.WaterSourceSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.TickSystem;
using System.Numerics;
using Timberborn.NeedSystem;

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
