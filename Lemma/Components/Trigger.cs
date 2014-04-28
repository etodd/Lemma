using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class Trigger : Component<Main>, IUpdateableComponent
	{
		public Property<float> Radius = new Property<float> { Value = 10.0f };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<bool> IsTriggered = new Property<bool> { Editable = false };
		public Property<Entity.Handle> Target = new Property<Entity.Handle> { Editable = false };

		[XmlIgnore]
		public Command Entered = new Command();

		[XmlIgnore]
		public Command Exited = new Command();

		public Trigger()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
			this.Enabled.Editable = true;
		}

		public override void InitializeProperties()
		{
			base.InitializeProperties();
			Action clear = delegate()
			{
				this.IsTriggered.Value = false;
			};
			this.Add(new CommandBinding(this.OnSuspended, clear));
			this.Add(new CommandBinding(this.OnDisabled, clear));
		}

		public void Update(float elapsedTime)
		{
			bool targetFound = false;
			Entity target = this.Target.Value.Target;
			if (target != null && target.Active && (target.Get<Transform>().Position.Value - this.Position.Value).Length() <= this.Radius)
			{
				targetFound = true;
				if (!this.IsTriggered)
				{
					this.Target.Value = target;
					this.IsTriggered.Value = true;
					this.Entered.Execute();
				}
			}

			if (!targetFound && this.IsTriggered)
				this.IsTriggered.Value = false;
		}

		public static void AttachEditorComponents(Entity entity, Main main, Vector3 color)
		{
			Property<bool> selected = new Property<bool> { Value = false, Editable = false, Serialize = false };
			entity.Add("EditorSelected", selected);

			Transform transform = entity.Get<Transform>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "Models\\alpha-sphere";
			model.Alpha.Value = 0.15f;
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(x), entity.Get<Trigger>().Radius));
			model.Editable = false;
			model.Serialize = false;
			model.DrawOrder.Value = 11; // In front of water
			model.Add(new Binding<bool>(model.Enabled, selected));

			entity.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
		}
	}
}
