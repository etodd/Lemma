using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Lemma.Util;
using Lemma.Factories;
using ComponentBind;

namespace Lemma.Components
{
	public class Editor : Component<Main>, IUpdateableComponent
	{
		static Editor()
		{
			Factory<Main>.DefaultEditorComponents = delegate(Factory<Main> factory, Entity entity, Main main)
			{
				Transform transform = entity.Get<Transform>();
				if (transform == null)
					return;

				Model model = new Model();
				model.Filename.Value = "Models\\sphere";
				model.Color.Value = new Vector3(factory.Color.X, factory.Color.Y, factory.Color.Z);
				model.IsInstanced.Value = false;
				model.Scale.Value = new Vector3(0.5f);
				model.Editable = false;
				model.Serialize = false;

				entity.Add("EditorModel", model);

				model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
			};
		}

		public Property<Vector3> Position = new Property<Vector3>();
		public Property<Matrix> Orientation = new Property<Matrix>();
		public Property<bool> MovementEnabled = new Property<bool>();
		public ListProperty<Entity> SelectedEntities = new ListProperty<Entity>();
		public Property<Transform> SelectedTransform = new Property<Transform>();
		public EditorProperty<string> Brush = new EditorProperty<string>();
		public EditorProperty<Voxel.Coord> Jitter = new EditorProperty<Voxel.Coord>();
		public EditorProperty<Voxel.Coord> JitterOctave = new EditorProperty<Voxel.Coord> { Value = new Voxel.Coord { X = 1, Y = 1, Z = 1 } };
		public EditorProperty<float> JitterOctaveMultiplier = new EditorProperty<float> { Value = 10.0f };
		public EditorProperty<int> BrushSize = new EditorProperty<int>();
		public EditorProperty<string> MapFile = new EditorProperty<string>();
		public EditorProperty<string> StartSpawnPoint = new EditorProperty<string>();
		public Property<bool> NeedsSave = new Property<bool>();

		// Input properties
		public Property<bool> VoxelEditMode = new Property<bool>();
		public Property<Vector2> Movement = new Property<Vector2>();
		public Property<Vector2> Mouse = new Property<Vector2>();
		public Property<bool> Up = new Property<bool>();
		public Property<bool> Down = new Property<bool>();
		public Property<bool> SpeedMode = new Property<bool>();
		public Property<bool> Extend = new Property<bool>();
		public Property<bool> Empty = new Property<bool>();
		public Property<bool> Fill = new Property<bool>();
		public Property<bool> EditSelection = new Property<bool>();
		public Property<Voxel.Coord> VoxelSelectionStart = new Property<Voxel.Coord>();
		public Property<Voxel.Coord> VoxelSelectionEnd = new Property<Voxel.Coord>();
		public Property<bool> VoxelSelectionActive = new Property<bool>();

		public Property<float> CameraDistance = new Property<float> { Value = 10.0f };

		public Command<string> Spawn = new Command<string>();
		public Command Save = new Command();
		public Command Duplicate = new Command();
		public Command DeleteSelected = new Command();

		public enum TransformModes { None, Translate, Rotate };
		public Property<TransformModes> TransformMode = new Property<TransformModes> { Value = TransformModes.None };
		public enum TransformAxes { All, X, Y, Z };
		public Property<TransformAxes> TransformAxis = new Property<TransformAxes> { Value = TransformAxes.All };
		protected Vector3 transformCenter;
		protected Vector2 originalTransformMouse;
		protected List<Matrix> offsetTransforms = new List<Matrix>();

		public Command VoxelDuplicate = new Command();
		public Command VoxelCopy = new Command();
		public Command VoxelPaste = new Command();
		public Command StartVoxelTranslation = new Command();
		public Command StartTranslation = new Command();
		public Command StartRotation = new Command();
		public Command CommitTransform = new Command();
		public Command RevertTransform = new Command();
		public Command PropagateMaterial = new Command();
		public Command IntersectMaterial = new Command();
		public Command PropagateMaterialAll = new Command();
		public Command PropagateMaterialBox = new Command();
		public Command SampleMaterial = new Command();
		public Command DeleteMaterial = new Command();
		public Command DeleteMaterialAll = new Command();

		private Voxel.Coord originalSelectionStart;
		private Voxel.Coord originalSelectionEnd;
		private Voxel.Coord originalSelectionCoord;
		private bool voxelDuplicate;

