using UnityEngine.UIElements;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Debugging;
using Timberborn.Localization;

namespace Mods.OldGopher.Pipe.Scripts
{
  public class PipeFragment : IEntityPanelFragment
  {
    private readonly VisualElementLoader visualElementLoader;
    private readonly ILoc loc;
    private readonly DevModeManager devModeManager;

    private PipeNode pipeNode;
    private VisualElement root;
    private Label field;
    private Button button;
    private VisualElement debugView;
    private Label debugField;
    private Button debugButton;
    private bool show = false;

    public PipeFragment(
      VisualElementLoader _visualElementLoader,
      ILoc _loc,
      DevModeManager _devModeManager
    )
    {
      visualElementLoader = _visualElementLoader;
      loc = _loc;
      devModeManager = _devModeManager;
    }

    public void ShowFragment(BaseComponent entity)
    {
      pipeNode = entity.GetComponentFast<PipeNode>();
    }

    public VisualElement InitializeFragment()
    {
      root = new VisualElement();
      root.Add(CreatePumpView());
      debugView = CreateDebugView();
      return root;
    }

    private VisualElement CreatePumpView()
    {
      var fragment = visualElementLoader.LoadVisualElement("Game/EntityPanel/StreamGaugeFragment");
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "GreatestDepthLabel", (string)null));
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "CurrentLabel", (string)null));
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "ContaminationLabel", (string)null));
      field = UQueryExtensions.Q<Label>(fragment, "DepthLabel", (string)null);
      field.text = "mode: ???";
      button = UQueryExtensions.Q<Button>(fragment, "ResetGreatestDepthButton", (string)null);
      button.text = "Toggle";
      button.RegisterCallback<ClickEvent>((EventCallback<ClickEvent>)ToggleWaterPump, (TrickleDown)0);
      return fragment;
    }

    private VisualElement CreateDebugView()
    {
      var fragment = visualElementLoader.LoadVisualElement("Game/EntityPanel/StreamGaugeFragment");
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "GreatestDepthLabel", (string)null));
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "CurrentLabel", (string)null));
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "ContaminationLabel", (string)null));
      debugField = UQueryExtensions.Q<Label>(fragment, "DepthLabel", (string)null);
      debugField.text = "no debug";
      fragment.Remove(debugField);
      var box = new ScrollView();
      box.style.height = 500;
      box.Add(debugField);
      fragment.Add(box);
      debugButton = UQueryExtensions.Q<Button>(fragment, "ResetGreatestDepthButton", (string)null);
      debugButton.text = "Test Particles";
      debugButton.RegisterCallback<ClickEvent>((EventCallback<ClickEvent>)TestParticle, (TrickleDown)0);
      return fragment;
    }

    private void ToggleWaterPump(ClickEvent evt)
    {
      if (!pipeNode.IsWaterPump)
        return;
      pipeNode?.ToggleWaterPump();
      UpdateFragment();
    }

    private void TestParticle(ClickEvent evt)
    {
      pipeNode?.TestParticle();
    }

    private void ShowPumpView()
    {
      field.text = $"mode: {pipeNode.GetWaterPumpState()}";
    }

    private void ShowDebugView()
    {
      root.Add(debugView);
      debugField.text = pipeNode.GetFragmentInfo();
    }

    private void RemoveDebugView()
    {
      if (root.Contains(debugView))
        root.Remove(debugView);
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
        root.ToggleDisplayStyle(false);
        return;
      }
      show = false;
      if (pipeNode.IsWaterPump)
      {
        ShowPumpView();
        show = true;
      }
      if (devModeManager.Enabled && ModUtils.enabled)
      {
        ShowDebugView();
        show = true;
      }
      else
        RemoveDebugView();
      root.ToggleDisplayStyle(show);
    }
  }
}
