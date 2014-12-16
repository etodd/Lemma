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
			attachable.Enabled.Value = true;

			PowerBlockSocket socket = entity.GetOrCreate<PowerBlockSocket>("PowerBlockSocket");
			socket.Add(new Binding<Entity.Handle>(socket.AttachedVoxel, attachable.AttachedVoxel));
			socket.Add(new Binding<Vector3>(socket.Position, transform.Position));

			PlayerTrigger trigger = entity.GetOrCreate<PlayerTrigger>("PlayerTrigger");
			trigger.Radius.Value = 10;
			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));
			trigger.Add(new CommandBinding(trigger.PlayerEntered, delegate()
			{
				BlockCloud cloud = PlayerFactory.Instance.Get<BlockCloud>();

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
						effectBlock.DoScale.Value = false;
						Transform blockTransform = block.Get<Transform>();
						effectBlock.StartPosition.Value = blockTransform.Position;
						effectBlock.StartOrientation.Value = blockTransform.Quaternion;
						effectBlock.TotalLifetime.Value = (i + 1) * 0.04f;
						effectBlock.Setup(sockVoxel.Entity, coords[i], cloud.Type);
						main.Add(effectBlockEntity);
						block.Delete.Execute();
						i++;
					}
					cloud.Blocks.Clear();
					cloud.Type.Value = Voxel.t.Empty;
					socket.Powered.Value = true;
				}
				else if (socket.Powered && cloud.Type.Value == Voxel.t.Empty)
				{
					SceneryBlockFactory factory = Factory.Get<SceneryBlockFactory>();
					Quaternion quat = Quaternion.CreateFromRotationMatrix(sockVoxel.Transform);
					cloud.Type.Value = socket.Type;
					List<Voxel.Coord> coords = sockVoxel.GetContiguousByType(new[] { sockVoxel.GetBox(transform.Position) }).SelectMany(x => x.GetCoords()).ToList();
					sockVoxel.Empty(coords, true);
					sockVoxel.Regenerate();
					Vector3 scale = new Vector3(0.6f);
					foreach (Voxel.Coord c in coords)
					{
						Entity block = factory.CreateAndBind(main);
						Transform blockTransform = block.Get<Transform>();
						blockTransform.Position.Value = sockVoxel.GetAbsolutePosition(c);
						blockTransform.Quaternion.Value = quat;
						block.Get<PhysicsBlock>().Size.Value = scale;
						block.Get<ModelInstance>().Scale.Value = scale;
						block.Get<SceneryBlock>().Type.Value = socket.Type;
						cloud.Blocks.Add(block);
						main.Add(block);
					}
					socket.Powered.Value = false;
				}
			}));

			PointLight light = entity.Create<PointLight>();
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

			entity.Add("Type", socket.Type);
			entity.Add("AttachOffset", attachable.Offset);
			entity.Add("Powered", socket.Powered, new PropertyEntry.EditorData { Readonly = true });
			entity.Add("OnPowered", socket.OnPowerOn);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}