		private Voxel.Snapshot mapState;
		private Voxel.Coord selectionStart;
		private Voxel.Coord lastCoord;
		private Voxel.Coord coord;
		private ProceduralGenerator generator;
		private float movementInterval;

		public EditorProperty<Voxel.Coord> Coordinate = new EditorProperty<Voxel.Coord>(); // Readonly, for displaying to the UI

		private bool justCommitedOrRevertedVoxelOperation;

		public Editor()
		{
			this.BrushSize.Value = 1;
			this.MovementEnabled.Value = true;
			this.Orientation.Value = Matrix.Identity;
		}

		private void restoreVoxel(Voxel.Coord start, Voxel.Coord end, bool eraseOriginal, int offsetX = 0, int offsetY = 0, int offsetZ = 0)
		{
			Voxel map = this.SelectedEntities[0].Get<Voxel>();
			List<Voxel.Coord> removals = new List<Voxel.Coord>();
			for (int x = start.X; x < end.X; x++)
			{
				for (int y = start.Y; y < end.Y; y++)
				{
					for (int z = start.Z; z < end.Z; z++)
					{
						Voxel.State desiredState;
						if (eraseOriginal && x >= this.originalSelectionStart.X && x < this.originalSelectionEnd.X
							&& y >= this.originalSelectionStart.Y && y < this.originalSelectionEnd.Y
							&& z >= this.originalSelectionStart.Z && z < this.originalSelectionEnd.Z)
							desiredState = null;
						else
							desiredState = this.mapState[new Voxel.Coord { X = x + offsetX, Y = y + offsetY, Z = z + offsetZ }];
						if (map[x, y, z] != desiredState)
							removals.Add(new Voxel.Coord { X = x, Y = y, Z = z });
					}
				}
			}
			map.Empty(removals, true);

			for (int x = start.X; x < end.X; x++)
			{
				for (int y = start.Y; y < end.Y; y++)
				{
					for (int z = start.Z; z < end.Z; z++)
					{
						Voxel.State desiredState;
						if (eraseOriginal && x >= this.originalSelectionStart.X && x < this.originalSelectionEnd.X
							&& y >= this.originalSelectionStart.Y && y < this.originalSelectionEnd.Y
							&& z >= this.originalSelectionStart.Z && z < this.originalSelectionEnd.Z)
							desiredState = null;
						else
							desiredState = this.mapState[new Voxel.Coord { X = x + offsetX, Y = y + offsetY, Z = z + offsetZ }];
						if (desiredState != null && map[x, y, z] != desiredState)
							map.Fill(x, y, z, desiredState);
					}
				}
			}
			map.Regenerate();
		}

		private Voxel.State getBrush()
		{
			Voxel.State result = Voxel.StateList.FirstOrDefault(x => x.ID.ToString() == this.Brush);
			if (result.ID == Voxel.t.Empty)
				return Voxel.EmptyState;
			return result;
		}

