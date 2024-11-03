using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.CoreSound;
using Timberborn.SoundSystem;
using UnityEngine;

namespace Mods.ShantySpeaker.Scripts {
  internal class FinishableBuildingSoundPlayer : BaseComponent,
                                                 IFinishedStateListener {

    private ISoundSystem _soundSystem;

    [Inject]
    public void InjectDependencies(ISoundSystem soundSystem) {
      _soundSystem = soundSystem;
    }

    public void OnEnterFinishedState() {
    }

    public void OnExitFinishedState() {
    }

  }
}