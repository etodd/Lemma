using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;
using Lemma.Util;
using BEPUphysics;
using BEPUphysics.Collidables.MobileCollidables;

namespace Lemma.Factories
{
	public class FloaterFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = Factory.Get<DynamicMapFactory>().Create(main);
			result.Type = "Floater";

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Factory.Get<DynamicMapFactory>().Bind(result, main);

			Transform transform = result.Get<Transform>();
			DynamicMap map = result.Get<DynamicMap>();

			Random random = new Random();

			Agent agent = result.GetOrCreate<Agent>();
			agent.Add(new Binding<Vector3>(agent.Position, transform.Position));
			agent.Add(new Binding<float, Vector3>(agent.Speed, x => x.Length() * 2.0f, map.LinearVelocity));
			agent.Add(new CommandBinding(agent.Die, delegate()
			{
				PointLight explosionLight = new PointLight();
				explosionLight.Color.Value = new Vector3(1.3f, 1.1f, 0.9f);
				explosionLight.Attenuation.Value = 20.0f;
				explosionLight.Position.Value = transform.Position;
				main.AddComponent(explosionLight);
				main.AddComponent(new Animation
				(
					new Animation.FloatMoveTo(explosionLight.Attenuation, 0.0f, 1.0f),
					new Animation.Execute(explosionLight.Delete)
				));
				Sound.PlayCue(main, "InfectedShatter", transform.Position, 1.0f, 0.05f);
				ParticleSystem shatter = ParticleSystem.Get(main, "FloaterShatter");
				if (shatter == null)
				{
					shatter = ParticleSystem.Add(main, "FloaterShatter",
					new ParticleSystem.ParticleSettings
					{
						TextureName = "Particles\\spark",
						MaxParticles = 1000,
						Duration = TimeSpan.FromSeconds(3.0f),
						MinHorizontalVelocity = -8.0f,
						MaxHorizontalVelocity = 8.0f,
						MinVerticalVelocity = 0.0f,
						MaxVerticalVelocity = 10.0f,
						Gravity = new Vector3(0.0f, -8.0f, 0.0f),
						MinRotateSpeed = -2.0f,
						MaxRotateSpeed = 2.0f,
						MinStartSize = 0.5f,
						MaxStartSize = 1.0f,
						MinEndSize = 0.0f,
						MaxEndSize = 0.0f,
						BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive,
						MinColor = new Vector4(0.75f, 2.0f, 0.75f, 1.0f),
						MaxColor = new Vector4(0.75f, 2.0f, 0.75f, 1.0f),
					});
				}
				for (int i = 0; i < 20; i++)
				{
					Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
					shatter.AddParticle(transform.Position + offset * 2.0f, offset);
				}
			}));
			agent.Add(new CommandBinding(agent.Die, result.Delete));

			map.Add(new CommandBinding(map.CompletelyEmptied, () => !main.EditorEnabled, result.Delete));

			PID3 linearPid = result.GetOrCreate<PID3>("LinearPID");
			linearPid.Add(new Binding<Vector3>(linearPid.Input, transform.Position));
			linearPid.Target.Value = transform.Position;
			linearPid.Add(new NotifyBinding(delegate()
			{
				Vector3 grabPoint = map.PhysicsEntity.Position;
				Vector3 diff = linearPid.Output;
				float force = diff.Length();
				if (force > 5.0f)
					diff *= 5.0f / force;
				map.PhysicsEntity.ApplyImpulse(ref grabPoint, ref diff);
			}, linearPid.Output));

			AI ai = result.GetOrCreate<AI>("AI");

			PointLight light = result.GetOrCreate<PointLight>();
			light.Shadowed.Value = false;
			light.Color.Value = new Vector3(0.75f, 2.0f, 0.75f);
			light.Attenuation.Value = 10.0f;
			light.Editable = false;
			light.Serialize = false;
			light.Add(new Binding<Vector3>(light.Position, transform.Position));
			light.Add(new Binding<Vector3, string>(light.Color, delegate(string value)
			{
				switch (value)
				{
					default:
						return new Vector3(0.75f, 2.0f, 0.75f);
				}
			}, ai.CurrentState));

			Property<Vector3> longTermDirection = result.GetOrMakeProperty<Vector3>("LongTermDirection");

			Property<Vector3> targetStart = result.GetOrMakeProperty<Vector3>("TargetStart");

			Property<Vector3> target = result.GetOrMakeProperty<Vector3>("Target");

			Property<float> targetBlend = result.GetOrMakeProperty<float>("TargetBlend");

			const float targetInterval = 1.0f;
			const float targetDistance = 2.0f;

			Vector3[] rays = new[]
			{
				new Vector3(0.0f, 0.0f, 1.0f),
				new Vector3(0.0f, 0.0f, -1.0f),
				new Vector3(0.0f, 1.0f, 0.0f),
				new Vector3(0.0f, -1.0f, 0.0f),
				new Vector3(1.0f, 0.0f, 0.0f),
				new Vector3(-1.0f, 0.0f, 0.0f),
			};

			Action longTerm = delegate()
			{
				if (random.NextDouble() > 0.5f)
				{
					float longestDistance = 0.0f;
					Vector3 longestRay = Vector3.Zero;
					foreach (Vector3 ray in rays)
					{
						Map.GlobalRaycastResult hit = Map.GlobalRaycast(transform.Position, ray, 50.0f, x => x != map);
						if (hit.Map != null && hit.Distance > longestDistance)
						{
							longestDistance = hit.Distance;
							longestRay = ray;
						}
					}
					longTermDirection.Value = Vector3.Normalize(longestRay);
				}
				else
				{
					float distance = 0.0f;
					Vector3 ray = Vector3.Zero;
					while (distance < 5.0f)
					{
						ray = Vector3.Normalize(new Vector3(((float)random.NextDouble() * 2.0f) - 1.0f, ((float)random.NextDouble() * 2.0f) - 1.0f, ((float)random.NextDouble() * 2.0f) - 1.0f));
						Map.GlobalRaycastResult hit = Map.GlobalRaycast(transform.Position, ray, 50.0f, x => x != map);
						if (hit.Map != null)
							distance = hit.Distance;
					}
					longTermDirection.Value = ray;
				}
			};

			Action shortTerm = delegate()
			{
				const float avoidanceDistance = 10.0f;
				Vector3 velocity = Vector3.Zero;
				foreach (Vector3 ray in rays)
				{
					Map.GlobalRaycastResult hit = Map.GlobalRaycast(transform.Position, ray, avoidanceDistance, x => x != map);
					if (hit.Map != null)
						velocity -= Vector3.Normalize(ray) * (avoidanceDistance - hit.Distance);
				}
				targetStart.Value = transform.Position;
				target.Value = transform.Position + (Vector3.Normalize(longTermDirection + (velocity * 0.5f)) * targetDistance);
				targetBlend.Value = 0.0f;
			};

			ai.Setup
			(
				new AI.State
				{
					Name = "Idle",
					Enter = delegate(AI.State lastState) { longTerm();  shortTerm(); },
					Tasks = new[]
					{
						new AI.Task
						{
							Interval = 10.0f,
							Action = longTerm,
						},
						new AI.Task
						{
							Interval = targetInterval,
							Action = shortTerm,
						},
						new AI.Task
						{
							Action = delegate()
							{
								targetBlend.Value += main.ElapsedTime / targetInterval;
								linearPid.Target.Value = Vector3.Lerp(targetStart, target, targetBlend);
							},
						},
					},
				}
			);
		}
	}
}