		public override void Awake()
		{
			base.Awake();
			this.generator = this.Entity.GetOrCreate<ProceduralGenerator>();
			this.generator.Editable = false;

			this.Spawn.Action = delegate(string type)
			{
				if (Factory<Main>.Get(type) != null)
				{
					Entity entity = Factory<Main>.Get(type).CreateAndBind(this.main);
					Transform position = entity.Get<Transform>();
					if (position != null)
						position.Position.Value = this.Position;
					this.NeedsSave.Value = true;
					this.main.Add(entity);
					this.SelectedEntities.Clear();
					this.SelectedEntities.Add(entity);
					this.StartTranslation.Execute();
				}
			};

			this.Save.Action = delegate()
			{
				IO.MapLoader.Save(this.main, null, this.main.MapFile);
				this.NeedsSave.Value = false;
			};

			this.Duplicate.Action = delegate()
			{
				this.NeedsSave.Value = true;

				if (this.TransformMode.Value != TransformModes.None)
					this.CommitTransform.Execute();

				IEnumerable<Entity> entities = this.SelectedEntities.ToList();
				this.SelectedEntities.Clear();
				foreach (Entity entity in entities)
				{
					Entity copy = Factory<Main>.Duplicate(this.main, entity);
					this.main.Add(copy);
					this.SelectedEntities.Add(copy);
				}
				this.StartTranslation.Execute();
			};

			this.VoxelEditMode.Set = delegate(bool value)
			{
				bool oldValue = this.VoxelEditMode.InternalValue;
				this.VoxelEditMode.InternalValue = value;
				if (value && !oldValue)
				{
					this.Orientation.Value = this.SelectedEntities[0].Get<Transform>().Orientation;
					this.lastCoord = this.coord = this.SelectedEntities[0].Get<Voxel>().GetCoordinate(this.Position);
					this.Coordinate.Value = this.coord;
				}
				else if (!value && oldValue)
					this.Orientation.Value = Matrix.Identity;
			};

			this.SelectedEntities.ItemAdded += delegate(int index, Entity t)
			{
				t.EditorSelected.Value = true;
				this.VoxelSelectionEnd.Value = this.VoxelSelectionStart.Value;
				this.SelectedTransform.Value = null;
			};

			this.SelectedEntities.ItemRemoved += delegate(int index, Entity t)
			{
				t.EditorSelected.Value = false;
				this.VoxelSelectionEnd.Value = this.VoxelSelectionStart.Value;
				this.SelectedTransform.Value = null;
			};

			this.SelectedEntities.Clearing += delegate()
			{
				foreach (Entity e in this.SelectedEntities)
					e.EditorSelected.Value = false;
				this.VoxelSelectionEnd.Value = this.VoxelSelectionStart.Value;
				this.SelectedTransform.Value = null;
			};

			this.EditSelection.Set = delegate(bool value)
			{
				if (value && !this.EditSelection.InternalValue)
				{
					this.selectionStart = this.coord;
					this.VoxelSelectionStart.Value = this.coord;
					this.VoxelSelectionEnd.Value = this.coord.Move(1, 1, 1);
				}
				else if (!value && this.EditSelection.InternalValue)
				{
					if (this.VoxelSelectionEnd.Value.Equivalent(this.VoxelSelectionStart.Value.Move(1, 1, 1)))
						this.VoxelSelectionEnd.Value = this.VoxelSelectionStart.Value;
				}
				this.EditSelection.InternalValue = value;
			};

			this.VoxelCopy.Action = delegate()
			{
				if (this.VoxelEditMode && this.VoxelSelectionActive)
				{
					Voxel m = this.SelectedEntities[0].Get<Voxel>();
					this.originalSelectionStart = this.VoxelSelectionStart;
					this.originalSelectionEnd = this.VoxelSelectionEnd;
					this.originalSelectionCoord = this.coord;
					if (this.mapState != null)
						this.mapState.Free();
					this.mapState = new Voxel.Snapshot(m, this.originalSelectionStart, this.originalSelectionEnd);
					this.voxelDuplicate = false;
				}
			};

			this.VoxelPaste.Action = delegate()
			{
				if (this.VoxelEditMode && this.mapState != null)
				{
					Voxel m = this.SelectedEntities[0].Get<Voxel>();
					Voxel.Coord newSelectionStart = this.coord.Plus(this.originalSelectionStart.Minus(this.originalSelectionCoord));
					this.VoxelSelectionStart.Value = newSelectionStart;
					this.VoxelSelectionEnd.Value = this.coord.Plus(this.originalSelectionEnd.Minus(this.originalSelectionCoord));

					this.mapState.Add(this.VoxelSelectionStart, this.VoxelSelectionEnd);

					Voxel.Coord offset = this.originalSelectionStart.Minus(newSelectionStart);
					this.restoreVoxel(newSelectionStart, this.VoxelSelectionEnd, false, offset.X, offset.Y, offset.Z);
				}
			};

			this.StartVoxelTranslation.Action = delegate()
			{
				if (this.VoxelEditMode && this.VoxelSelectionActive)
				{
					this.VoxelCopy.Execute();
					this.TransformMode.Value = TransformModes.Translate;
				}
			};

			this.VoxelDuplicate.Action = delegate()
			{
				if (this.VoxelEditMode && this.VoxelSelectionActive)
				{
					this.StartVoxelTranslation.Execute();
					this.voxelDuplicate = true;
				}
			};

			this.PropagateMaterial.Action = delegate()
			{
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.Coord startSelection = this.VoxelSelectionStart;
				Voxel.Coord endSelection = this.VoxelSelectionEnd;
				bool selectionActive = this.VoxelSelectionActive;

				Voxel.State material = this.getBrush();
				if (material != Voxel.EmptyState)
				{
					if (material == selectedBox.Type)
						return;

					IEnumerable<Voxel.Coord> coordEnumerable;
					if (selectionActive)
						coordEnumerable = m.GetContiguousByType(new Voxel.Box[] { selectedBox }).SelectMany(x => x.GetCoords().Where(y => y.Between(startSelection, endSelection)));
					else
						coordEnumerable = m.GetContiguousByType(new Voxel.Box[] { selectedBox }).SelectMany(x => x.GetCoords());

					List<Voxel.Coord> coords = coordEnumerable.ToList();
					m.Empty(coords, true);
					foreach (Voxel.Coord c in coords)
						m.Fill(c, material);
					m.Regenerate();
				}
			};

			this.IntersectMaterial.Action = delegate()
			{
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.Coord startSelection = this.VoxelSelectionStart;
				Voxel.Coord endSelection = this.VoxelSelectionEnd;
				bool selectionActive = this.VoxelSelectionActive;

				IEnumerable<Voxel.Coord> coordEnumerable;
				if (selectionActive)
					coordEnumerable = m.GetContiguousByType(new Voxel.Box[] { selectedBox }).SelectMany(x => x.GetCoords().Where(y => !y.Between(startSelection, endSelection)));
				else
					coordEnumerable = m.GetContiguousByType(new Voxel.Box[] { selectedBox }).SelectMany(x => x.GetCoords().Where(y => (m.GetRelativePosition(this.coord) - m.GetRelativePosition(y)).Length() > this.BrushSize));

				List<Voxel.Coord> coords = coordEnumerable.ToList();
				m.Empty(coords, true);
				m.Regenerate();
			};

			// Propagate to all cells of a certain type, including non-contiguous ones
			this.PropagateMaterialAll.Action = delegate()
			{
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.State oldMaterial = selectedBox.Type;

				Voxel.State material = this.getBrush();
				if (material != Voxel.EmptyState)
				{
					if (material == oldMaterial)
						return;
					List<Voxel.Coord> coords = m.Chunks.SelectMany(x => x.Boxes).Where(x => x.Type == oldMaterial).SelectMany(x => x.GetCoords()).ToList();
					m.Empty(coords, true);
					foreach (Voxel.Coord c in coords)
						m.Fill(c, material);
					m.Regenerate();
				}
			};

			this.PropagateMaterialBox.Action = delegate()
			{
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.Coord startSelection = this.VoxelSelectionStart;
				Voxel.Coord endSelection = this.VoxelSelectionEnd;
				bool selectionActive = this.VoxelSelectionActive;

				Voxel.State material = this.getBrush();
				if (material != Voxel.EmptyState)
				{
					if (material == selectedBox.Type)
						return;

					IEnumerable<Voxel.Coord> coordEnumerable;
					if (selectionActive)
						coordEnumerable = selectedBox.GetCoords().Where(y => y.Between(startSelection, endSelection));
					else
						coordEnumerable = selectedBox.GetCoords();

					List<Voxel.Coord> coords = coordEnumerable.ToList();
					m.Empty(coords, true);
					foreach (Voxel.Coord c in coords)
						m.Fill(c, material);
					m.Regenerate();
				}
			};

			this.SampleMaterial.Action = delegate()
			{
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				this.Brush.Value = selectedBox.Type.ID.ToString();
			};

			this.DeleteMaterial.Action = delegate()
			{
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.Coord startSelection = this.VoxelSelectionStart;
				Voxel.Coord endSelection = this.VoxelSelectionEnd;
				bool selectionActive = this.VoxelSelectionActive;

				IEnumerable<Voxel.Coord> coordEnumerable;
				if (selectionActive)
					coordEnumerable = m.GetContiguousByType(new Voxel.Box[] { selectedBox }).SelectMany(x => x.GetCoords().Where(y => y.Between(startSelection, endSelection)));
				else
					coordEnumerable = m.GetContiguousByType(new Voxel.Box[] { selectedBox }).SelectMany(x => x.GetCoords());

				List<Voxel.Coord> coords = coordEnumerable.ToList();
				m.Empty(coords, true);
				m.Regenerate();
			};

			// Delete all cells of a certain type in the current map, including non-contiguous ones
			this.DeleteMaterialAll.Action = delegate()
			{
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.State material = selectedBox.Type;

				m.Empty(m.Chunks.SelectMany(x => x.Boxes).Where(x => x.Type == material).SelectMany(x => x.GetCoords()).ToList(), true);
				m.Regenerate();
			};

			Action<TransformModes> startTransform = delegate(TransformModes mode)
			{
				this.TransformMode.Value = mode;
				this.TransformAxis.Value = TransformAxes.All;
				this.originalTransformMouse = this.Mouse;
				this.offsetTransforms.Clear();
				this.transformCenter = Vector3.Zero;
				if (this.SelectedTransform.Value != null)
				{
					this.offsetTransforms.Add(this.SelectedTransform.Value.Matrix);
					this.transformCenter = this.SelectedTransform.Value.Position;
				}
				else
				{
					int entityCount = 0;
					foreach (Entity entity in this.SelectedEntities)
					{
						Transform transform = entity.Get<Transform>();
						if (transform != null)
						{
							this.offsetTransforms.Add(transform.Matrix);
							this.transformCenter += transform.Position;
							entityCount++;
						}
					}
					this.transformCenter /= (float)entityCount;
				}
			};

			this.StartTranslation.Action = delegate()
			{
				startTransform(TransformModes.Translate);
			};

			this.StartRotation.Action = delegate()
			{
				startTransform(TransformModes.Rotate);
			};

			this.CommitTransform.Action = delegate()
			{
				this.NeedsSave.Value = true;
				this.TransformMode.Value = TransformModes.None;
				this.TransformAxis.Value = TransformAxes.All;
				if (this.VoxelEditMode)
					this.justCommitedOrRevertedVoxelOperation = true;
				this.offsetTransforms.Clear();
			};

			this.RevertTransform.Action = delegate()
			{
				this.TransformMode.Value = TransformModes.None;
				if (this.VoxelEditMode)
				{
					this.restoreVoxel(this.VoxelSelectionStart, this.VoxelSelectionEnd, false);
					this.VoxelSelectionStart.Value = this.originalSelectionStart;
					this.VoxelSelectionEnd.Value = this.originalSelectionEnd;
					this.restoreVoxel(this.VoxelSelectionStart, this.VoxelSelectionEnd, false);
					this.justCommitedOrRevertedVoxelOperation = true;
				}
				else
				{
					this.TransformAxis.Value = TransformAxes.All;
					if (this.SelectedTransform.Value != null)
						this.SelectedTransform.Value.Matrix.Value = this.offsetTransforms[0];
					else
					{
						int i = 0;
						foreach (Entity entity in this.SelectedEntities)
						{
							Transform transform = entity.Get<Transform>();
							if (transform != null)
							{
								transform.Matrix.Value = this.offsetTransforms[i];
								i++;
							}
						}
					}
					this.offsetTransforms.Clear();
				}
			};

			this.DeleteSelected.Action = delegate()
			{
				this.NeedsSave.Value = true;
				this.TransformMode.Value = TransformModes.None;
				this.TransformAxis.Value = TransformAxes.All;
				this.offsetTransforms.Clear();
				foreach (Entity entity in this.SelectedEntities)
				{
					if (entity.EditorCanDelete)
						entity.Delete.Execute();
				}
				this.SelectedEntities.Clear();
			};

			this.Add(new Binding<bool>(this.VoxelSelectionActive, delegate()
			{
				if (!this.VoxelEditMode)
					return false;
				Voxel.Coord start = this.VoxelSelectionStart, end = this.VoxelSelectionEnd;
				return start.X != end.X && start.Y != end.Y && start.Z != end.Z;
			}, this.VoxelEditMode, this.VoxelSelectionStart, this.VoxelSelectionEnd));
		}

