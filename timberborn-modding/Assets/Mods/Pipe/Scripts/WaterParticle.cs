using System.Collections.Generic;
using Bindito.Core;
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
    private string attachmentId;

    private Colors colors;

    private ParticleSystem ParticleSystem;

    private MainModule particlesMainModule;

    private ParticlesRunner particlesRunner;

    private float waterPower = 0f;

    private float waterStep = 0.2f;

    public void Initialize(Colors _colors, WaterGate waterGate)
    {
      if (attachmentId != null)
        return;
      colors = _colors;
      attachmentId = WaterGateConfig.getParticleAttachmentId(waterGate.Side);
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

    private void SetWaterFlow(WaterAddition Event)
    {
      if (!HasWater(Event))
        return;
      var maxWater = 1f;
      var waterPower = Event.Water > maxWater
        ? maxWater
        : Event.Water > 0
          ? Event.Water
          : 0;
      waterPower = waterPower / maxWater;
      waterPower = waterPower > 0.95f ? 1f : waterPower;
      int steps = (int)(waterPower / waterStep);
      float newWaterPower = waterStep * steps;
      ModUtils.Log($"[Particle.SetWaterFlow] waterPower={waterPower} steps={steps} newWaterPower={newWaterPower}");
      if (newWaterPower == waterPower)
        return;
      waterPower = newWaterPower;
      float particles = (waterPower * 0.2f) + 0.1f; // 0.1 to 0.3
      particlesMainModule.startLifetime = new ParticleSystem.MinMaxCurve(particles);
      float speed = (waterPower * 1f) + 1f; // 1 to 2
      particlesMainModule.startSpeed = speed;
      float angle = (waterPower * 8f) + 8f; // 8 to 16
      float radius = (waterPower * 0.1f) + 0.01f; // 0.01 to 0.11
      var shape = ParticleSystem.shape;
      shape.angle = angle;
      shape.radius = radius;
    }

    private bool HasWater(WaterAddition Event)
    {
      return Event.Water > 0;
    }

    private void SetColor(WaterAddition Event)
    {
      if (!HasWater(Event))
        return;
      MinMaxGradient startColor = particlesMainModule.startColor;
      startColor.color = colors.WaterContaminationParticleGradient.Evaluate(Event.ContaminatedPercentage);
      particlesMainModule.startColor = startColor;
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
