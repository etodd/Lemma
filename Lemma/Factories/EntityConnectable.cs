using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public class EntityConnectable
	{
		public static void AttachEditorComponents(Entity result, Main main, Property<Entity.Handle> target)
		{
			Transform transform = result.Get<Transform>();

			Property<bool> selected = result.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					if (target.Value.Target == entity)
						target.Value = null;
					else if (entity != result)
						target.Value = entity;
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

			connectionLines.Add(new NotifyBinding(delegate()
			{
				connectionLines.Lines.Clear();
				Entity targetEntity = target.Value.Target;
				if (targetEntity != null)
				{
					connectionLines.Lines.Add
					(
						new LineDrawer.Line
						{
							A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
							B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(targetEntity.Get<Transform>().Position, connectionLineColor)
						}
					);
				}
			}, transform.Position, target, selected));

			result.Add(connectionLines);
		}

		public static void AttachEditorComponents(Entity result, Main main, ListProperty<Entity.Handle> target)
		{
			Transform transform = result.Get<Transform>();

			Property<bool> selected = result.GetOrMakeProperty<bool>("EditorSelected");

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					if (target.Contains(entity))
						target.Remove(entity);
					else if (entity != result)
						target.Add(entity);
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
			ListBinding<LineDrawer.Line, Entity.Handle> connectionBinding = new ListBinding<LineDrawer.Line, Entity.Handle>(connectionLines.Lines, target, delegate(Entity.Handle entity)
			{
				if (entity.Target == null)
					return new LineDrawer.Line[] { };
				else
				{
					return new[]
					{
						new LineDrawer.Line
						{
							A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
							B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(entity.Target.Get<Transform>().Position, connectionLineColor)
						}
					};
				}
			});
			result.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, selected));
			result.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, () => selected, transform.Position));
			connectionLines.Add(connectionBinding);
			result.Add(connectionLines);
		}
	}
}
