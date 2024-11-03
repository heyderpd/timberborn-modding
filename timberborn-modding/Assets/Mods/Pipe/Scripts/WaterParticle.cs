using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using Timberborn.CoreUI;
using Timberborn.Particles;
using Timberborn.Buildings;
using Timberborn.BaseComponentSystem;
using Timberborn.ModelAttachmentSystem;

namespace Mods.OldGopher.Pipe.Scripts
{
  internal class WaterParticle : BaseComponent
  {
    private bool initialized;

    private string attachmentId;

    private Colors colors;

    private ParticleSystem ParticleSystem;

    private MainModule particlesMainModule;

    private ParticlesRunner particlesRunner;

    private float waterPower = -1f;

    private float waterContaminated = -1f;

    public void Initialize(Colors _colors, WaterGate waterGate)
    {
      if (initialized)
        return;
      initialized = true;
      attachmentId = WaterGateConfig.getParticleAttachmentId(waterGate.Type);
      if (attachmentId == "")
        return;
      colors = _colors;
      ParticleSystem = ((Component)GetComponentFast<ModelAttachments>().GetOrCreateAttachment(attachmentId).Transform).GetComponentInChildren<ParticleSystem>(true);
      particlesMainModule = ParticleSystem.main;
      particlesRunner = GetComponentFast<BuildingParticleAttachment>().CreateParticlesRunner(new List<string> { attachmentId });
      Settup();
    }

    private void Settup()
    {
      particlesMainModule.startLifetime = new ParticleSystem.MinMaxCurve(0.3f); // Length
      particlesMainModule.maxParticles = 15;
      particlesMainModule.startSpeed = 2;
      var shape = ParticleSystem.shape;
      shape.angle = 16f;
      shape.radius = 0.11f;
      var force = ParticleSystem.forceOverLifetime;
      force.enabled = true;
      force.y = -7;
      force.space = ParticleSystemSimulationSpace.World;
    }

    private bool HasWater(WaterAddition Event)
    {
      ModUtils.Log($"[WaterParticle.HasWater] Water={Event.Water}");
      return Event.Water > 0;
    }

    private void SetColor(WaterAddition Event)
    {
      ModUtils.Log($"[WaterParticle.SetColor] init");
      if (!HasWater(Event))
        return;
      var newWaterContaminated = ModUtils.GetValueStep(Event.ContaminatedPercentage, 1.0f);
      if (newWaterContaminated == waterContaminated)
        return;
      waterContaminated = newWaterContaminated;
      MinMaxGradient startColor = particlesMainModule.startColor;
      startColor.color = colors.WaterContaminationParticleGradient.Evaluate(waterContaminated);
      particlesMainModule.startColor = startColor;
    }

    private void SetWaterFlow(WaterAddition Event)
    {
      ModUtils.Log($"[WaterParticle.SetWaterFlow] init");
      if (!HasWater(Event))
        return;
      var newWaterPower = ModUtils.GetValueStep(Event.Water, WaterService.maximumFlow);
      if (newWaterPower == waterPower)
        return;
      waterPower = newWaterPower;
      ModUtils.Log($"[WaterParticle.SetWaterFlow] waterPower={newWaterPower}");
      float particles = (waterPower * 0.2f) + 0.1f; // range of 0.1 to 0.3
      particlesMainModule.startLifetime = new ParticleSystem.MinMaxCurve(particles);
      float speed = (waterPower * 1f) + 1f; // range of 1 to 2
      particlesMainModule.startSpeed = speed;
      float angle = (waterPower * 8f) + 8f; // range of 8 to 16
      float radius = (waterPower * 0.1f) + 0.01f; // range of 0.01 to 0.11
      var shape = ParticleSystem.shape;
      shape.angle = angle;
      shape.radius = radius;
    }

    private void SetAnimation(WaterAddition Event)
    {
      if (HasWater(Event))
        particlesRunner.Play();
      else if (particlesRunner.IsPlaying)
        particlesRunner.Stop();
    }

    public void StopAnimation()
    {
      particlesRunner.Stop();
    }

    public void OnWaterAdded(object sender, WaterAddition Event)
    {
      if (!particlesRunner)
        return;
      SetColor(Event);
      SetWaterFlow(Event);
      SetAnimation(Event);
    }
  }
}
