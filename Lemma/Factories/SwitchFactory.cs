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
			Entity result = new Entity(main, "Switch");

			result.Add("Transform", new Transform());
			PointLight pointLight = new PointLight();
			pointLight.Attenuation.Value = 10.0f;
			pointLight.Color.Value = Vector3.One;
			result.Add("PointLight", pointLight);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspendByDistance = true;

			PointLight light = result.Get<PointLight>();
			Transform transform = result.Get<Transform>();

			Property<float> attachOffset = result.GetOrMakeProperty<float>("AttachmentOffset", true);
			Property<Entity.Handle> map = result.GetOrMakeProperty<Entity.Handle>("AttachedMap");
			Property<Map.Coordinate> coord = result.GetOrMakeProperty<Map.Coordinate>("AttachedCoordinate");

			Property<bool> on = result.GetOrMakeProperty<bool>("On");

			light.Add(new Binding<Vector3>(light.Position, () => Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix), attachOffset, transform.Matrix));

			ListProperty<Entity.Handle> targets = result.GetOrMakeListProperty<Entity.Handle>("Targets");

			result.Add(new NotifyBinding(delegate()
			{
				Sound.PlayCue(main, on ? "Switch On" : "Switch Off", light.Position);
				foreach (Entity.Handle targetHandle in targets)
				{
					Entity target = targetHandle.Target;
					if (target != null && target.Active)
					{
						if (on)
						{
							Command<Entity> triggerCommand = target.GetCommand<Entity>("Trigger");
							if (triggerCommand != null)
								triggerCommand.Execute(null);
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
				CommandBinding<IEnumerable<Map.Coordinate>, Map> cellFilledBinding = null;

				Map.CellState poweredState = WorldFactory.StatesByName["PoweredSwitch"];

				result.Add(new NotifyBinding(delegate()
				{
					if (attachmentBinding != null)
					{
						result.Remove(attachmentBinding);
						result.Remove(deleteBinding);
						result.Remove(cellFilledBinding);
					}

					Map m = map.Value.Target.Get<Map>();
					coord.Value = m.GetCoordinate(Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix));

					on.Value = m[coord] == poweredState;

					Matrix offset = transform.Matrix * Matrix.Invert(Matrix.CreateTranslation(m.Offset) * m.Transform);

					attachmentBinding = new Binding<Matrix>(transform.Matrix, () => offset * Matrix.CreateTranslation(m.Offset) * m.Transform, m.Transform, m.Offset);
					result.Add(attachmentBinding);

					deleteBinding = new CommandBinding(m.Delete, result.Delete);
					result.Add(deleteBinding);

					cellFilledBinding = new CommandBinding<IEnumerable<Map.Coordinate>, Map>(m.CellsFilled, delegate(IEnumerable<Map.Coordinate> coords, Map newMap)
					{
						foreach (Map.Coordinate c in coords)
						{
							if (c.Equivalent(coord))
							{
								on.Value = c.Data == poweredState;
								break;
							}
						}
					});
					result.Add(cellFilledBinding);
				}, map));

				result.Add(new PostInitialization
				{
					delegate()
					{
						if (map.Value.Target == null)
						{
							Map closestMap = null;
							int closestDistance = 3;
							float closestFloatDistance = 3.0f;
							Vector3 target = Vector3.Transform(new Vector3(0, 0, attachOffset), transform.Matrix);
							foreach (Map m in Map.Maps)
							{
								Map.Coordinate targetCoord = m.GetCoordinate(target);
								Map.Coordinate? c = m.FindClosestFilledCell(targetCoord, closestDistance);
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
								result.Delete.Execute();
							else
								map.Value = closestMap.Entity;
						}
						else
							map.Reset();
					}
				});
			}

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			base.AttachEditorComponents(result, main);
			EntityConnectable.AttachEditorComponents(result, result.GetOrMakeListProperty<Entity.Handle>("Targets"));
			MapAttachable.AttachEditorComponents(result, main, result.Get<Model>().Color);
		}
	}
}
