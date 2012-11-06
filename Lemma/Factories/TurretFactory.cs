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
	public class TurretFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = Factory.Get<DynamicMapFactory>().Create(main);
			result.Type = "Turret";

			PointLight light = new PointLight();
			light.Shadowed.Value = true;
			light.Color.Value = new Vector3(0.75f, 2.0f, 0.75f);
			light.Attenuation.Value = 0.0f;
			light.Editable = false;
			result.Add("Light", light);

			Sound blastChargeSound = new Sound();
			blastChargeSound.Cue.Value = "Blast Charge";
			result.Add("BlastChargeSound", blastChargeSound);

			Sound blastFireSound = new Sound();
			blastFireSound.Cue.Value = "Blast Fire";
			result.Add("BlastFireSound", blastFireSound);

			result.Add("Damage", new Property<float> { Editable = true, Value = 0.1f });
			result.Add("VisibilityCheckInterval", new Property<float> { Editable = true, Value = 1.0f });
			result.Add("BlastChargeTime", new Property<float> { Editable = true, Value = 1.25f });
			result.Add("BlastInterval", new Property<float> { Editable = true, Value = 1.0f });
			result.Add("PlayerPositionMemoryTime", new Property<float> { Editable = true, Value = 4.0f });
			result.Add("BlastSpeed", new Property<float> { Editable = true, Value = 75.0f });
			result.Add("PlayerDetectionRadius", new Property<float> { Editable = true, Value = 15.0f });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Factory.Get<DynamicMapFactory>().Bind(result, main);

			Transform transform = result.Get<Transform>();
			DynamicMap map = result.Get<DynamicMap>();
			PointLight light = result.Get<PointLight>();

			Sound blastFireSound = result.Get<Sound>("BlastFireSound");
			blastFireSound.Add(new Binding<Vector3>(blastFireSound.Position, transform.Position));
			blastFireSound.Add(new Binding<Vector3>(blastFireSound.Velocity, map.LinearVelocity));

			Sound blastChargeSound = result.Get<Sound>("BlastChargeSound");
			blastChargeSound.Add(new Binding<Vector3>(blastChargeSound.Position, transform.Position));
			blastChargeSound.Add(new Binding<Vector3>(blastChargeSound.Velocity, map.LinearVelocity));

			map.Add(new CommandBinding(map.CompletelyEmptied, delegate()
			{
				if (!main.EditorEnabled)
					result.Delete.Execute();
			}));

			EntityRotator rotator = null;
			EntityMover mover = null;
			if (!main.EditorEnabled)
			{
				rotator = new EntityRotator(map.PhysicsEntity);
				main.Space.Add(rotator);

				mover = new EntityMover(map.PhysicsEntity);
				mover.TargetPosition = transform.Position;
				main.Space.Add(mover);
			}

			Map.Coordinate blastSource = map.GetCoordinate(0, 0, 0);
			Map.Coordinate blastPosition = blastSource;
			Map.CellState criticalMaterial = WorldFactory.StatesByName["Critical"];
			foreach (Map.Box box in map.Chunks.SelectMany(x => x.Boxes))
			{
				if (box.Type == criticalMaterial)
				{
					blastSource = map.GetCoordinate(box.X, box.Y, box.Z);
					blastPosition = map.GetCoordinate(box.X, box.Y, box.Z - 3);
					break;
				}
			}

			Property<float> blastIntervalTime = result.GetProperty<float>("BlastInterval");
			float blastInterval = 0.0f;

			Property<float> playerPositionMemoryTime = result.GetProperty<float>("PlayerPositionMemoryTime");
			float timeSinceLastSpottedPlayer = playerPositionMemoryTime;

			Property<float> visibilityCheckInterval = result.GetProperty<float>("VisibilityCheckInterval");
			float timeSinceLastVisibilityCheck = 0.0f;

			Property<float> blastChargeTime = result.GetProperty<float>("BlastChargeTime");
			float blastCharge = 0.0f;

			Property<float> blastSpeed = result.GetProperty<float>("BlastSpeed");
			Property<float> playerDetectionRadius = result.GetProperty<float>("PlayerDetectionRadius");

			Updater update = new Updater();
			update.Add(delegate(float dt)
				{
					if (map[blastSource].ID == 0)
					{
						update.Delete.Execute();
						if (rotator != null)
						{
							main.Space.Remove(rotator);
							main.Space.Remove(mover);
						}
						light.Delete.Execute();
						return;
					}
					Entity player = main.Get("Player").FirstOrDefault();
					if (player != null)
					{
						Vector3 playerPosition = player.Get<Transform>().Position.Value;

						Vector3 rayStart = map.GetAbsolutePosition(blastPosition);

						Vector3 rayDirection = playerPosition - rayStart;
						rayDirection.Normalize();

						timeSinceLastVisibilityCheck += dt;
						if (timeSinceLastVisibilityCheck > visibilityCheckInterval)
						{
							if ((playerPosition - transform.Position).Length() < playerDetectionRadius)
								timeSinceLastSpottedPlayer = 0.0f;
							else if (Vector3.Dot(rayDirection, map.GetAbsoluteVector(Vector3.Forward)) > 0)
							{
								RayCastResult hit;
								if (main.Space.RayCast(new Ray(rayStart, rayDirection), out hit))
								{
									EntityCollidable collidable = hit.HitObject as EntityCollidable;
									if (collidable != null && collidable.Entity.Tag is Player)
										timeSinceLastSpottedPlayer = 0.0f;
								}
							}
							timeSinceLastVisibilityCheck = 0.0f;
						}
						timeSinceLastSpottedPlayer += dt;

						light.Attenuation.Value = 0.0f;
						if (timeSinceLastSpottedPlayer < playerPositionMemoryTime)
						{
							rotator.TargetOrientation = Quaternion.CreateFromRotationMatrix(Matrix.Invert(Matrix.CreateLookAt(rayStart, playerPosition, Vector3.Up)));
							if (blastInterval > blastIntervalTime)
							{
								if (blastCharge < blastChargeTime)
								{
									if (blastCharge == 0.0f)
										blastChargeSound.Play.Execute();
									blastCharge += dt;
									light.Position.Value = rayStart;
									light.Attenuation.Value = (blastCharge / blastChargeTime) * 30.0f;
								}
								else
								{
									blastCharge = 0.0f;
									blastFireSound.Play.Execute();
									blastInterval = 0.0f;
									Entity blast = Factory.CreateAndBind(main, "Blast");

									PhysicsBlock physics = blast.Get<PhysicsBlock>();
									Transform blastTransform = blast.Get<Transform>();
									blastTransform.Position.Value = rayStart;
									physics.LinearVelocity.Value = (rayDirection * blastSpeed) + new Vector3(0.0f, 6.0f, 0.0f);
									main.Add(blast);
								}
							}
							else
							{
								blastInterval += dt;
								blastCharge = 0.0f;
							}
						}
						else
							blastCharge = 0.0f;
					}
				});
			result.Add("Update", update);
		}
	}
}
