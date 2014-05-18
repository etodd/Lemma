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
	public class PlayerTrigger : Component<Main>, IUpdateableComponent
	{
		public Property<float> Radius = new Property<float> { Value = 10.0f };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<bool> IsTriggered = new Property<bool> { Editable = false };
		public Property<Entity.Handle> Player = new Property<Entity.Handle> { Editable = false };

		[XmlIgnore]
		public Command<Entity> PlayerEntered = new Command<Entity>();

		[XmlIgnore]
		public Command<Entity> PlayerExited = new Command<Entity>();

		public PlayerTrigger()
		{
			this.EnabledInEditMode = false;
			this.EnabledWhenPaused = false;
			this.Enabled.Editable = true;
		}

		public override void Awake()
		{
			base.Awake();
			Action clear = delegate()
			{
				this.IsTriggered.Value = false;
				this.Player.Value = null;
			};
			this.Add(new CommandBinding(this.OnSuspended, clear));
			this.Add(new CommandBinding(this.OnDisabled, clear));
		}

		public void Update(float elapsedTime)
		{
			bool playerFound = false;
			Entity player = PlayerFactory.Instance;
			if (player != null && (player.Get<Transform>().Position.Value - this.Position.Value).Length() <= this.Radius)
			{
				playerFound = true;
				if (!this.IsTriggered)
				{
					this.Player.Value = player;
					this.IsTriggered.Value = true;
					this.PlayerEntered.Execute(player);
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
			Property<bool> selected = entity.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			Transform transform = entity.Get<Transform>();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "Models\\alpha-sphere";
			model.Alpha.Value = 0.15f;
			model.Color.Value = color;
			model.DisableCulling.Value = true;
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(x), entity.Get<PlayerTrigger>().Radius));
			model.Editable = false;
			model.Serialize = false;
			model.DrawOrder.Value = 11; // In front of water
			model.Add(new Binding<bool>(model.Enabled, selected));

			entity.Add(model);

			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
		}
	}
}
