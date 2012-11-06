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
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class PlatformFactory : DynamicMapFactory
	{
		public override Entity Create(Main main)
		{
			Entity result = base.Create(main);
			result.Type = "Platform";
			result.ID = Entity.GenerateID(result, main);

			result.Add("Limit 1", new Property<Entity.Handle> { Editable = true });
			result.Add("Limit 2", new Property<Entity.Handle> { Editable = true });
			result.Add("Direction", new Property<Direction> { Editable = true, Value = Direction.PositiveX });
			result.Add("Speed", new Property<float> { Editable = true, Value = 1.0f });
			result.Add("Enabled", new Property<bool> { Editable = true, Value = true });
			result.Add("StopOnEnd", new Property<bool> { Editable = true, Value = false });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			base.Bind(result, main);

			Transform transform = result.Get<Transform>();
			DynamicMap map = result.Get<DynamicMap>();

			Direction initialDirection = result.GetProperty<Direction>("Direction");
			Direction dir = initialDirection;

			Entity.Handle limit1 = result.GetProperty<Entity.Handle>("Limit 1");
			Entity.Handle limit2 = result.GetProperty<Entity.Handle>("Limit 2");

			Property<bool> isAtStart = new Property<bool> { Editable = false, Serialize = false };
			Property<bool> isAtEnd = new Property<bool> { Editable = false, Serialize = false };
			result.Add("IsAtStart", isAtStart);
			result.Add("IsAtEnd", isAtEnd);

			EntityMover mover = null;
			EntityRotator rotator = null;
			if (!main.EditorEnabled)
			{
				mover = new EntityMover(map.PhysicsEntity);
				mover.TargetPosition = transform.Position;
				main.Space.Add(mover);
				rotator = new EntityRotator(map.PhysicsEntity);
				rotator.TargetOrientation = transform.Quaternion;
				main.Space.Add(rotator);
			}

			Vector3 targetPosition = transform.Position;

			Property<float> speed = result.GetProperty<float>("Speed");
			Property<bool> stopOnEnd = result.GetProperty<bool>("StopOnEnd");

			Updater update = null;
			update = new Updater
			{
				delegate(float dt)
				{
					if (!result.Active || limit1.Target == null || limit2.Target == null)
						return;

					float currentLocation = targetPosition.GetComponent(dir);

					targetPosition = targetPosition.SetComponent(dir, currentLocation + dt * speed);

					mover.TargetPosition = targetPosition;

					float limit1Location = limit1.Target.Get<Transform>().Position.Value.GetComponent(dir);
					float limit2Location = limit2.Target.Get<Transform>().Position.Value.GetComponent(dir);
					float limitLocation = Math.Max(limit1Location, limit2Location);
					if (currentLocation > limitLocation)
					{
						dir = dir.GetReverse();
						if (limitLocation == limit1Location)
						{
							isAtStart.Value = true;
							isAtEnd.Value = false;
						}
						else
						{
							isAtStart.Value = false;
							isAtEnd.Value = true;
						}
						if (stopOnEnd)
							update.Enabled.Value = false;
					}
				}
			};
			update.Add(new TwoWayBinding<bool>(result.GetProperty<bool>("Enabled"), update.Enabled));

			result.Add(update);
		}

		private static Matrix[] rotationMatrices = new[]
		{
			Matrix.CreateRotationY((float)Math.PI * 0.5f), // PositiveX
			Matrix.CreateRotationY((float)Math.PI * -0.5f), // NegativeX
			Matrix.CreateRotationX((float)Math.PI * -0.5f), // PositiveY
			Matrix.CreateRotationX((float)Math.PI * 0.5f), // NegativeY
			Matrix.Identity, // PositiveZ
			Matrix.CreateRotationY((float)Math.PI), // NegativeZ
			Matrix.Identity, // None
		};

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Model model = new Model();
			model.Filename.Value = "Models\\light";
			model.Color.Value = this.Color;
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel", model);

			Transform transform = result.Get<Transform>();
			Property<Direction> dir = result.GetProperty<Direction>("Direction");

			model.Add(new Binding<Matrix>(model.Transform, delegate()
			{
				Vector3 pos = transform.Position;
				return PlatformFactory.rotationMatrices[(int)dir.Value] * Matrix.CreateTranslation(pos);
			}, transform.Position, dir));
		}
	}
}
