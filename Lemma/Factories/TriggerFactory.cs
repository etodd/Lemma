using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class TriggerFactory : Factory
	{
		public TriggerFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 1.0f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Trigger");

			Transform position = new Transform();

			Trigger trigger = new Trigger();
			trigger.Radius.Value = 10.0f;
			result.Add("Trigger", trigger);

			result.Add("Position", position);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Transform transform = result.Get<Transform>();
			Trigger trigger = result.Get<Trigger>();

			if (result.GetOrMakeProperty<bool>("Attach", true))
				MapAttachable.MakeAttachable(result, main);

			trigger.Add(new TwoWayBinding<Vector3>(transform.Position, trigger.Position));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			Trigger.AttachEditorComponents(result, main, this.Color);

			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);

			Trigger trigger = result.Get<Trigger>();

			Transform transform = result.Get<Transform>();

			Property<bool> selected = result.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					if (trigger.Target.Value.Target == entity)
						trigger.Target.Value = null;
					else
						trigger.Target.Value = entity;
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

			connectionLines.Add(new NotifyBinding(delegate()
			{
				connectionLines.Lines.Clear();
				Entity target = trigger.Target.Value.Target;
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
			}, transform.Position, trigger.Target, selected));

			result.Add(connectionLines);
		}
	}
}
