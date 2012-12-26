using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class BlastFactory : Factory
	{
		public BlastFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Blast");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			PhysicsBlock physics = new PhysicsBlock();
			result.Add("Physics", physics);

			Model model = new Model();
			result.Add("Model", model);
			model.Filename.Value = "Models\\blast";
			model.Color.Value = new Vector3(0.75f, 2.0f, 0.75f);

			PointLight light = new PointLight();
			light.Shadowed.Value = true;
			light.Color.Value = new Vector3(model.Color.Value.X, model.Color.Value.Y, model.Color.Value.Z);
			light.Attenuation.Value = 10.0f;
			result.Add("Light", light);

			if (ParticleSystem.Get(main, "Sparks") == null)
			{
				ParticleSystem.Add(main, "Sparks",
				new ParticleSystem.ParticleSettings
				{
					TextureName = "Particles\\spark",
					MaxParticles = 1000,
					Duration = TimeSpan.FromSeconds(1.0f),
					MinHorizontalVelocity = 0.0f,
					MaxHorizontalVelocity = 0.0f,
					MinVerticalVelocity = 0.0f,
					MaxVerticalVelocity = 0.0f,
					Gravity = new Vector3(0.0f, 0.0f, 0.0f),
					EndVelocity = 0.0f,
					MinRotateSpeed = -20.0f,
					MaxRotateSpeed = 20.0f,
					MinStartSize = 0.5f,
					MaxStartSize = 0.4f,
					MinEndSize = 0.2f,
					MaxEndSize = 0.1f,
					BlendState = Microsoft.Xna.Framework.Graphics.BlendState.Additive,
					MinColor = new Vector4(0.75f, 2.0f, 0.75f, 1.0f),
					MaxColor = new Vector4(0.75f, 2.0f, 0.75f, 1.0f),
				});
			}

			ParticleEmitter emitter = new ParticleEmitter();
			emitter.ParticleType.Value = "Sparks";
			emitter.ParticlesPerSecond.Value = 200;
			emitter.Jitter.Value = new Vector3(0.25f);
			result.Add("Particles", emitter);

			Sound loopSound = new Sound();
			result.Add("LoopSound", loopSound);
			loopSound.Cue.Value = "Blast Loop";

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;
			Transform transform = result.Get<Transform>();
			PhysicsBlock physics = result.Get<PhysicsBlock>();
			Model model = result.Get<Model>();
			Sound loopSound = result.Get<Sound>("LoopSound");
			PointLight light = result.Get<PointLight>();
			ParticleEmitter emitter = result.Get<ParticleEmitter>("Particles");

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));

			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			loopSound.Add(new Binding<Vector3>(loopSound.Position, transform.Position));
			loopSound.Add(new Binding<Vector3>(loopSound.Velocity, physics.LinearVelocity));
			loopSound.Position.Value = transform.Position;

			physics.Add(new CommandBinding<Collidable, ContactCollection>(physics.Collided, delegate(Collidable collidable, ContactCollection contacts)
			{
				if (result.Active)
				{
					result.Delete.Execute();

					Sound.PlayCue(main, "Explosion", transform.Position);

					if (collidable is EntityCollidable)
					{
						if (((EntityCollidable)collidable).Entity.Tag is Map)
						{
							ContactInformation contact = contacts.First();
							Vector3 pos = contact.Contact.Position - (contact.Contact.Normal * 0.5f);

							Map map = (Map)((EntityCollidable)collidable).Entity.Tag;
							Map.Coordinate center = map.GetCoordinate(pos);
							int radius = 3;
							Random random = new Random();
							for (Map.Coordinate x = center.Move(Direction.NegativeX, radius - 1); x.X < center.X + radius; x.X++)
							{
								for (Map.Coordinate y = x.Move(Direction.NegativeY, radius - 1); y.Y < center.Y + radius; y.Y++)
								{
									for (Map.Coordinate z = y.Move(Direction.NegativeZ, radius - 1); z.Z < center.Z + radius; z.Z++)
									{
										Vector3 cellPos = map.GetAbsolutePosition(z);
										Vector3 toCell = cellPos - pos;
										if (toCell.Length() < radius - 1)
										{
											Map.CellState state = map[z];
											if (map.Empty(z))
											{
												Entity block = Factory.CreateAndBind(main, "Block");
												block.Get<Transform>().Position.Value = cellPos;
												block.Get<Transform>().Quaternion.Value = map.Entity.Get<Transform>().Quaternion;
												state.ApplyToBlock(block);
												block.Get<ModelInstance>().GetVector3Parameter("Offset").Value = map.GetRelativePosition(z);
												toCell += contact.Contact.Normal * 4.0f;
												toCell.Normalize();
												block.Get<PhysicsBlock>().LinearVelocity.Value = toCell * 15.0f;
												block.Get<PhysicsBlock>().AngularVelocity.Value = new Vector3(((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f, ((float)random.NextDouble() - 0.5f) * 2.0f);
												main.Add(block);
											}
										}
									}
								}
							}
							map.Regenerate();
						}
						else if (((EntityCollidable)collidable).Entity.Tag is Player)
						{
							Player player = (Player)((EntityCollidable)collidable).Entity.Tag;
							player.Health.Value -= 0.75f;
						}
					}
				}
			}));

			this.SetMain(result, main);
			loopSound.Play.Execute();
		}
	}
}
