using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class VoxelAttachable : Component<Main>
	{
		public Property<float> Offset = new Property<float>();
		public Property<Entity.Handle> AttachedVoxel = new Property<Entity.Handle>();
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();

		[XmlIgnore]
		public Property<Matrix> Transform = new Property<Matrix>();

		private bool detachIfRemoved;
		private bool detachIfMoved;

		[XmlIgnore]
		public Command Detach = new Command();

		public VoxelAttachable()
		{
			this.Enabled.Value = false;
		}

		public static VoxelAttachable MakeAttachable(Entity entity, Main main, bool detachIfRemoved = true, bool detachIfMoved = false, Command detachCommand = null)
		{
			VoxelAttachable attachable = entity.GetOrCreate<VoxelAttachable>("VoxelAttachable");
			attachable.detachIfRemoved = detachIfRemoved;
			attachable.detachIfMoved = detachIfMoved;

			if (main.EditorEnabled)
				return attachable;

			Transform transform = entity.Get<Transform>();

			if (detachCommand == null)
			{
				detachCommand = new Command
				{
					Action = delegate()
					{
						entity.Add(new Animation(new Animation.Execute(entity.Delete)));
					}
				};
			}
			
			attachable.Add(new CommandBinding(attachable.Detach, detachCommand));
			attachable.Add(new TwoWayBinding<Matrix>(transform.Matrix, attachable.Transform));

			return attachable;
		}

		public void EditorProperties()
		{
			this.Entity.Add("AttachOffset", this.Offset);
			this.Entity.Add("Attach", this.Enabled);
		}

		private bool isInitialAttachment;

		public override void Awake()
		{
			base.Awake();
			Binding<Matrix> attachmentBinding = null;
			CommandBinding<IEnumerable<Voxel.Coord>, Voxel> cellEmptiedBinding = null;

			this.isInitialAttachment = this.AttachedVoxel.Value.GUID != 0;

			this.Add(new NotifyBinding(delegate()
			{
				if (attachmentBinding != null)
				{
					this.Remove(attachmentBinding);
					this.Remove(cellEmptiedBinding);
				}

				if (this.AttachedVoxel.Value.Target != null)
				{
					Voxel m = this.AttachedVoxel.Value.Target.Get<Voxel>();
					SliderCommon s = m.Entity.Get<SliderCommon>();
					Vector3 pos = Vector3.Transform(new Vector3(0, 0, this.Offset), this.Transform);
					Matrix voxelTransform;
					if (this.isInitialAttachment)
					{
						voxelTransform = m.Transform;
						this.isInitialAttachment = false;
					}
					else
					{
						voxelTransform = s != null ? s.OriginalTransform : m.Transform;
						Vector3 relativePos = Vector3.Transform(pos, Matrix.Invert(voxelTransform)) + m.Offset;
						this.Coord.Value = m.GetCoordinateFromRelative(relativePos);
					}

					Matrix offset = this.Transform * Matrix.Invert(Matrix.CreateTranslation(m.Offset) * voxelTransform);

					attachmentBinding = new Binding<Matrix>(this.Transform, () => offset * Matrix.CreateTranslation(m.Offset) * m.Transform, m.Transform, m.Offset);
					this.Add(attachmentBinding);

					cellEmptiedBinding = new CommandBinding<IEnumerable<Voxel.Coord>, Voxel>(m.CellsEmptied, delegate(IEnumerable<Voxel.Coord> coords, Voxel newMap)
					{
						foreach (Voxel.Coord c in coords)
						{
							if (c.Equivalent(this.Coord))
							{
								if (newMap == null)
								{
									if (this.detachIfRemoved)
										this.Detach.Execute();
								}
								else
								{
									if (this.detachIfMoved)
										this.Detach.Execute();
									else
										this.AttachedVoxel.Value = newMap.Entity;
								}
								break;
							}
						}
					});
					this.Add(cellEmptiedBinding);
				}
			}, this.AttachedVoxel));
		}

		public override void Start()
		{
			if (this.Enabled && !this.main.EditorEnabled)
			{
				if (this.AttachedVoxel.Value.Target == null)
				{
					Voxel closestMap = null;
					int closestDistance = 3;
					float closestFloatDistance = 3.0f;
					Vector3 target = Vector3.Transform(new Vector3(0, 0, this.Offset), this.Transform);
					foreach (Voxel m in Voxel.Voxels)
					{
						SliderCommon s = m.Entity.Get<SliderCommon>();
						Vector3 relativeTarget = Vector3.Transform(target, Matrix.Invert(s != null ? s.OriginalTransform : m.Transform)) + m.Offset;
						Voxel.Coord targetCoord = m.GetCoordinateFromRelative(relativeTarget);
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
						this.Detach.Execute();
					else
						this.AttachedVoxel.Value = closestMap.Entity;
				}
				else
					this.AttachedVoxel.Reset();
			}
		}

		public static void BindTarget(Entity entity, Property<Vector3> target)
		{
			VoxelAttachable attachable = entity.Get<VoxelAttachable>();
			Transform transform = entity.Get<Transform>();
			entity.Add(new Binding<Vector3>(target, () => Vector3.Transform(new Vector3(0, 0, attachable.Offset), transform.Matrix), attachable.Offset, transform.Matrix));
		}

		public static void BindTarget(Entity entity, Property<Matrix> target)
		{
			VoxelAttachable attachable = entity.Get<VoxelAttachable>();
			Transform transform = entity.Get<Transform>();
			entity.Add(new Binding<Matrix>(target, () => Matrix.CreateTranslation(0, 0, attachable.Offset) * transform.Matrix, attachable.Offset, transform.Matrix));
		}

		public static void AttachEditorComponents(Entity entity, Main main, Property<Vector3> color = null)
		{
			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\cone";
			if (color != null)
				model.Add(new Binding<Vector3>(model.Color, color));

			VoxelAttachable attachable = entity.GetOrCreate<VoxelAttachable>("VoxelAttachable");

			Model editorModel = entity.Get<Model>("EditorModel");
			model.Add(new Binding<bool>(model.Enabled, () => Editor.EditorModelsVisible && (entity.EditorSelected && attachable.Offset > 0), entity.EditorSelected, attachable.Offset, Editor.EditorModelsVisible));
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(1.0f, 1.0f, x), attachable.Offset));
			model.Serialize = false;
			entity.Add("EditorModel2", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<Transform>().Matrix));
		}
	}
}
