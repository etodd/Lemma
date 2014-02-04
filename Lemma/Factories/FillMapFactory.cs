using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;
using Lemma.Util;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Constraints.TwoEntity.Motors;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.SolverGroups;

namespace Lemma.Factories
{
	public class FillMapFactory : MapFactory
	{
		public override Entity Create(Main main, int offsetX, int offsetY, int offsetZ)
		{
			Entity result = base.Create(main, offsetX, offsetY, offsetZ);
			result.Type = "FillMap";
			result.ID = Entity.GenerateID(result, main);
			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.InternalBind(result, main, creating, null, true);
			if (result.GetOrMakeProperty<bool>("Attached", true))
				MapAttachable.MakeAttachable(result, main);

			Property<Entity.Handle> target = result.GetOrMakeProperty<Entity.Handle>("Target");

			Map map = result.Get<Map>();

			Property<float> intervalMultiplier = result.GetOrMakeProperty<float>("IntervalMultiplier", true, 1.0f);

			Action fill = delegate()
			{
				EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();

				Entity targetEntity = target.Value.Target;
				if (targetEntity != null && targetEntity.Active)
				{
					Map m = targetEntity.Get<Map>();
					int index = 0;
					foreach (Map.Chunk chunk in map.Chunks)
					{
						foreach (Map.Coordinate coord in chunk.Boxes.SelectMany(x => x.GetCoords()))
						{
							Vector3 pos = map.GetAbsolutePosition(coord);
							Map.Coordinate c = m.GetCoordinate(pos);
							Entity block = factory.CreateAndBind(main);
							coord.Data.ApplyToEffectBlock(block.Get<ModelInstance>());
							block.GetProperty<bool>("CheckAdjacent").Value = false;
							block.GetProperty<Vector3>("Offset").Value = m.GetRelativePosition(c);
							block.GetProperty<bool>("Scale").Value = true;

							block.GetProperty<Vector3>("StartPosition").Value = pos + new Vector3(0.25f, 0.5f, 0.25f) * index;
							block.GetProperty<Matrix>("StartOrientation").Value = Matrix.CreateRotationX(0.15f * index) * Matrix.CreateRotationY(0.15f * index);

							block.GetProperty<float>("TotalLifetime").Value = 0.05f + (index * 0.03f * intervalMultiplier);
							factory.Setup(block, targetEntity, c, coord.Data.ID);
							main.Add(block);
							index++;
						}
					}
				}
				result.Delete.Execute();
			};

			result.Add("Fill", new Command
			{
				Action = fill
			});

			result.Add("Trigger", new Command<Entity>
			{
				Action = delegate(Entity p)
				{
					fill();
				}
			});
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
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
