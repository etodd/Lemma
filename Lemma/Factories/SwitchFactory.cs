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

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);
			Property<bool> on = entity.GetOrMakeProperty<bool>("On");

			light.Add(new Binding<Vector3>(light.Position, () => Vector3.Transform(new Vector3(0, 0, attachable.Offset), transform.Matrix), attachable.Offset, transform.Matrix));

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
				CommandBinding<IEnumerable<Voxel.Coord>, Voxel> cellFilledBinding = null;

				Voxel.State poweredState = Voxel.States[Voxel.t.PoweredSwitch];

				entity.Add(new NotifyBinding(delegate()
				{
					Voxel m = attachable.AttachedVoxel.Value.Target.Get<Voxel>();
					if (cellFilledBinding != null)
						entity.Remove(cellFilledBinding);

					cellFilledBinding = new CommandBinding<IEnumerable<Voxel.Coord>, Voxel>(m.CellsFilled, delegate(IEnumerable<Voxel.Coord> coords, Voxel newMap)
					{
						foreach (Voxel.Coord c in coords)
						{
							if (c.Equivalent(attachable.Coord))
							{
								on.Value = c.Data == poweredState;
								break;
							}
						}
					});
					entity.Add(cellFilledBinding);
				}, attachable.AttachedVoxel));
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
