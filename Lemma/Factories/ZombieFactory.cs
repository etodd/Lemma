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
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;
using BEPUphysics.NarrowPhaseSystems.Pairs;

namespace Lemma.Factories
{
	public class ZombieFactory : Factory
	{
		public override Entity Create(Main main)
		{
			Entity result = Factory.Get<DynamicMapFactory>().Create(main);
			result.Type = "Zombie";
			result.ID = Entity.GenerateID(result, main);

			result.Add("Damage", new Property<float> { Editable = true, Value = 0.1f });
			result.Add("VisibilityCheckInterval", new Property<float> { Editable = true, Value = 1.0f });
			result.Add("TorqueMultiplier", new Property<float> { Editable = true, Value = 0.2f });
			result.Add("MaxSpeed", new Property<float> { Editable = true, Value = 10.0f });
			result.Add("PlayerPositionMemoryTime", new Property<float> { Editable = true, Value = 4.0f });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Factory.Get<DynamicMapFactory>().Bind(result, main, creating);

			Transform transform = result.Get<Transform>();
			DynamicMap map = result.Get<DynamicMap>();

			Sound zombieSound = new Sound();
			zombieSound.Serialize = false;
			zombieSound.Cue.Value = "Zombie";
			result.Add("ZombieSound", zombieSound);

			Property<bool> playerVisible = new Property<bool> { Value = false, Editable = false, Serialize = false };
			result.Add("PlayerVisible", playerVisible);

			zombieSound.Add(new Binding<Vector3>(zombieSound.Position, transform.Position));
			zombieSound.Add(new Binding<Vector3>(zombieSound.Velocity, map.LinearVelocity));

			Property<float> damage = result.GetProperty<float>("Damage");

			map.Add(new CommandBinding<Collidable, ContactCollection>(map.Collided, delegate(Collidable collidable, ContactCollection contact)
			{
				if (result.Active && collidable is EntityCollidable)
				{
					if (((EntityCollidable)collidable).Entity.Tag is Player)
					{
						Player player = (Player)((EntityCollidable)collidable).Entity.Tag;
						player.Health.Value -= damage;
					}
				}
			}));

			Property<float> zombieSoundPitch = zombieSound.GetProperty("Pitch");

			Property<float> visibilityCheckInterval = result.GetProperty<float>("VisibilityCheckInterval");
			Property<float> torqueMultiplier = result.GetProperty<float>("TorqueMultiplier");
			Property<float> maxSpeed = result.GetProperty<float>("MaxSpeed");
			Property<float> playerPositionMemoryTime = result.GetProperty<float>("PlayerPositionMemoryTime");

			float timeSinceLastSpottedPlayer = playerPositionMemoryTime;
			float timeSinceLastVisibilityCheck = 0.0f;

			result.Add(new Updater
			{
				delegate(float dt)
				{
					if (!result.Active)
						return;

					Entity player = main.Get("Player").FirstOrDefault();
					if (player != null)
					{
						Vector3 playerPosition = player.Get<Transform>().Position.Value;

						Vector3 rayDirection = playerPosition - transform.Position;

						float playerDistance = rayDirection.Length();

						rayDirection /= playerDistance;

						timeSinceLastVisibilityCheck += dt;
						if (timeSinceLastVisibilityCheck > visibilityCheckInterval)
						{
							Map.GlobalRaycastResult hit = Map.GlobalRaycast(playerPosition + (rayDirection * -3.0f), -rayDirection, playerDistance);
							if (hit.Map == map)
							{
								timeSinceLastSpottedPlayer = 0.0f;
								playerVisible.Value = true;
							}
							timeSinceLastVisibilityCheck = 0.0f;
						}
						timeSinceLastSpottedPlayer += dt;

						if (timeSinceLastSpottedPlayer < playerPositionMemoryTime)
						{
							float torque = torqueMultiplier * map.PhysicsEntity.Mass * dt;
							Vector3 impulse = new Vector3(torque * rayDirection.Z, 0.0f, -torque * rayDirection.X);
							map.PhysicsEntity.ApplyAngularImpulse(ref impulse);
							Vector3 velocity = map.PhysicsEntity.AngularVelocity;
							float speed = velocity.Length();
							if (speed > maxSpeed)
								map.PhysicsEntity.AngularVelocity = velocity * (maxSpeed / speed);

							map.PhysicsEntity.ActivityInformation.Activate();
							zombieSoundPitch.Value = ((speed / maxSpeed) / torque) - 1.0f;
						}
						else if (playerVisible)
							playerVisible.Value = false;
					}

					if (timeSinceLastSpottedPlayer > playerPositionMemoryTime && zombieSound.IsPlaying)
						zombieSound.Stop.Execute(Microsoft.Xna.Framework.Audio.AudioStopOptions.AsAuthored);
					else if (timeSinceLastSpottedPlayer < playerPositionMemoryTime && !zombieSound.IsPlaying)
						zombieSound.Play.Execute();
				}
			});
		}
	}
}
