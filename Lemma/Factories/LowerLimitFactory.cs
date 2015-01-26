using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class LowerLimitFactory : Factory<Main>
	{
		private List<Property<Vector3>> positions = new List<Property<Vector3>>();

		public LowerLimitFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "LowerLimit");
		}

		const float absoluteLimit = -20.0f;
		const float velocityThreshold = -40.0f;

		public float GetLowerLimit()
		{
			float limit = float.MinValue;
			foreach (Property<Vector3> pos in this.positions)
				limit = Math.Max(limit, pos.Value.Y);
			return limit;
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			this.positions.Add(transform.Position);
			transform.Add(new CommandBinding(transform.Delete, delegate()
			{
				this.positions.Remove(transform.Position);
			}));

			entity.CannotSuspendByDistance = true;
			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);

			this.SetMain(entity, main);
			
			transform.EditorProperties();
			attachable.EditorProperties();

			entity.Add(new Updater
			(
				delegate(float dt)
				{
					Entity player = PlayerFactory.Instance;
					if (player != null && player.Active)
					{
						Player p = player.Get<Player>();
						float y = p.Character.Transform.Value.Translation.Y;
						float limit = transform.Position.Value.Y;
						if (y < limit + absoluteLimit || (y < limit && p.Character.LinearVelocity.Value.Y < velocityThreshold))
							p.Die.Execute();
					}
					else
						player = null;
				}
			));
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
