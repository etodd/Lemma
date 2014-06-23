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
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<bool> IsTriggered = new Property<bool>();
		public Property<Entity.Handle> Target = new Property<Entity.Handle>();

		[XmlIgnore]
		public Command Entered = new Command();

		[XmlIgnore]
		public Command Exited = new Command();

		public Trigger()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
		}

		public void EditorProperties()
		{
			this.Entity.Add("Radius", this.Radius);
			this.Entity.Add("Enabled", this.Enabled);
		}

		public override void Awake()
		{
			base.Awake();
			Action clear = delegate()
			{
				this.IsTriggered.Value = false;
			};
			this.Add(new CommandBinding(this.OnSuspended, clear));
			this.Add(new CommandBinding(this.Disable, clear));
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
			Transform transform = entity.Get<Transform>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\sphere";
			model.Alpha.Value = 0.15f;
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(x), entity.Get<Trigger>().Radius));
			model.Serialize = false;
			model.DrawOrder.Value = 11; // In front of water
			model.Add(new Binding<bool>(model.Enabled, entity.EditorSelected));

			entity.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
		}
	}
}
