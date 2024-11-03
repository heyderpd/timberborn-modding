using UnityEngine;
using UnityEngine.UIElements;
using Timberborn.CoreUI;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;

namespace Mods.OldGopher.Pipe.Scripts
{
  public class PipeFragment : IEntityPanelFragment
  {
    private readonly VisualElementLoader visualElementLoader;
    private readonly ILoc loc;

    private PipeNode pipeNode;
    private VisualElement root;
    private Label text;

    public PipeFragment(
      VisualElementLoader _visualElementLoader,
      ILoc _loc
    )
    {
      visualElementLoader = _visualElementLoader;
      loc = _loc;
    }

    public VisualElement InitializeFragment()
    {
      root = new VisualElement();
      var box = new Box();
      box.style.backgroundColor = Color.white;
      box.style.width = 400;
      text = new Label();
      text.style.color = Color.black;
      box.Add(text);
      root.Add(box);
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
      text.text = pipeNode.GetFragmentInfo();
    }
  }
}
