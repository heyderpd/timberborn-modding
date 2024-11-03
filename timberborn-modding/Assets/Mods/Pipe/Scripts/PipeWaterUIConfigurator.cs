using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace Mods.Pipe.Scripts
{
  [Context("Game")]
  public class PipeWaterUIConfigurator : IConfigurator
  {
    public void Configure(IContainerDefinition containerDefinition)
    {
      containerDefinition.Bind<PipeWaterFragment>().AsSingleton();
      //containerDefinition.MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
      containerDefinition.MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
    }

    /*private static TemplateModule ProvideTemplateModule() // apresenta sinalizacoes virtuais ao selecionar o objeto
    {
      TemplateModule.Builder builder = new TemplateModule.Builder();
      builder.AddDecorator<PipeWaterMove, SluiceMarker>();
      return builder.Build();
    }*/

    private class EntityPanelModuleProvider : IProvider<EntityPanelModule>
    {
      private readonly PipeWaterFragment _pipeWaterFragment;

      public EntityPanelModuleProvider(
        PipeWaterFragment pipeWaterFragment
      )
      {
        _pipeWaterFragment = pipeWaterFragment;
      }

      public EntityPanelModule Get()
      {
        var builder = new EntityPanelModule.Builder();
        builder.AddSideFragment(_pipeWaterFragment);
        return builder.Build();
      }
    }
  }
}
