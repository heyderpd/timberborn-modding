using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace Mods.Pipe.Scripts
{
  public class PipeFragment : IEntityPanelFragment
  {
    private static readonly string DepthLocKey = "Building.Pipe.Depth";

    private readonly VisualElementLoader _visualElementLoader;
    private readonly ILoc _loc;

    private PipeNode pipeNode;
    private VisualElement root;
    private Label depthLabel;

    public PipeFragment(
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
      root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/StreamGaugeFragment");
      root.ToggleDisplayStyle(visible: false);
      depthLabel = root.Q<Label>("DepthLabel");
      depthLabel.text = "";
      (root.Q<Label>("GreatestDepthLabel")).text = "";
      (root.Q<Label>("CurrentLabel")).text = "";
      (root.Q<Label>("ContaminationLabel")).text = "";
      return root;
    }

    public void ShowFragment(BaseComponent entity)
    {
      pipeNode = entity.GetComponentFast<PipeNode>();
    }

    public void ClearFragment()
    {
      root.ToggleDisplayStyle(false);
      pipeNode = null;
    }

    public void UpdateFragment()
    {
      if (!pipeNode)
      {
        return;
      }
      root.ToggleDisplayStyle(true);
      depthLabel.text = pipeNode.GetFragmentInfo();
      //depthLabel.text = _loc.T(pipeNode.GetFragmentInfo());
    }
  }
}
