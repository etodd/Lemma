using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public class TargetFactory : Factory
	{
		public static ListProperty<Transform> Positions = new ListProperty<Transform>();

		public TargetFactory()
		{
			this.Color = new Vector3(1.0f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Target");

			result.Add("Transform", new Transform());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			PlayerTrigger trigger = result.GetOrCreate<PlayerTrigger>("Trigger");

			base.Bind(result, main, creating);

			Transform transform = result.Get<Transform>();

			TargetFactory.Positions.Add(transform);
			result.Add(new CommandBinding(result.Delete, delegate()
			{
				TargetFactory.Positions.Remove(transform);
			}));

			Property<bool> deleteWhenReached = result.GetOrMakeProperty<bool>("DeleteWhenReached", true, true);
			trigger.Add(new Binding<bool>(trigger.Enabled, deleteWhenReached));
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity p)
			{
				result.Delete.Execute();
			}));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			PlayerTrigger.AttachEditorComponents(result, main, this.Color);

			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.Color.Value = this.Color;
			model.Scale.Value = new Vector3(0.5f);
			model.IsInstanced.Value = false;
			model.Editable = false;
			model.Serialize = false;

			result.Add("EditorModel2", model);

			model.Add(new Binding<Matrix>(model.Transform, result.Get<Transform>().Matrix));
		}
	}
}
