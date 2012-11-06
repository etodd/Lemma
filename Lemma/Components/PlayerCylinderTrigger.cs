using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class PlayerCylinderTrigger : Component, IUpdateableComponent
	{
		public Property<float> Radius = new Property<float> { Value = 5.0f };
		public Property<float> Top = new Property<float> { Value = 10.0f };
		public Property<float> Bottom = new Property<float> { Value = 0.0f };
		public Property<Matrix> Transform = new Property<Matrix> { Editable = false };
		public Property<bool> IsTriggered = new Property<bool> { Editable = false };
		public Property<Entity.Handle> Player = new Property<Entity.Handle> { Editable = false };

		[XmlIgnore]
		public Command<Entity> PlayerEntered = new Command<Entity>();

		[XmlIgnore]
		public Command<Entity> PlayerExited = new Command<Entity>();

		public PlayerCylinderTrigger()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
		}

		public override void InitializeProperties()
		{
			this.Add(new CommandBinding(this.OnDisabled, delegate() { this.IsTriggered.Value = false; }));
		}

		public void Update(float elapsedTime)
		{
			bool playerFound = false;
			foreach (Entity player in this.main.Get("Player"))
			{
				Vector3 pos = Vector3.Transform(player.Get<Transform>().Position, Matrix.Invert(this.Transform));
				if (pos.Y > this.Bottom && pos.Y < this.Top)
				{
					pos.Y = 0.0f;
					if (pos.Length() < this.Radius)
					{
						playerFound = true;
						if (!this.IsTriggered)
						{
							this.Player.Value = player;
							this.IsTriggered.Value = true;
							this.PlayerEntered.Execute(player);
						}
						break;
					}
				}
			}
			if (!playerFound && this.IsTriggered)
			{
				this.PlayerExited.Execute(this.Player.Value.Target);
				this.IsTriggered.Value = false;
				this.Player.Value = null;
			}
		}

		public static void AttachEditorComponents(Entity entity, Main main, Vector3 color)
		{
			Property<bool> selected = new Property<bool> { Value = false, Editable = false, Serialize = false };
			entity.Add("EditorSelected", selected);

			PlayerCylinderTrigger trigger = entity.Get<PlayerCylinderTrigger>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "Models\\alpha-cylinder";
			model.Alpha.Value = 0.15f;
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Add(new Binding<Vector3>(model.Scale, () => new Vector3(trigger.Radius, trigger.Top - trigger.Bottom, trigger.Radius), trigger.Top, trigger.Bottom, trigger.Radius));
			model.Editable = false;
			model.Serialize = false;
			model.Add(new Binding<bool>(model.Enabled, selected));

			entity.Add(model);

			model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateTranslation(0.0f, (trigger.Top + trigger.Bottom) * 0.5f, 0.0f) * trigger.Transform, trigger.Transform, trigger.Top, trigger.Bottom));
		}
	}
}
