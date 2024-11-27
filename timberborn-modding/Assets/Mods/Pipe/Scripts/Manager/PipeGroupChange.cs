using System.Collections.Generic;
using Timberborn.Common;
using Timberborn.BlockSystem;

namespace Mods.OldGopher.Pipe
{
  internal readonly struct PipeGroupChange
  {
    public readonly PipeGroup group;

    public readonly PipeNode node;

    public readonly PipeNode secondNode;

    public readonly BlockObject blockObject;

    public readonly WaterGate gate;

    public readonly PipeGroupChangeTypes type;
    
    public PipeGroupChange(
      PipeGroupChangeTypes _type,
      PipeGroup _group = null,
      PipeNode _node = null,
      PipeNode _secondNode = null,
      BlockObject _blockObject = null,
      WaterGate _gate = null
    )
    {
      type = _type;
      group = _group;
      node = _node;
      secondNode = _secondNode;
      blockObject = _blockObject;
      gate = _gate;
    }
  }
}
