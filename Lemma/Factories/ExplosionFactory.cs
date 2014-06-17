using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public class ExplosionFactory : Factory<Main>
	{
		public ExplosionFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Explosion");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			this.SetMain(entity, main);

			VoxelAttachable.MakeAttachable(entity, main, true, false).EditorProperties();

			Explosion explosion = entity.GetOrCreate<Explosion>("Explosion");
			entity.Add(new CommandBinding(explosion.Delete, entity.Delete));

			explosion.Add(new Binding<Vector3>(explosion.Position, transform.Position));

			entity.Add("Explode", explosion.Go);
			entity.Add("DeleteAfter", explosion.DeleteAfter);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
