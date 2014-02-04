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

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			Property<Entity.Handle> targetProperty = result.GetOrMakeProperty<Entity.Handle>("Target");
			result.Add(new PostInitialization
			{
				delegate()
				{
					Entity target = targetProperty.Value.Target;
					if (target != null && target.Active)
						trigger.Add(new CommandBinding<Entity>(trigger.PlayerEntered, target.GetCommand<Entity>("Trigger")));
				}
			});

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			PlayerTrigger.AttachEditorComponents(result, main, this.Color);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);

			Property<Entity.Handle> targetProperty = result.GetOrMakeProperty<Entity.Handle>("Target");

			Transform transform = result.Get<Transform>();

			Property<bool> selected = result.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					if (targetProperty.Value.Target == entity)
						targetProperty.Value = null;
					else
						targetProperty.Value = entity;
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

			connectionLines.Add(new NotifyBinding(delegate()
			{
				connectionLines.Lines.Clear();
				Entity target = targetProperty.Value.Target;
				if (target != null)
				{
					connectionLines.Lines.Add
					(
						new LineDrawer.Line
						{
							A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
							B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(target.Get<Transform>().Position, connectionLineColor)
						}
					);
				}
			}, transform.Position, targetProperty, selected));

			result.Add(connectionLines);
		}
	}
}
