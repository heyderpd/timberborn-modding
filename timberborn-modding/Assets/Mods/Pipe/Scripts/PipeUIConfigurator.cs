using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  [Context("Game")]
  public class PipeWaterUIConfigurator : IConfigurator
  {
    public void Configure(IContainerDefinition containerDefinition)
    {
      containerDefinition.Bind<PipeGroupQueue>().AsSingleton();
      containerDefinition.Bind<PipeGroupManager>().AsSingleton();
      if (OldGopherLog.enabled)
      {
        containerDefinition.Bind<PipeFragment>().AsSingleton();
        containerDefinition.MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
      }
    }

    private class EntityPanelModuleProvider : IProvider<EntityPanelModule>
    {
      private readonly PipeFragment pipeFragment;

      public EntityPanelModuleProvider(
        PipeFragment _pipeFragment
      )
      {
        pipeFragment = _pipeFragment;
      }

      public EntityPanelModule Get()
      {
        var builder = new EntityPanelModule.Builder();
        builder.AddSideFragment(pipeFragment);
        return builder.Build();
      }
    }
  }
}