		public void Update(float elapsedTime)
		{
			Vector3 movementDir = new Vector3();
			if (this.MovementEnabled)
			{
				Vector2 controller = this.main.Camera.GetWorldSpaceControllerCoordinates(this.Movement);
				movementDir = new Vector3(controller.X, 0, controller.Y);
				if (this.Up)
					movementDir = movementDir.SetComponent(Direction.PositiveY, 1.0f);
				else if (this.Down)
					movementDir = movementDir.SetComponent(Direction.NegativeY, 1.0f);
					
				if (this.VoxelEditMode)
				{
					bool moving = movementDir.LengthSquared() > 0.0f;

					// When the user lets go of the key, reset the timer
					// That way they can hit the key faster than the 0.1 sec interval
					if (!moving)
						this.movementInterval = 0.5f; 

					if (this.movementInterval > (this.SpeedMode ? 0.5f : 1.0f) / this.CameraDistance)
					{
						if (moving)
							this.movementInterval = 0.0f;
						if (movementDir.LengthSquared() > 0.0f)
						{
							Voxel map = this.SelectedEntities[0].Get<Voxel>();
							Direction relativeDir = map.GetRelativeDirection(movementDir);
							this.coord = this.coord.Move(relativeDir);
							this.Coordinate.Value = this.coord;
							if (this.EditSelection)
							{
								this.VoxelSelectionStart.Value = new Voxel.Coord
								{
									X = Math.Min(this.selectionStart.X, this.coord.X),
									Y = Math.Min(this.selectionStart.Y, this.coord.Y),
									Z = Math.Min(this.selectionStart.Z, this.coord.Z),
								};
								this.VoxelSelectionEnd.Value = new Voxel.Coord
								{
									X = Math.Max(this.selectionStart.X, this.coord.X) + 1,
									Y = Math.Max(this.selectionStart.Y, this.coord.Y) + 1,
									Z = Math.Max(this.selectionStart.Z, this.coord.Z) + 1,
								};
							}
							else if (this.TransformMode.Value == TransformModes.Translate)
							{
								this.NeedsSave.Value = true;

								this.restoreVoxel(this.VoxelSelectionStart, this.VoxelSelectionEnd, !this.voxelDuplicate);

								Voxel.Coord newSelectionStart = this.VoxelSelectionStart.Value.Move(relativeDir);
								this.VoxelSelectionStart.Value = newSelectionStart;
								this.VoxelSelectionEnd.Value = this.VoxelSelectionEnd.Value.Move(relativeDir);

								this.mapState.Add(this.VoxelSelectionStart, this.VoxelSelectionEnd);

								Voxel.Coord offset = this.originalSelectionStart.Minus(newSelectionStart);
								this.restoreVoxel(newSelectionStart, this.VoxelSelectionEnd, false, offset.X, offset.Y, offset.Z);
							}
							this.Position.Value = map.GetAbsolutePosition(this.coord);
						}
					}
					this.movementInterval += elapsedTime;
				}
				else
					this.Position.Value = this.Position.Value + movementDir * (this.SpeedMode ? 5.0f : 2.5f) * this.CameraDistance * elapsedTime;
			}

			if (this.VoxelEditMode)
			{
				if (!this.Fill && !this.Empty)
					this.justCommitedOrRevertedVoxelOperation = false;

				Voxel map = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Coord coord = map.GetCoordinate(this.Position);
				if (this.TransformMode.Value == TransformModes.None && (this.Fill || this.Empty || this.Extend) && !this.justCommitedOrRevertedVoxelOperation)
				{
					this.NeedsSave.Value = true;
					if (this.Fill)
					{
						Voxel.State material = this.getBrush();
						if (material != Voxel.EmptyState)
						{
							if (this.VoxelSelectionActive)
							{
								if (this.Jitter.Value.Equivalent(new Voxel.Coord { X = 0, Y = 0, Z = 0 }) || this.BrushSize <= 1)
									map.Fill(this.VoxelSelectionStart, this.VoxelSelectionEnd, material);
								else
								{
									Voxel.Coord start = this.VoxelSelectionStart;
									Voxel.Coord end = this.VoxelSelectionEnd;
									int size = this.BrushSize;
									int halfSize = size / 2;
									for (int x = start.X + size - 1; x < end.X - size + 1; x += halfSize)
									{
										for (int y = start.Y + size - 1; y < end.Y - size + 1; y += halfSize)
										{
											for (int z = start.Z + size - 1; z < end.Z - size + 1; z += halfSize)
											{
												this.brushStroke(map, new Voxel.Coord { X = x, Y = y, Z = z }, size, material);
											}
										}
									}
								}
							}
							else
								this.brushStroke(map, coord, this.BrushSize, material);
						}
					}
					else if (this.Empty)
					{
						if (this.VoxelSelectionActive)
						{
							if (this.Jitter.Value.Equivalent(new Voxel.Coord { X = 0, Y = 0, Z = 0 }) || this.BrushSize <= 1)
								map.Empty(this.VoxelSelectionStart, this.VoxelSelectionEnd, true);
							else
							{
								Voxel.Coord start = this.VoxelSelectionStart;
								Voxel.Coord end = this.VoxelSelectionEnd;
								int size = this.BrushSize;
								int halfSize = size / 2;
								for (int x = start.X + size - 2; x < end.X - size; x += halfSize)
								{
									for (int y = start.Y + size - 2; y < end.Y - size; y += halfSize)
									{
										for (int z = start.Z + size - 2; z < end.Z - size; z += halfSize)
										{
											this.brushStroke(map, new Voxel.Coord { X = x, Y = y, Z = z }, size, new Voxel.State());
										}
									}
								}
							}
						}
						else
							this.brushStroke(map, coord, this.BrushSize, new Voxel.State());
					}

					if (this.Extend && !this.coord.Equivalent(this.lastCoord))
					{
						Direction dir = DirectionExtensions.GetDirectionFromVector(Vector3.TransformNormal(movementDir, Matrix.Invert(map.Transform)));
						Voxel.Box box = map.GetBox(this.lastCoord);
						bool grow = map.GetBox(this.coord) != box;
						if (box != null)
						{
							List<Voxel.Coord> removals = new List<Voxel.Coord>();
							if (dir.IsParallel(Direction.PositiveX))
							{
								for (int y = box.Y; y < box.Y + box.Height; y++)
								{
									for (int z = box.Z; z < box.Z + box.Depth; z++)
									{
										if (grow)
											map.Fill(this.coord.X, y, z, box.Type);
										else
											removals.Add(map.GetCoordinate(this.lastCoord.X, y, z));
									}
								}
							}
							if (dir.IsParallel(Direction.PositiveY))
							{
								for (int x = box.X; x < box.X + box.Width; x++)
								{
									for (int z = box.Z; z < box.Z + box.Depth; z++)
									{
										if (grow)
											map.Fill(x, this.coord.Y, z, box.Type);
										else
											removals.Add(map.GetCoordinate(x, this.lastCoord.Y, z));
									}
								}
							}
							if (dir.IsParallel(Direction.PositiveZ))
							{
								for (int x = box.X; x < box.X + box.Width; x++)
								{
									for (int y = box.Y; y < box.Y + box.Height; y++)
									{
										if (grow)
											map.Fill(x, y, this.coord.Z, box.Type);
										else
											removals.Add(map.GetCoordinate(x, y, this.lastCoord.Z));
									}
								}
							}
							map.Empty(removals, true);
						}
					}
					map.Regenerate();
				}
				this.lastCoord = this.coord;
			}
			else if (this.TransformMode.Value == TransformModes.Translate)
			{
				// Translate entities
				this.NeedsSave.Value = true;
				float rayLength = (this.transformCenter - this.main.Camera.Position.Value).Length();
				Vector2 mouseOffset = this.Mouse - this.originalTransformMouse;
				Vector3 offset = ((this.main.Camera.Right.Value * mouseOffset.X * rayLength) + (this.main.Camera.Up.Value * -mouseOffset.Y * rayLength)) * 0.0025f;
				switch (this.TransformAxis.Value)
				{
					case TransformAxes.X:
						offset.Y = offset.Z = 0.0f;
						break;
					case TransformAxes.Y:
						offset.X = offset.Z = 0.0f;
						break;
					case TransformAxes.Z:
						offset.X = offset.Y = 0.0f;
						break;
				}
				if (this.SelectedTransform.Value != null)
					this.SelectedTransform.Value.Position.Value = this.offsetTransforms[0].Translation + offset;
				else
				{
					int i = 0;
					foreach (Entity entity in this.SelectedEntities)
					{
						Transform transform = entity.Get<Transform>();
						if (transform != null)
						{
							Matrix originalTransform = this.offsetTransforms[i];
							transform.Position.Value = originalTransform.Translation + offset;
							i++;
						}
					}
				}
			}
			else if (this.TransformMode.Value == TransformModes.Rotate)
			{
				// Rotate entities
				this.NeedsSave.Value = true;
				Vector3 screenSpaceCenter = this.main.GraphicsDevice.Viewport.Project(this.transformCenter, this.main.Camera.Projection, this.main.Camera.View, Matrix.Identity);
				Vector2 originalOffset = new Vector2(this.originalTransformMouse.X - screenSpaceCenter.X, this.originalTransformMouse.Y - screenSpaceCenter.Y);
				float originalAngle = (float)Math.Atan2(originalOffset.Y, originalOffset.X);
				Vector2 newOffset = new Vector2(this.Mouse.Value.X - screenSpaceCenter.X, this.Mouse.Value.Y - screenSpaceCenter.Y);
				float newAngle = (float)Math.Atan2(newOffset.Y, newOffset.X);
				Vector3 axis = this.main.Camera.Forward;
				switch (this.TransformAxis.Value)
				{
					case TransformAxes.X:
						axis = Vector3.Right;
						break;
					case TransformAxes.Y:
						axis = Vector3.Up;
						break;
					case TransformAxes.Z:
						axis = Vector3.Forward;
						break;
				}
				if (this.SelectedTransform.Value != null)
				{
					Matrix originalTransform = this.offsetTransforms[0];
					originalTransform.Translation -= this.transformCenter;
					originalTransform *= Matrix.CreateFromAxisAngle(axis, newAngle - originalAngle);
					originalTransform.Translation += this.transformCenter;
					this.SelectedTransform.Value.Matrix.Value = originalTransform;
				}
				else
				{
					int i = 0;
					foreach (Entity entity in this.SelectedEntities)
					{
						Transform transform = entity.Get<Transform>();
						if (transform != null)
						{
							Matrix originalTransform = this.offsetTransforms[i];
							originalTransform.Translation -= this.transformCenter;
							originalTransform *= Matrix.CreateFromAxisAngle(axis, newAngle - originalAngle);
							originalTransform.Translation += this.transformCenter;
							transform.Matrix.Value = originalTransform;
							i++;
						}
					}
				}
			}
		}

