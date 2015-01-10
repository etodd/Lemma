using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class FallingTowerFactory : Factory<Main>
	{
		public override Entity Create(Main main)
		{
			return new Entity(main, "FallingTower");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			EnemyBase enemy = entity.GetOrCreate<EnemyBase>("Base");
			PlayerCylinderTrigger trigger = entity.GetOrCreate<PlayerCylinderTrigger>("Trigger");

			PointLight light = entity.GetOrCreate<PointLight>();
			light.Color.Value = new Vector3(1.3f, 0.5f, 0.5f);
			light.Attenuation.Value = 15.0f;
			light.Serialize = false;

			FallingTower fallingTower = entity.GetOrCreate<FallingTower>("FallingTower");
			fallingTower.Add(new CommandBinding(fallingTower.Delete, entity.Delete));
			fallingTower.Add(new Binding<Entity.Handle>(fallingTower.TargetVoxel, enemy.Voxel));
			fallingTower.Add(new Binding<bool>(fallingTower.IsTriggered, trigger.IsTriggered));
			fallingTower.Base = enemy;

			enemy.Add(new CommandBinding(enemy.Delete, entity.Delete));
			enemy.Add(new Binding<Matrix>(enemy.Transform, transform.Matrix));
			light.Add(new Binding<Vector3>(light.Position, enemy.Position));

			trigger.Add(new Binding<Matrix>(trigger.Transform, () => Matrix.CreateTranslation(0.0f, 0.0f, enemy.Offset) * transform.Matrix, transform.Matrix, enemy.Offset));

			entity.Add(new CommandBinding(trigger.PlayerEntered, fallingTower.Fall));

			this.SetMain(entity, main);

			entity.Add("Fall", fallingTower.Fall);
			trigger.EditorProperties();
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			EnemyBase.AttachEditorComponents(entity, main, this.Color);

			PlayerCylinderTrigger.AttachEditorComponents(entity, main, this.Color);
		}
	}
}