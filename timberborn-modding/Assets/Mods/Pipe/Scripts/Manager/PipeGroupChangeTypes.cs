using System.Collections.Generic;
using Timberborn.Common;
using Timberborn.BlockSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  public enum PipeGroupChangeTypes
  {
    GROUP_RECALCULATE_GATES,
    GROUP_REMOVE,
    PIPE_CREATE,
    PIPE_REMOVE,
    PIPE_JOIN,
    GATE_CHECK_BY_BLOCKEVENT,
    PIPE_CHECK_GATES,
    GATE_CHECK
  }
}
