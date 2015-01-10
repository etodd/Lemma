using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class PowerBlockSocketFactory : Factory<Main>
	{
		public PowerBlockSocketFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "PowerBlockSocket");
		}

		private Random random = new Random();

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			this.SetMain(entity, main);

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main, true, false);
			attachable.Offset.Value = 1;
			attachable.Enabled.Value = true;

			PowerBlockSocket socket = entity.GetOrCreate<PowerBlockSocket>("PowerBlockSocket");
			socket.Add(new Binding<Voxel.Coord>(socket.Coord, attachable.Coord));
			socket.Add(new Binding<Entity.Handle>(socket.AttachedVoxel, attachable.AttachedVoxel));

			const float maxLightAttenuation = 15.0f;
			PointLight light = entity.Create<PointLight>();
			light.Attenuation.Value = maxLightAttenuation;
			light.Add(new Binding<Vector3>(light.Position, transform.Position));
			light.Add(new Binding<Vector3, Voxel.t>(light.Color, delegate(Voxel.t t)
			{
				switch (t)
				{
					case Voxel.t.GlowBlue:
						return new Vector3(0.8f, 0.9f, 1.2f);
					case Voxel.t.GlowYellow:
						return new Vector3(1.2f, 1.2f, 0.8f);
					default:
						return Vector3.One;
				}
			}, socket.Type));
			light.Add(new Binding<bool>(light.Enabled, socket.Powered));

			PointLight animationLight = entity.Create<PointLight>();
			animationLight.Add(new Binding<Vector3>(animationLight.Position, light.Position));
			animationLight.Add(new Binding<Vector3>(animationLight.Color, light.Color));
			animationLight.Enabled.Value = false;

			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("PlayerTrigger");
			trigger.Radius.Value = 7;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			const float minimumChangeTime = 1.5f;
			float lastChange = -minimumChangeTime;
			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate()
			{
				if (main.TotalTime - lastChange > minimumChangeTime)
				{
					BlockCloud cloud = PlayerFactory.Instance.Get<BlockCloud>();

					bool changed = false;
					Voxel sockVoxel = attachable.AttachedVoxel.Value.Target.Get<Voxel>();
					if (!socket.Powered && cloud.Type.Value == socket.Type.Value)
					{
						// Plug in to the socket
						List<Voxel.Coord> coords = new List<Voxel.Coord>();
						Queue<Voxel.Coord> queue = new Queue<Voxel.Coord>();
						queue.Enqueue(sockVoxel.GetCoordinate(transform.Position));
						while (queue.Count > 0)
						{
							Voxel.Coord c = queue.Dequeue();
							coords.Add(c);
							if (coords.Count >= cloud.Blocks.Length)
								break;

							Voxel.CoordDictionaryCache[c] = true;
							foreach (Direction adjacentDirection in DirectionExtensions.Directions)
							{
								Voxel.Coord adjacentCoord = c.Move(adjacentDirection);
								if (!Voxel.CoordDictionaryCache.ContainsKey(adjacentCoord))
								{
									Voxel.t adjacentID = sockVoxel[adjacentCoord].ID;
									if (adjacentID == Voxel.t.Empty)
										queue.Enqueue(adjacentCoord);
								}
							}
						}
						Voxel.CoordDictionaryCache.Clear();

						EffectBlockFactory factory = Factory.Get<EffectBlockFactory>();
						int i = 0;
						foreach (Entity block in cloud.Blocks)
						{
							Entity effectBlockEntity = factory.CreateAndBind(main);
							Voxel.States.All[cloud.Type].ApplyToEffectBlock(effectBlockEntity.Get<ModelInstance>());
							EffectBlock effectBlock = effectBlockEntity.Get<EffectBlock>();
							effectBlock.DoScale = false;
							Transform blockTransform = block.Get<Transform>();
							effectBlock.StartPosition = blockTransform.Position;
							effectBlock.StartOrientation = blockTransform.Quaternion;
							effectBlock.TotalLifetime = (i + 1) * 0.04f;
							effectBlock.Setup(sockVoxel.Entity, coords[i], cloud.Type);
							main.Add(effectBlockEntity);
							block.Delete.Execute();
							i++;
						}
						cloud.Blocks.Clear();
						cloud.Type.Value = Voxel.t.Empty;
						socket.Powered.Value = true;
						changed = true;
					}
					else if (socket.Powered && cloud.Type.Value == Voxel.t.Empty && !socket.PowerOnOnly)
					{
						// Pull blocks out of the socket
						SceneryBlockFactory factory = Factory.Get<SceneryBlockFactory>();
						Quaternion quat = Quaternion.CreateFromRotationMatrix(sockVoxel.Transform);
						cloud.Type.Value = socket.Type;
						List<Voxel.Coord> coords = sockVoxel.GetContiguousByType(new[] { sockVoxel.GetBox(transform.Position) }).SelectMany(x => x.GetCoords()).ToList();
						sockVoxel.Empty(coords, true);
						sockVoxel.Regenerate();
						ParticleSystem particles = ParticleSystem.Get(main, "WhiteShatter");
						foreach (Voxel.Coord c in coords)
						{
							Vector3 pos = sockVoxel.GetAbsolutePosition(c);
							for (int j = 0; j < 20; j++)
							{
								Vector3 offset = new Vector3((float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f, (float)this.random.NextDouble() - 0.5f);
								particles.AddParticle(pos + offset, offset);
							}
							Entity block = factory.CreateAndBind(main);
							Transform blockTransform = block.Get<Transform>();
							blockTransform.Position.Value = pos;
							blockTransform.Quaternion.Value = quat;
							SceneryBlock sceneryBlock = block.Get<SceneryBlock>();
							sceneryBlock.Type.Value = socket.Type;
							sceneryBlock.Scale.Value = 0.5f;
							cloud.Blocks.Add(block);
							main.Add(block);
						}
						socket.Powered.Value = false;
						changed = true;
					}

					if (changed)
					{
						lastChange = main.TotalTime;
						animationLight.Enabled.Value = true;
						animationLight.Attenuation.Value = 0.0f;
						entity.Add(new Animation
						(
							new Animation.FloatMoveTo(animationLight.Attenuation, maxLightAttenuation, 0.25f),
							new Animation.FloatMoveTo(animationLight.Attenuation, 0.0f, 2.0f),
							new Animation.Set<bool>(animationLight.Enabled, false)
						));
					}
				}
			}));

			entity.Add("Type", socket.Type);
			entity.Add("Powered", socket.Powered, new PropertyEntry.EditorData { Readonly = true });
			entity.Add("PowerOnOnly", socket.PowerOnOnly);
			entity.Add("OnPowerOn", socket.OnPowerOn);
			entity.Add("OnPowerOff", socket.OnPowerOff);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}