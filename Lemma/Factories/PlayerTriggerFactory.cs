using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class PlayerTriggerFactory : Factory
	{
		public PlayerTriggerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "PlayerTrigger");

			Transform position = new Transform();

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 10.0f;
			result.Add("PlayerTrigger", trigger);

			result.Add("Position", position);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			Property<bool> selected = new Property<bool> { Value = false, Editable = false, Serialize = false };
			result.Add("EditorSelected", selected);

			PlayerTrigger trigger = result.Get<PlayerTrigger>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "Models\\alpha-sphere";
			model.Alpha.Value = 0.15f;
			model.Color.Value = this.Color;
			model.DisableCulling.Value = true;
			model.IsInstanced.Value = false;
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(x), trigger.Radius));
			model.Editable = false;
			model.Serialize = false;
			model.Add(new Binding<bool>(model.Enabled, selected));

			result.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), trigger.Position));
		}
	}
}
