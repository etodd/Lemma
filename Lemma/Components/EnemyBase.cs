using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class EnemyBase : Component<Main>
	{
		public Property<Matrix> Transform = new Property<Matrix> { Editable = false };
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<float> Offset = new Property<float> { Editable = true, Value = 4.0f };
		public Property<Entity.Handle> Voxel = new Property<Entity.Handle> { Editable = false };
		public ListProperty<Voxel.Box> BaseBoxes = new ListProperty<Voxel.Box> { Editable = false };

		private CommandBinding<IEnumerable<Voxel.Coord>, Voxel> cellEmptiedBinding;

		public bool EnableCellEmptyBinding
		{
			get
			{
				return this.cellEmptiedBinding != null && this.cellEmptiedBinding.Enabled;
			}
			set
			{
				if (this.cellEmptiedBinding != null)
					this.cellEmptiedBinding.Enabled = value;
			}
		}

		public bool IsValid
		{
			get
			{
				Entity mapEntity = this.Voxel.Value.Target;
				if (mapEntity == null || !mapEntity.Active)
					return false;

				Voxel m = mapEntity.Get<Voxel>();

				bool found = false;
				List<Voxel.Box> boxRemovals = null;
				foreach (Voxel.Box box in this.BaseBoxes)
				{
					foreach (Voxel.Coord coord in box.GetCoords())
					{
						if (m[coord].ID == Components.Voxel.t.InfectedCritical)
						{
							found = true;
							break;
						}
					}
					if (!found)
					{
						if (boxRemovals == null)
							boxRemovals = new List<Voxel.Box>();
						boxRemovals.Add(box);
					}
				}

				if (boxRemovals != null)
				{
					foreach (Voxel.Box box in boxRemovals)
						this.BaseBoxes.Remove(box);
				}

				return found;
			}
		}

		public static void AttachEditorComponents(Entity entity, Main main, Vector3 color)
		{
			Property<float> offset = entity.Get<EnemyBase>().Offset;

			Model model = new Model();
			model.Filename.Value = "Models\\cone";
			model.Color.Value = color;
			model.IsInstanced.Value = false;
			model.Add(new Binding<Vector3, float>(model.Scale, x => new Vector3(1.0f, 1.0f, x), offset));
			model.Editable = false;
			model.Serialize = false;

			entity.Add("EditorModel2", model);

			model.Add(new Binding<Matrix>(model.Transform, entity.Get<EnemyBase>().Transform));
		}

		public override void Awake()
		{
			base.Awake();
			this.Add(new Binding<Vector3>(this.Position, () => Vector3.Transform(new Vector3(0.0f, 0.0f, this.Offset), this.Transform), this.Offset, this.Transform));
			if (!this.main.EditorEnabled)
			{
				Action setupMap = delegate()
				{
					Entity entity = this.Voxel.Value.Target;
					if (entity == null || !entity.Active)
					{
						this.Delete.Execute();
						return;
					}
					else
					{
						if (this.cellEmptiedBinding != null)
							this.Remove(this.cellEmptiedBinding);
						this.cellEmptiedBinding = new CommandBinding<IEnumerable<Voxel.Coord>, Voxel>(entity.Get<Voxel>().CellsEmptied, delegate(IEnumerable<Voxel.Coord> coords, Voxel newMap)
						{
							if (!this.IsValid)
								this.Delete.Execute();
						});
						this.Add(this.cellEmptiedBinding);
					}
				};
				this.Add(new NotifyBinding(setupMap, this.Voxel));
				if (this.Voxel.Value.Target != null)
					setupMap();

				this.main.AddComponent(new PostInitialization
				{
					delegate()
					{
						if (this.Voxel.Value.Target == null || !this.Voxel.Value.Target.Active)
						{
							this.BaseBoxes.Clear();

							bool found = false;
							foreach (Voxel m in Lemma.Components.Voxel.Voxels)
							{
								Voxel.Box box = m.GetBox(this.Position);
								if (box != null && box.Type.ID == Components.Voxel.t.InfectedCritical)
								{
									foreach (Voxel.Box b in m.GetContiguousByType(new[] { box }))
										this.BaseBoxes.Add(b);
									this.Voxel.Value = m.Entity;
									found = true;
									break;
								}
							}
							if (!found)
								this.Delete.Execute();
						}
					}
				});
			}
		}
	}
}
