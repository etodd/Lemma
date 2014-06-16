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
	public class PlayerCylinderTrigger : Component<Main>, IUpdateableComponent
	{
		public Property<float> Radius = new Property<float> { Value = 5.0f };
		public Property<float> Top = new Property<float> { Value = 10.0f };
		public Property<float> Bottom = new Property<float> { Value = 0.0f };
		public Property<Matrix> Transform = new Property<Matrix>();
		public Property<bool> IsTriggered = new Property<bool>();
		public Property<Entity.Handle> Player = new Property<Entity.Handle>();

		[XmlIgnore]
		public Command PlayerEntered = new Command();

		[XmlIgnore]
		public Command PlayerExited = new Command();

		public PlayerCylinderTrigger()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
		}

		public override void Awake()
		{
			base.Awake();
			this.Add(new CommandBinding(this.Disable, delegate() { this.IsTriggered.Value = false; }));
		}

		public void Update(float elapsedTime)
		{
			bool playerFound = false;
			Entity player = PlayerFactory.Instance;
			if (player != null)
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
							this.PlayerEntered.Execute();
						}
					}
				}
			}

			if (!playerFound && this.IsTriggered)
			{
				this.PlayerExited.Execute();
				this.IsTriggered.Value = false;
				this.Player.Value = null;
			}
		}

		public static void AttachEditorComponents(Entity entity, Main main, Vector3 color)
		{
			PlayerCylinderTrigger trigger = entity.Get<PlayerCylinderTrigger>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "Models\\alpha-cylinder";
			model.Alpha.Value = 0.15f;
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Add(new Binding<Vector3>(model.Scale, () => new Vector3(trigger.Radius, trigger.Top - trigger.Bottom, trigger.Radius), trigger.Top, trigger.Bottom, trigger.Radius));
			model.Serialize = false;
			model.Add(new Binding<bool>(model.Enabled, entity.EditorSelected));

			entity.Add(model);

			model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateTranslation(0.0f, (trigger.Top + trigger.Bottom) * 0.5f, 0.0f) * trigger.Transform, trigger.Transform, trigger.Top, trigger.Bottom));
		}
	}
}
