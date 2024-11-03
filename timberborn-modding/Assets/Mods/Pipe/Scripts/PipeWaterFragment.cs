using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace Mods.Pipe.Scripts
{
  public class PipeWaterFragment : IEntityPanelFragment
  {
    private static readonly string DepthLocKey = "Building.Pipe.Depth";

    private readonly VisualElementLoader _visualElementLoader;
    private readonly ILoc _loc;

    private PipeWaterLink _pipeWaterLink;
    private VisualElement _root;
    private Label _depthLabel;

    public PipeWaterFragment(
      VisualElementLoader visualElementLoader,
      ILoc loc,
      DebugFragmentFactory debugFragmentFactory
    )
    {
      _visualElementLoader = visualElementLoader;
      _loc = loc;
    }

    public VisualElement InitializeFragment()
    {
      _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/StreamGaugeFragment");
      _root.ToggleDisplayStyle(visible: false);
      _depthLabel = _root.Q<Label>("DepthLabel");
      /*_root = new VisualElement();
      var box = new Box();
      _depthLabel = new Label();
      box.Add(_depthLabel);
      _root.Add(box);*/
      return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
      _pipeWaterLink = entity.GetComponentFast<PipeWaterLink>();
    }

    public void ClearFragment()
    {
      _root.ToggleDisplayStyle(false);
      _pipeWaterLink = null;
    }

    public void UpdateFragment()
    {
      if (!_pipeWaterLink)
      {
        return;
      }
      _root.ToggleDisplayStyle(true);
      _depthLabel.text = _loc.T(_pipeWaterLink.GetInfo());
    }
  }
}
