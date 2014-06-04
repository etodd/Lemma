using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Factories
{
	public class EntityConnectable
	{
		public static void AttachEditorComponents(Entity entity, Property<Entity.Handle> target)
		{
			Transform transform = entity.Get<Transform>();

			Property<bool> selected = entity.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity other)
				{
					if (target.Value.Target == other)
						target.Value = null;
					else if (entity != other)
						target.Value = other;
				}
			};
			entity.Add("ToggleEntityConnected", toggleEntityConnected);

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

			entity.Add(connectionLines);
		}

		public static void AttachEditorComponents(Entity entity, ListProperty<Entity.Handle> target)
		{
			Transform transform = entity.Get<Transform>();

			Property<bool> selected = entity.GetOrMakeProperty<bool>("EditorSelected", false);
			selected.Serialize = false;

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity other)
				{
					if (target.Contains(other))
						target.Remove(other);
					else if (entity != other)
						target.Add(other);
				}
			};
			entity.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
			ListBinding<LineDrawer.Line, Entity.Handle> connectionBinding = new ListBinding<LineDrawer.Line, Entity.Handle>(connectionLines.Lines, target, delegate(Entity.Handle other)
			{
				return new LineDrawer.Line
				{
					A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
					B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(other.Target.Get<Transform>().Position, connectionLineColor)
				};
			}, x => x.Target != null && x.Target.Active);
			entity.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, selected));
			entity.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, () => selected, transform.Position));
			connectionLines.Add(connectionBinding);
			entity.Add(connectionLines);
		}
	}
}
