using System;
using Bindito.Core;
using Timberborn.Localization;
using Timberborn.BaseComponentSystem;
using Timberborn.StatusSystem;
using Timberborn.BuildingsBlocking;
using Timberborn.BlockSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class PipeStatus : BaseComponent, IFinishedStateListener
  {
    private static readonly string BlockedPipeLocKey = "Building.PipeGeneric.Blocked";

    private static readonly string BlockedPipeShortLocKey = "Building.PipeGeneric.Blocked.Short";

    private ILoc loc;

    private BlockableBuilding blockableBuilding;

    private StatusToggle status;

    private bool isBlocked;

    [Inject]
    public void InjectDependencies(
      ILoc _loc
    )
    {
      loc = _loc;
    }

    public void Awake()
    {
      ModUtils.Log($"[PipeStatus.Awake] call");
      status = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("NoPower", loc.T(BlockedPipeLocKey), loc.T(BlockedPipeShortLocKey));
      GetComponentFast<StatusSubject>().RegisterStatus(status);
      blockableBuilding = GetComponentFast<BlockableBuilding>();
    }

    public void OnEnterFinishedState()
    {
      ModUtils.Log($"[PipeStatus.OnEnterFinishedState] call");
      blockableBuilding.BuildingBlocked += OnBuildingBlocked;
      blockableBuilding.BuildingUnblocked += OnBuildingUnblocked;
    }

    public void OnExitFinishedState()
    {
      ModUtils.Log($"[PipeStatus.OnExitFinishedState] call");
      blockableBuilding.BuildingBlocked -= OnBuildingBlocked;
      blockableBuilding.BuildingUnblocked -= OnBuildingUnblocked;
    }

    public void SetBlocked(WaterGate gate, bool state)
    {
      ModUtils.Log($"[PipeStatus.SetBlocked] call blockableBuilding={!blockableBuilding} status={status == null}");
      if (!blockableBuilding || status == null)
        return;
      ModUtils.Log($"[PipeStatus.SetBlocked] DO state={state}");
      isBlocked = state;
      if (state)
        blockableBuilding.Block(gate);
      else
        blockableBuilding.Unblock(gate);
    }

    private void OnBuildingBlocked(object sender, EventArgs e)
    {
      ModUtils.Log($"[PipeStatus.OnBuildingBlocked] call");
      status.Activate();
    }

    private void OnBuildingUnblocked(object sender, EventArgs e)
    {
      ModUtils.Log($"[PipeStatus.OnBuildingUnblocked] call");
      status.Deactivate();
    }
  }
}
