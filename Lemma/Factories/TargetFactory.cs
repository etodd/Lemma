using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public class TargetFactory : Factory<Main>
	{
		public static ListProperty<Transform> Positions = new ListProperty<Transform>();

		public TargetFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Target");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("Trigger");

			VoxelAttachable.MakeAttachable(entity, main);

			base.Bind(entity, main, creating);

			TargetFactory.Positions.Add(transform);
			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				TargetFactory.Positions.Remove(transform);
			}));

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));

			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate()
			{
				entity.Add(new Animation(new Animation.Execute(entity.Delete)));
			}));
			trigger.Add(new Binding<bool>(trigger.Enabled, transform.Enabled));
			trigger.EditorProperties();

			entity.Add("Reached", trigger.PlayerEntered);
			entity.Add("Enabled", transform.Enabled);

			entity.Add("Enable", transform.Enable);
			entity.Add("Disable", transform.Disable);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);

			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.Color.Value = this.Color;
			model.Scale.Value = new Vector3(0.5f);
			model.Serialize = false;

			entity.Add("EditorModel3", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));

			VoxelAttachable.AttachEditorComponents(entity, main);
		}
	}
}
