using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class ZoneFactory : Factory
	{
		public ZoneFactory()
		{
			this.Color = new Vector3(1.0f, 0.7f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Zone");

			result.Add("Zone", new Zone());
			result.Add("Transform", new Transform());

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;
			result.CannotSuspendByDistance = true;
			Zone zone = result.Get<Zone>();
			Transform transform = result.Get<Transform>();
			zone.Add(new Binding<Vector3>(zone.Position, transform.Position));
			this.SetMain(result, main);
		}

		private Transform addCornerModel(Entity entity, Property<bool> selected)
		{
			Transform transform = new Transform { Serialize = false, Editable = false };
			entity.AddWithoutOverwriting(transform);

			Model cornerModel1 = new Model();
			cornerModel1.Filename.Value = "Models\\sphere";
			cornerModel1.Color.Value = this.Color;
			cornerModel1.IsInstanced.Value = false;
			cornerModel1.Scale.Value = new Vector3(0.5f);
			cornerModel1.Editable = false;
			cornerModel1.Serialize = false;
			entity.Add(cornerModel1);

			cornerModel1.Add(new Binding<Matrix, Vector3>(cornerModel1.Transform, x => Matrix.CreateTranslation(x), transform.Position));
			cornerModel1.Add(new Binding<bool>(cornerModel1.Enabled, selected));

			return transform;
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			Transform transform = result.Get<Transform>();

			Zone zone = result.Get<Zone>();

			Property<bool> selected = new Property<bool> { Value = false, Editable = false, Serialize = false };
			result.Add("EditorSelected", selected);

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					if (zone.ConnectedEntities.Contains(entity))
					{
						zone.ConnectedEntities.Remove(entity);
						Zone z = entity.Get<Zone>();
						if (z != null)
							z.Parent.Value = null;
					}
					else
					{
						zone.ConnectedEntities.Add(entity);
						Zone z = entity.Get<Zone>();
						if (z != null)
							z.Parent.Value = result;
					}
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
			ListBinding<LineDrawer.Line, Entity.Handle> connectionBinding = new ListBinding<LineDrawer.Line, Entity.Handle>(connectionLines.Lines, zone.ConnectedEntities, delegate(Entity.Handle entity)
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

			Model model = new Model();
			model.Filename.Value = "Models\\sphere";
			model.Color.Value = this.Color;
			model.IsInstanced.Value = false;
			model.Scale.Value = new Vector3(0.5f);
			model.Editable = false;
			model.Serialize = false;
			result.Add("EditorModel", model);
			model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));

			Property<Vector3> corner1 = new Property<Vector3> { Editable = false, Serialize = false, Value = zone.BoundingBox.Value.Min };
			Property<Vector3> corner2 = new Property<Vector3> { Editable = false, Serialize = false, Value = zone.BoundingBox.Value.Max };

			result.Add(new Binding<BoundingBox>(zone.BoundingBox, delegate()
			{
				Vector3 a = corner1, b = corner2;
				return new BoundingBox(new Vector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z)), new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z)));
			}, corner1, corner2));

			Transform cornerTransform1 = this.addCornerModel(result, selected);
			cornerTransform1.Add(new TwoWayBinding<Vector3, Vector3>
			(
				corner1,
				x => x - transform.Position,
				new[] { transform.Position },
				cornerTransform1.Position,
				x => x + transform.Position,
				new[] { transform.Position }
			));

			Transform cornerTransform2 = this.addCornerModel(result, selected);
			cornerTransform2.Add(new TwoWayBinding<Vector3, Vector3>
			(
				corner2,
				x => x - transform.Position,
				new[] { transform.Position },
				cornerTransform2.Position,
				x => x + transform.Position,
				new[] { transform.Position }
			));

			ModelAlpha box = new ModelAlpha();
			box.Filename.Value = "Models\\alpha-box";
			box.Color.Value = new Vector3(this.Color.X, this.Color.Y, this.Color.Z);
			box.Alpha.Value = 0.125f;
			box.IsInstanced.Value = false;
			box.Editable = false;
			box.Serialize = false;
			box.DrawOrder.Value = 11; // In front of water
			box.DisableCulling.Value = true;
			result.Add(box);
			box.Add(new Binding<Matrix, BoundingBox>(box.Transform, delegate(BoundingBox x)
			{
				return Matrix.CreateScale(x.Max - x.Min) * Matrix.CreateTranslation((x.Min + x.Max) * 0.5f);
			}, zone.AbsoluteBoundingBox));
			box.Add(new Binding<bool>(box.Enabled, selected));
			box.Add(new Binding<BoundingBox>(box.BoundingBox, zone.BoundingBox));
			box.CullBoundingBox.Value = false;
		}
	}
}
