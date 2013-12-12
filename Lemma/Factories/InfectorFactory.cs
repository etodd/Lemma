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

			Property<Entity.Handle> spawn = result.GetOrMakeProperty<Entity.Handle>("SpawnPoint");

			Action<Entity> infect = delegate(Entity player)
			{
				Entity mapEntity = map.Value.Target;
				if (mapEntity.Active)
				{
					Map.CellState neutral = WorldFactory.StatesByName["Neutral"];
					Map.CellState infected = WorldFactory.StatesByName["Infected"];

					Map m = mapEntity.Get<Map>();
					if (m[coord.Value].ID == infected.ID)
					{
						List<Map.Coordinate> contiguous = m.GetContiguousByType(new[] { m.GetBox(coord.Value) }).SelectMany(x => x.GetCoords()).ToList();

						m.Empty(contiguous);
						foreach (Map.Coordinate c in contiguous)
							m.Fill(c, c.Equivalent(coord.Value) ? infected : neutral);
					}
					else
					{
						if (!m.Empty(coord.Value))
						{
							result.Delete.Execute();
							return;
						}
						else
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
						Entity spawnEntity = spawn.Value.Target;
						if (spawnEntity != null && spawnEntity.Active)
							result.Add(new CommandBinding<Entity>(spawnEntity.Get<PlayerTrigger>().PlayerEntered, infect));
						else
							result.Delete.Execute();
					}
				});
			}

			result.Add(new CommandBinding<Entity>(((GameMain)main).PlayerSpawned, delegate(Entity p)
			{
				Player player = p.Get<Player>();
				p.Add(new CommandBinding(p.Delete, delegate()
				{
					if (player.Health.Value == 0)
						infect(p);
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

			Property<Entity.Handle> spawn = result.GetOrMakeProperty<Entity.Handle>("SpawnPoint");

			Command<Entity> toggleEntityConnected = new Command<Entity>
			{
				Action = delegate(Entity entity)
				{
					spawn.Value = entity;
				}
			};
			result.Add("ToggleEntityConnected", toggleEntityConnected);

			LineDrawer connectionLines = new LineDrawer { Serialize = false };
			connectionLines.Add(new Binding<bool>(connectionLines.Enabled, selected));

			Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

			Action recalculateLine = delegate()
			{
				connectionLines.Lines.Clear();
				Entity parent = spawn.Value.Target;
				if (parent != null)
				{
					connectionLines.Lines.Add(new LineDrawer.Line
					{
						A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
						B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(parent.Get<Transform>().Position, connectionLineColor)
					});
				}
			};

			NotifyBinding recalculateBinding = null;
			Action rebuildBinding = delegate()
			{
				if (recalculateBinding != null)
				{
					connectionLines.Remove(recalculateBinding);
					recalculateBinding = null;
				}
				if (spawn.Value.Target != null)
				{
					recalculateBinding = new NotifyBinding(recalculateLine, spawn.Value.Target.Get<Transform>().Matrix);
					connectionLines.Add(recalculateBinding);
				}
				recalculateLine();
			};
			connectionLines.Add(new NotifyBinding(rebuildBinding, spawn));

			connectionLines.Add(new NotifyBinding(recalculateLine, selected));
			connectionLines.Add(new NotifyBinding(recalculateLine, () => selected, transform.Position));
			result.Add(connectionLines);
		}
	}
}
