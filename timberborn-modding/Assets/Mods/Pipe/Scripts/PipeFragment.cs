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

    private static readonly string TakeToPipeLocKey = "Building.PipeWaterPump.TakeToPipe";
    private static readonly string TakeFromPipeLocKey = "Building.PipeWaterPump.TakeFromPipe";

    private PipeNode pipeNode;
    private VisualElement root;
    private Label field;
    private Button button;
    private VisualElement debugView;
    private Label debugField;
    private Button debugButton;

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
      root.Add(CreateDebugView());
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
      button.RegisterCallback<ClickEvent>(ToggleWaterPump, (TrickleDown)0);
      return fragment;
    }

    private VisualElement CreateDebugView()
    {
      var fragment = visualElementLoader.LoadVisualElement("Game/EntityPanel/StreamGaugeFragment");
      fragment.ToggleDisplayStyle(false);
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "GreatestDepthLabel", (string)null));
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "CurrentLabel", (string)null));
      fragment.Remove(UQueryExtensions.Q<Label>(fragment, "ContaminationLabel", (string)null));
      debugField = UQueryExtensions.Q<Label>(fragment, "DepthLabel", (string)null);
      debugField.text = "no debug";
      debugButton = UQueryExtensions.Q<Button>(fragment, "ResetGreatestDepthButton", (string)null);
      debugButton.text = "Test Particles";
      debugButton.RegisterCallback<ClickEvent>(TestParticle, (TrickleDown)0);
      fragment.Remove(debugField);
      var box = new ScrollView();
      box.style.height = 500;
      box.Add(debugField);
      fragment.Add(box);
      debugView = fragment;
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
      var state = pipeNode.GetWaterPumpState();
      if (state == null)
      {
        field.text = "none";
        return;
      }
      field.text = state ?? false
        ? loc.T(TakeToPipeLocKey)
        : loc.T(TakeFromPipeLocKey);
    }

    private void ShowDebugView()
    {
      debugView.ToggleDisplayStyle(true);
      debugField.text = pipeNode.GetFragmentInfo();
    }

    private void RemoveDebugView()
    {
      debugView.ToggleDisplayStyle(false);
    }

    public void ClearFragment()
    {
      root.ToggleDisplayStyle(false);
      pipeNode = null;
    }

    public void UpdateFragment()
    {
      root.ToggleDisplayStyle(false);
      if (!pipeNode)
        return;
      if (pipeNode.IsWaterPump)
      {
        ShowPumpView();
        root.ToggleDisplayStyle(true);
      }
      if (devModeManager.Enabled && ModUtils.enabled)
      {
        ShowDebugView();
        root.ToggleDisplayStyle(true);
      }
      else
        RemoveDebugView();
    }
  }
}
