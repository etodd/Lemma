using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class InfectorFactory : Factory
	{
		public InfectorFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Infector");

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspendByDistance = true;

			Transform transform = result.GetOrCreate<Transform>("Transform");

			Property<Entity.Handle> map = result.GetOrMakeProperty<Entity.Handle>("AttachedMap");
			Property<Map.Coordinate> coord = result.GetOrMakeProperty<Map.Coordinate>("AttachedCoordinate");

			Property<float> lastTrigger = result.GetOrMakeProperty<float>("LastTrigger", false, -3.0f);

			ListProperty<Entity.Handle> triggers = result.GetOrMakeListProperty<Entity.Handle>("Triggers");

			Property<int> operationalRadius = result.GetOrMakeProperty<int>("OperationalRadius", true, 200);

			Property<bool> enabled = result.GetOrMakeProperty<bool>("Enabled", true, false);

			Map.CellState neutral = WorldFactory.StatesByName["Neutral"];
			Map.CellState infected = WorldFactory.StatesByName["Infected"];

			Action<Entity> infect = delegate(Entity player)
			{
				enabled.Value = true;

				if (main.TotalTime.Value - lastTrigger.Value < 3.0f)
					return;

				lastTrigger.Value = main.TotalTime;

				Entity mapEntity = map.Value.Target;
				if (mapEntity.Active)
				{
					Map m = mapEntity.Get<Map>();
					if (m[coord.Value].ID == infected.ID)
					{
						List<Map.Coordinate> contiguous = m.GetContiguousByType(new[] { m.GetBox(coord.Value) }).SelectMany(x => x.GetCoords()).ToList();

						m.Empty(contiguous, null, false);
						foreach (Map.Coordinate c in contiguous)
						{
							bool isStart = c.Equivalent(coord.Value);
							m.Fill(c, isStart ? infected : neutral, isStart);
						}
					}
					else
					{
						m.Empty(coord.Value, null, false);
						m.Fill(coord.Value, infected);
					}
					m.Regenerate();
				}
				else
					result.Delete.Execute();
			};

			if (!main.EditorEnabled)
			{
				result.Add(new PostInitialization
				{
					delegate()
					{
						bool delete = true;
						foreach (Entity.Handle trigger in triggers)
						{
							Entity e = trigger.Target;
							if (e != null && e.Active)
							{
								result.Add(new CommandBinding<Entity>(e.Get<PlayerTrigger>().PlayerEntered, infect));
								delete = false;
							}
						}
						if (delete)
							result.Delete.Execute();
					}
				});
			}

			result.Add(new CommandBinding<Entity>(((GameMain)main).PlayerSpawned, delegate(Entity p)
			{
				Player player = p.Get<Player>();
				p.Add(new CommandBinding(p.Delete, delegate()
				{
					if (player.Health.Value == 0 && enabled && (p.Get<Transform>().Position - transform.Position.Value).Length() < operationalRadius)
					{
						result.Add(new Animation
						(
							new Animation.Delay(0.5f),
							new Animation.Execute(delegate()
							{
								infect(p);
							})
						));
					}
				}));
			}));

			VoxelEntity.Attach(result, main);

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);

			VoxelEntity.AttachEditorComponents(result, main);

			Transform transform = result.Get<Transform>();

			Property<bool> selected = result.GetOrMakeProperty<bool>("EditorSelected");
			selected.Serialize = false;

			ListProperty<Entity.Handle> triggers = result.GetOrMakeListProperty<Entity.Handle>("Triggers");

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					if (triggers.Contains(entity))
						triggers.Remove(entity);
					else
						triggers.Add(entity);
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

			ListBinding<LineDrawer.Line, Entity.Handle> connectionBinding = new ListBinding<LineDrawer.Line, Entity.Handle>(connectionLines.Lines, triggers, delegate(Entity.Handle entity)
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