		protected Voxel.Coord jitter(Voxel map, Voxel.Coord coord)
		{
			Voxel.Coord octave = this.JitterOctave;
			Voxel.Coord jitter = this.Jitter;
			Voxel.Coord sample = coord.Clone();
			sample.X *= octave.X;
			sample.Y *= octave.Y;
			sample.Z *= octave.Z;
			coord.X += (int)Math.Round(this.generator.Sample(map, sample.Move(0, 0, map.ChunkSize * 2), this.JitterOctaveMultiplier) * jitter.X);
			coord.Y += (int)Math.Round(this.generator.Sample(map, sample.Move(map.ChunkSize * 2, 0, 0), this.JitterOctaveMultiplier) * jitter.Y);
			coord.Z += (int)Math.Round(this.generator.Sample(map, sample.Move(0, map.ChunkSize * 2, 0), this.JitterOctaveMultiplier) * jitter.Z);
			return coord;
		}

		protected void brushStroke(Voxel map, Voxel.Coord center, int brushSize, Func<Voxel.Coord, Voxel.State> function, bool fill = true, bool empty = true)
		{
			if (brushSize > 1)
				center = this.jitter(map, center);

			Vector3 pos = map.GetRelativePosition(center);
			List<Voxel.Coord> coords = new List<Voxel.Coord>();
			for (Voxel.Coord x = center.Move(Direction.NegativeX, this.BrushSize - 1); x.X < center.X + this.BrushSize; x.X++)
			{
				for (Voxel.Coord y = x.Move(Direction.NegativeY, this.BrushSize - 1); y.Y < center.Y + this.BrushSize; y.Y++)
				{
					for (Voxel.Coord z = y.Move(Direction.NegativeZ, this.BrushSize - 1); z.Z < center.Z + this.BrushSize; z.Z++)
					{
						if ((pos - map.GetRelativePosition(z)).Length() <= this.BrushSize)
							coords.Add(new Voxel.Coord { X = z.X, Y = z.Y, Z = z.Z, Data = function(z) });
					}
				}
			}

			if (empty)
				map.Empty(coords.Where(x => x.Data.ID == 0), true);

			if (fill)
			{
				foreach (Voxel.Coord coord in coords)
					map.Fill(coord, coord.Data);
			}
		}

		protected void brushStroke(Voxel map, Voxel.Coord center, int brushSize, Voxel.State state)
		{
			if (brushSize > 1)
				center = this.jitter(map, center);

			Vector3 pos = map.GetRelativePosition(center);
			List<Voxel.Coord> coords = new List<Voxel.Coord>();
			for (Voxel.Coord x = center.Move(Direction.NegativeX, this.BrushSize - 1); x.X < center.X + this.BrushSize; x.X++)
			{
				for (Voxel.Coord y = x.Move(Direction.NegativeY, this.BrushSize - 1); y.Y < center.Y + this.BrushSize; y.Y++)
				{
					for (Voxel.Coord z = y.Move(Direction.NegativeZ, this.BrushSize - 1); z.Z < center.Z + this.BrushSize; z.Z++)
					{
						if ((pos - map.GetRelativePosition(z)).Length() <= this.BrushSize)
							coords.Add(z);
					}
				}
			}
			if (state.ID == 0)
				map.Empty(coords, true);
			else
			{
				foreach (Voxel.Coord coord in coords)
					map.Fill(coord, state);
			}
		}
	}
}
