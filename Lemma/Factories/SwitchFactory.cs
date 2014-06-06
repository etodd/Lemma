using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;

namespace Lemma.Factories
{
	public class SwitchFactory : Factory<Main>
	{
		public SwitchFactory()
		{
			this.Color = new Vector3(1.0f, 1.0f, 0.7f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "Switch");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspendByDistance = true;

			PointLight light = entity.GetOrCreate<PointLight>("PointLight");
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			Property<float> attachOffset = entity.GetOrMakeProperty<float>("AttachmentOffset", true);
			Property<Entity.Handle> map = entity.GetOrMakeProperty<Entity.Handle>("AttachedVoxel");
			Property<Voxel.Coord> coord = entity.GetOrMakeProperty<Voxel.Coord>("AttachedCoordinate");

			Property<bool> on = entity.GetOrMakeProperty<bool>("On");

			light.Add(new Binding<Vector3>(light.Position, () => Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix), attachOffset, transform.Matrix));

			ListProperty<Entity.Handle> targets = entity.GetOrMakeListProperty<Entity.Handle>("Targets");

			entity.Add(new NotifyBinding(delegate()
			{
				AkSoundEngine.PostEvent(on ? AK.EVENTS.PLAY_SWITCH_ON : AK.EVENTS.PLAY_SWITCH_OFF, light.Position);
				foreach (Entity.Handle targetHandle in targets)
				{
					Entity target = targetHandle.Target;
					if (target != null && target.Active)
					{
						if (on)
						{
							Command triggerCommand = target.GetCommand("Trigger");
							if (triggerCommand != null)
								triggerCommand.Execute();
						}

						if (target.Type == "Spinner")
							target.GetProperty<float>("Goal").Value = target.GetProperty<float>(on ? "Maximum" : "Minimum").Value;
						else if (target.Type == "Slider")
							target.GetProperty<int>("Goal").Value = target.GetProperty<int>(on ? "Maximum" : "Minimum").Value;
					}
				}
			}, on));

			if (main.EditorEnabled)
				light.Enabled.Value = true;
			else
			{
				light.Add(new Binding<bool>(light.Enabled, on));
				Binding<Matrix> attachmentBinding = null;
				CommandBinding deleteBinding = null;
				CommandBinding<IEnumerable<Voxel.Coord>, Voxel> cellFilledBinding = null;

				Voxel.State poweredState = Voxel.States[Voxel.t.PoweredSwitch];

				entity.Add(new NotifyBinding(delegate()
				{
					if (attachmentBinding != null)
					{
						entity.Remove(attachmentBinding);
						entity.Remove(deleteBinding);
						entity.Remove(cellFilledBinding);
					}

					Voxel m = map.Value.Target.Get<Voxel>();
					coord.Value = m.GetCoordinate(Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix));

					on.Value = m[coord] == poweredState;

					Matrix offset = transform.Matrix * Matrix.Invert(Matrix.CreateTranslation(m.Offset) * m.Transform);

					attachmentBinding = new Binding<Matrix>(transform.Matrix, () => offset * Matrix.CreateTranslation(m.Offset) * m.Transform, m.Transform, m.Offset);
					entity.Add(attachmentBinding);

					deleteBinding = new CommandBinding(m.Delete, entity.Delete);
					entity.Add(deleteBinding);

					cellFilledBinding = new CommandBinding<IEnumerable<Voxel.Coord>, Voxel>(m.CellsFilled, delegate(IEnumerable<Voxel.Coord> coords, Voxel newMap)
					{
						foreach (Voxel.Coord c in coords)
						{
							if (c.Equivalent(coord))
							{
								on.Value = c.Data == poweredState;
								break;
							}
						}
					});
					entity.Add(cellFilledBinding);
				}, map));

				entity.Add(new PostInitialization
				{
					delegate()
					{
						if (map.Value.Target == null)
						{
							Voxel closestMap = null;
							int closestDistance = 3;
							float closestFloatDistance = 3.0f;
							Vector3 target = Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix);
							foreach (Voxel m in Voxel.Voxels)
							{
								Voxel.Coord targetCoord = m.GetCoordinate(target);
								Voxel.Coord? c = m.FindClosestFilledCell(targetCoord, closestDistance);
								if (c.HasValue)
								{
									float distance = (m.GetRelativePosition(c.Value) - m.GetRelativePosition(targetCoord)).Length();
									if (distance < closestFloatDistance)
									{
										closestFloatDistance = distance;
										closestDistance = (int)Math.Floor(distance);
										closestMap = m;
									}
								}
							}
							if (closestMap == null)
								entity.Delete.Execute();
							else
								map.Value = closestMap.Entity;
						}
						else
							map.Reset();
					}
				});
			}

			this.SetMain(entity, main);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			EntityConnectable.AttachEditorComponents(entity, entity.GetOrMakeListProperty<Entity.Handle>("Targets"));
			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
