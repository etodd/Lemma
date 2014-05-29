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
			transform.Editable = true;
			transform.Enabled.Editable = true;

			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("Trigger");

			base.Bind(entity, main, creating);

			TargetFactory.Positions.Add(transform);
			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				TargetFactory.Positions.Remove(transform);
			}));

			Property<bool> deleteWhenReached = entity.GetOrMakeProperty<bool>("DeleteWhenReached", true, true);
			trigger.Add(new TwoWayBinding<bool>(deleteWhenReached, trigger.Enabled));
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate()
			{
				entity.Add(new Animation
				(
					new Animation.Delay(0.0f),
					new Animation.Execute(entity.Delete)
				));
			}));

			if (entity.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			PlayerTrigger.AttachEditorComponents(entity, main, this.Color);

			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.Color.Value = this.Color;
			model.Scale.Value = new Vector3(0.5f);
			model.Editable = false;
			model.Serialize = false;

			entity.Add("EditorModel3", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));

			MapAttachable.AttachEditorComponents(entity, main);
		}
	}
}
