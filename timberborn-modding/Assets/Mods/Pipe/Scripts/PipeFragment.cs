using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Timberborn.CoreUI;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.Debugging;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal struct GateVisualElement
  {
    public readonly WaterGate gate;

    public string PipeId => gate?.pipeNode.id.ToString() ?? "";

    public string Id => gate?.id.ToString() ?? "";

    public string WaterLevel => gate?.WaterLevel.ToString("0.00") ?? "";

    public string WaterDetected => gate?.Water.ToString("0.00") ?? "";

    public string WaterContamination => gate?.ContaminationPercentage.ToString("0.00") ?? "";

    public string WaterChange => gate?.DesiredWater.ToString("0.00") ?? "";

    public bool StopWhenSubmerged => gate?.StopWhenSubmerged ?? false;

    public string Side => gate?.Side.ToString() ?? "";

    public string Type => gate?.Type.ToString() ?? "";

    public GateVisualElement(
      WaterGate _gate
    )
    {
      gate = _gate;
    }
  }

  internal struct PipeVisualElement
  {
    public readonly PipeNode pipe;

    public string Id => pipe?.id.ToString() ?? "";

    public PipeVisualElement(
      PipeNode _pipe
    )
    {
      pipe = _pipe;
    }
  }

  internal struct GroupVisualElement
  {
    public readonly PipeVisualElement pipe;

    public readonly List<GateVisualElement> pipeGates;

    public readonly List<GateVisualElement> groupGates;

    public GroupVisualElement(
      PipeNode _pipe
    )
    {
      pipe = new PipeVisualElement(_pipe);
      pipeGates = _pipe.waterGates.Select((WaterGate gate) => new GateVisualElement(gate)).ToList();
      groupGates = _pipe.group?.WaterGates.Select((WaterGate gate) => new GateVisualElement(gate)).ToList();
    }
  }

  public class PipeFragment : IEntityPanelFragment
  {
    private readonly VisualElementLoader visualElementLoader;
    private readonly ILoc loc;
    private readonly DevModeManager devModeManager;

    private PipeNode pipeNode;
    private GroupVisualElement pipeNodeVisual;

    private VisualElement root;
    private Label text;
    private TextField field;
    private Action<float> setter;
    private Func<float> getter;
    private Func<bool> focused;

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

    public VisualElement InitializeFragment()
    {
      root = visualElementLoader.LoadVisualElement("Common/EntityPanel/WaterSourceFragment");
      return root;

      /*VisualElement item = visualElementLoader.LoadVisualElement("Common/EntityPanel/WaterSetting");

      setter = value => pipeNode.SetGateValue(value);
      getter = () => pipeNode.GetGateValue();
      ((TextElement)UQueryExtensions.Q<Label>(item, "Text", (string)null)).text = "Item01";
      field = UQueryExtensions.Q<TextField>(item, "Value", (string)null);

      INotifyValueChangedExtensions.RegisterValueChangedCallback<string>((INotifyValueChanged<string>)(object)field, (EventCallback<ChangeEvent<string>>)delegate (ChangeEvent<string> value)
      {
        if (float.TryParse(value.newValue, out var result))
        {
          setter(result);
          ((BaseField<string>)(object)field).SetValueWithoutNotify(getter().ToString(CultureInfo.InvariantCulture));
        }
      });
      focused = () => field.IsFocused();

      text = new Label();

      root.Add(text);
      root.Add(item);
      return root;*/
    }

    private void MountItemProp(VisualElement box, string name, string value)
    {
      VisualElement item = visualElementLoader.LoadVisualElement("Common/EntityPanel/WaterSetting");
      UQueryExtensions.Q<Label>(item, "Text", (string)null).text = name;
      UQueryExtensions.Q<TextField>(item, "Value", (string)null).SetValueWithoutNotify(value);
      box.Add(item);
    }

    private void MountItem(GateVisualElement gate)
    {
      var box = new Box();
      var gateId = new Label();
      gateId.text = $"gate.id={gate.Id}";
      box.Add(gateId);
      MountItemProp(box, "Type", gate.Type);
      MountItemProp(box, "WaterLevel", gate.WaterLevel);
      MountItemProp(box, "WaterChange", gate.WaterChange);
      root.Add(box);
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

    public bool ProcessInput()
    {
      return focused();
    }

    public void UpdateInfo()
    {
      root.Clear();
      pipeNodeVisual.pipeGates.ForEach((GateVisualElement gate) => MountItem(gate));
      //pipeNodeVisual.groupGates.ForEach((GateVisualElement gate) => MountItem(gate));
    }

    public void UpdateFragment()
    {
      if (!pipeNode || !devModeManager.Enabled)
      {
        root.ToggleDisplayStyle(false);
        return;
      }
      /*((VisualElement)field).parent.ToggleDisplayStyle(true);
      if (focused())
      {
        ((BaseField<string>)(object)field).SetValueWithoutNotify(getter().ToString(CultureInfo.InvariantCulture));
      }*/
      root.ToggleDisplayStyle(true);
      pipeNodeVisual = new GroupVisualElement(pipeNode);
      UpdateInfo();
    }
  }
}
