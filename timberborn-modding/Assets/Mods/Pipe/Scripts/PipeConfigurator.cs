using Bindito.Core;
using Mods.OldGopher.Pipe.Scripts;
using Timberborn.Buildings;
using Timberborn.EntityPanelSystem;
using Timberborn.PathSystem;
using Timberborn.Rendering;
using Timberborn.TemplateSystem;
using Timberborn.WaterBuildings;
using Timberborn.WaterObjects;

namespace Mods.OldGopher.Pipe.Scripts
{
  [Context("Game")]
  public class PipeWaterConfigurator : IConfigurator
  {
    public void Configure(IContainerDefinition containerDefinition)
    {
      containerDefinition.Bind<WaterRadar>().AsSingleton();
      containerDefinition.Bind<PipeGroupQueue>().AsSingleton();
      containerDefinition.Bind<PipeGroupManager>().AsSingleton();
      if (ModUtils.enabled)
      {
        containerDefinition.Bind<PipeFragment>().AsSingleton();
        containerDefinition.MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
      }
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
        builder.AddSideFragment(pipeFragment);
        return builder.Build();
      }
    }

    private static TemplateModule ProvideTemplateModule()
    {
      TemplateModule.Builder builder = new TemplateModule.Builder();
      builder.AddDecorator<WaterGate, BuildingParticleAttachment>();
      return builder.Build();
    }
  }
}
