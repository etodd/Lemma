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
		private static Color connectionLineColor = new Color(1.0f, 1.0f, 0.0f, 0.5f);

		public static void AttachEditorComponents(Entity entity, Property<Entity.Handle> target)
		{
			Transform transform = entity.Get<Transform>("Transform");

			entity.Add(new CommandBinding<Entity>(entity.ToggleEntityConnection, delegate(Entity other)
			{
				if (target.Value.Target == other)
					target.Value = null;
				else if (entity != other)
					target.Value = other;
			}));

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, entity.EditorSelected));

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
							B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(targetEntity.Get<Transform>("Transform").Position, connectionLineColor)
						}
					);
				}
			}, transform.Position, target, entity.EditorSelected));

			entity.Add(connectionLines);
		}

		public static void AttachEditorComponents(Entity entity, ListProperty<Entity.Handle> target)
		{
			Transform transform = entity.Get<Transform>("Transform");

			entity.Add(new CommandBinding<Entity>(entity.ToggleEntityConnection, delegate(Entity other)
			{
				if (target.Contains(other))
					target.Remove(other);
				else if (entity != other)
					target.Add(other);
			}));

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, entity.EditorSelected));

			ListBinding<LineDrawer.Line, Entity.Handle> connectionBinding = new ListBinding<LineDrawer.Line, Entity.Handle>(connectionLines.Lines, target, delegate(Entity.Handle other)
			{
				return new LineDrawer.Line
				{
					A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
					B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(other.Target.Get<Transform>("Transform").Position, connectionLineColor)
				};
			}, x => x.Target != null && x.Target.Active);
			entity.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, entity.EditorSelected));
			entity.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, () => entity.EditorSelected, transform.Position));
			connectionLines.Add(connectionBinding);
			entity.Add(connectionLines);
		}
	}
}
