using System;
using ComponentBind;
using Lemma.GameScripts;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Util;
using Lemma.Components;
using Lemma.Factories;

namespace Lemma.GameScripts
{
	public class RiftSpawner : ScriptBase
	{
		public static new bool AvailableInReleaseEditor = true;
		private static Random random = new Random();
		public static void Run(Entity script)
		{
			Updater updater = script.Create<Updater>();
			Command enable = command(script, "Enable");
			Command disable = command(script, "Disable");
			Property<bool> enabled = property<bool>(script, "Enabled");
			updater.Add(new Binding<bool>(updater.Enabled, enabled));

			script.Add(new CommandBinding(enable, () => !enabled, delegate()
			{
				enabled.Value = true;
			}));

			script.Add(new CommandBinding(disable, () => enabled, delegate()
			{
				enabled.Value = false;
			}));

			script.Add(new Binding<float, bool>(WorldFactory.Instance.Get<World>().CameraShakeAmount, x => x ? 0.02f : 0.0f, enabled));

			script.Add(new ChangeBinding<bool>(enabled, delegate(bool old, bool value)
			{
				if (!old && value)
					enable.Execute();
				else if (old && !value)
					disable.Execute();
			}));

			RiftFactory riftFactory = Factory.Get<RiftFactory>();
			const float minInterval = 3.0f;
			const float maxInterval = 8.0f;
			float interval = minInterval + (float)random.NextDouble() * (maxInterval - minInterval);
			updater.Action = delegate(float dt)
			{
				Entity player = PlayerFactory.Instance;
				if (player != null)
				{
					interval -= dt;
					if (interval < 0.0f)
					{
						Vector3 pos = player.Get<Transform>().Position;
						Vector3 dir = Vector3.Normalize(new Vector3((float)random.NextDouble() * 2.0f - 1.0f, (float)random.NextDouble() * 2.0f - 1.0f, (float)random.NextDouble() * 2.0f - 1.0f));
						Voxel.GlobalRaycastResult hit = default(Voxel.GlobalRaycastResult);
						int radius = random.Next(4, 10);
						int tries = 30;
						bool success = false;
						while (tries > 0)
						{
							hit = Voxel.GlobalRaycast(pos, dir, 50.0f);

							if (hit.Voxel != null
								&& hit.Distance > radius
								&& Rift.Query(hit.Position) == null
								&& Zone.CanSpawnRift(hit.Position)
								&& MapExit.Query(hit.Position, 5 + radius) == null)
							{
								success = true;
								break;
							}

							tries--;
						}

						if (success)
						{
							Entity rift = riftFactory.CreateAndBind(main);
							rift.Get<Transform>().Position.Value = hit.Position;
							VoxelAttachable attachment = rift.Get<VoxelAttachable>();
							attachment.AttachedVoxel.Value = hit.Voxel.Entity;
							attachment.Coord.Value = hit.Coordinate.Value;
							Rift riftComponent = rift.Get<Rift>();
							riftComponent.Radius.Value = radius;
							main.Add(rift);
							riftComponent.Enabled.Value = true;
							interval = minInterval + (float)random.NextDouble() * (maxInterval - minInterval);
						}
					}
				}
			};
			script.Add(updater);
		}

		public static IEnumerable<string> EditorProperties(Entity script)
		{
			script.Add("Enabled", property<bool>(script, "Enabled"));
			return new string[]
			{
				"Enabled",
			};
		}

		public static IEnumerable<string> Commands(Entity script)
		{
			script.Add("Enable", command(script, "Enable"));
			script.Add("Disable", command(script, "Disable"));
			return new string[]
			{
				"Enable",
				"Disable",
			};
		}
	}
}