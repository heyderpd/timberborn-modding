using Bindito.Core;
using Timberborn.Buildings;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateSystem;

namespace Mods.OldGopher.Pipe
{
  [Context("Game")]
  public class PipeWaterConfigurator : IConfigurator
  {
    public void Configure(IContainerDefinition containerDefinition)
    {
      containerDefinition.Bind<WaterObstacleMap>().AsSingleton();
      containerDefinition.Bind<WaterRadar>().AsSingleton();
      containerDefinition.Bind<WaterMapExtender>().AsSingleton();
      containerDefinition.Bind<WaterMapPatch>().AsSingleton();
      containerDefinition.Bind<PipeGroupQueue>().AsSingleton();
      containerDefinition.Bind<PipeGroupManager>().AsSingleton();
      containerDefinition.Bind<PipeFragment>().AsSingleton();
      containerDefinition.MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
      containerDefinition.MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
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
        builder.AddMiddleFragment(pipeFragment);
        return builder.Build();
      }
    }

    private static TemplateModule ProvideTemplateModule()
    {
      TemplateModule.Builder builder = new TemplateModule.Builder();
      builder.AddDecorator<WaterGate, BuildingParticleAttachment>();
      builder.AddDecorator<PipeNode, PipeStatus>();
      return builder.Build();
    }
  }
}
