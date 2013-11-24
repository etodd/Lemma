using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;
using Lemma.Util;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;

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

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Factory.Get<DynamicMapFactory>().Bind(result, main);

			Transform transform = result.Get<Transform>();
			DynamicMap map = result.Get<DynamicMap>();
			PointLight light = result.Get<PointLight>();

			Sound blastChargeSound = new Sound();
			blastChargeSound.Cue.Value = "Blast Charge";
			blastChargeSound.Serialize = false;
			result.Add("BlastChargeSound", blastChargeSound);

			Sound blastFireSound = new Sound();
			blastFireSound.Cue.Value = "Blast Fire";
			blastFireSound.Serialize = false;
			result.Add("BlastFireSound", blastFireSound);

			blastFireSound.Add(new Binding<Vector3>(blastFireSound.Position, transform.Position));
			blastFireSound.Add(new Binding<Vector3>(blastFireSound.Velocity, map.LinearVelocity));

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
				rotator.AngularMotor.Settings.Servo.MaxCorrectiveVelocity = 3.0f;
				main.Space.Add(rotator);

				mover = new EntityMover(map.PhysicsEntity);
				mover.TargetPosition = transform.Position;
				main.Space.Add(mover);
			}

			Map.Coordinate blastSource = map.GetCoordinate(0, 0, 0);
			Map.Coordinate blastPosition = blastSource;
			Map.CellState whiteMaterial = WorldFactory.StatesByName["White"];
			Map.CellState permanentWhiteMaterial = WorldFactory.StatesByName["WhitePermanent"];
			foreach (Map.Box box in map.Chunks.SelectMany(x => x.Boxes))
			{
				if (box.Type == whiteMaterial || box.Type == permanentWhiteMaterial)
				{
					blastSource = map.GetCoordinate(box.X, box.Y, box.Z);
					blastPosition = map.GetCoordinate(box.X, box.Y, box.Z - 1);
					break;
				}
			}

			LineDrawer laser = new LineDrawer { Serialize = false };
			result.Add(laser);

			const float blastIntervalTime = 3.0f;
			float blastInterval = 0.0f;

			const float playerPositionMemoryTime = 4.0f;
			float timeSinceLastSpottedPlayer = playerPositionMemoryTime;

			const float visibilityCheckInterval = 1.0f;
			float timeSinceLastVisibilityCheck = 0.0f;

			const float blastChargeTime = 1.25f;
			float blastCharge = 0.0f;

			const float playerDetectionRadius = 15.0f;

			result.Add(new CommandBinding(result.Delete, delegate()
			{
				if (rotator != null)
				{
					main.Space.Remove(rotator);
					main.Space.Remove(mover);
				}
			}));

			Property<Vector3> target = result.GetOrMakeProperty<Vector3>("Target");

			Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(0.2f, 1.0f, 0.2f, 0.7f);

			Updater update = new Updater();
			update.Add(delegate(float dt)
			{
				if (map[blastSource].ID == 0)
				{
					result.Delete.Execute();
					return;
				}

				laser.Lines.Clear();
				Entity player = PlayerFactory.Instance;
				if (player != null)
				{
					Vector3 rayStart = map.GetAbsolutePosition(blastPosition);

					target.Value += (player.Get<Transform>().Position - target.Value) * 0.5f * dt;

					Vector3 rayDirection = target - rayStart;
					rayDirection.Normalize();

					Vector3 aimDirection = map.GetAbsoluteVector(Vector3.Forward);

					Map.GlobalRaycastResult hit = Map.GlobalRaycast(rayStart, aimDirection, 300.0f);

					laser.Lines.Add(new LineDrawer.Line
					{
						A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(map.GetAbsolutePosition(blastSource), color),
						B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(hit.Position, color),
					});

					timeSinceLastVisibilityCheck += dt;
					if (timeSinceLastVisibilityCheck > visibilityCheckInterval)
					{
						if ((target.Value - transform.Position).Length() < playerDetectionRadius)
							timeSinceLastSpottedPlayer = 0.0f;
						else if (Vector3.Dot(rayDirection, aimDirection) > 0)
						{
							RayCastResult physicsHit;
							if (main.Space.RayCast(new Ray(rayStart, rayDirection), out physicsHit))
							{
								EntityCollidable collidable = physicsHit.HitObject as EntityCollidable;
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
						rotator.TargetOrientation = Quaternion.CreateFromRotationMatrix(Matrix.Invert(Matrix.CreateLookAt(rayStart, target, Vector3.Up)));
						if (blastInterval > blastIntervalTime && Vector3.Dot(rayDirection, aimDirection) > 0.9f)
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

								if (hit.Map != null)
									Explosion.Explode(main, hit.Map, hit.Coordinate.Value, 5, 8.0f);
								else
								{
									BEPUutilities.RayHit physicsHit;
									if (player.Get<Player>().Body.CollisionInformation.RayCast(new Ray(rayStart, aimDirection), hit.Distance, out physicsHit))
										Explosion.Explode(main, player.Get<Transform>().Position, 5, 8.0f);
								}
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
