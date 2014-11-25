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
		public static Property<bool> EditorModelsVisible = new Property<bool> { Value = true };
		public static void SetupDefaultEditorComponents()
		{
			Factory<Main>.DefaultEditorComponents = delegate(Factory<Main> factory, Entity entity, Main main)
			{
				Transform transform = entity.Get<Transform>("Transform");
				if (transform == null)
					return;

				ModelAlpha model = new ModelAlpha();
				model.Filename.Value = "AlphaModels\\sphere";
				model.Color.Value = new Vector3(factory.Color.X, factory.Color.Y, factory.Color.Z);
				model.IsInstanced.Value = false;
				model.Scale.Value = new Vector3(0.5f);
				model.Serialize = false;

				entity.Add("EditorModel", model);

				model.Add(new Binding<bool>(model.Enabled, Editor.EditorModelsVisible));
				model.Add(new Binding<Matrix, Vector3>(model.Transform, x => Matrix.CreateTranslation(x), transform.Position));
			};

			Factory<Main>.GlobalEditorComponents = delegate(Entity entity, Main main)
			{
				Transform transform = entity.Get<Transform>("Transform");
				if (transform != null)
				{
					LineDrawer connectionLines = new LineDrawer { Serialize = false };
					connectionLines.Add(new Binding<bool>(connectionLines.Enabled, entity.EditorSelected));

					Color connectionLineColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
					ListBinding<LineDrawer.Line, Entity.CommandLink> connectionBinding = new ListBinding<LineDrawer.Line, Entity.CommandLink>(connectionLines.Lines, entity.LinkedCommands, delegate(Entity.CommandLink link)
					{
						return new LineDrawer.Line
						{
							A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(transform.Position, connectionLineColor),
							B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(link.TargetEntity.Target.Get<Transform>("Transform").Position, connectionLineColor)
						};
					}, x => x.TargetEntity.Target != null && x.TargetEntity.Target.Active);
					entity.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, entity.EditorSelected));
					entity.Add(new NotifyBinding(delegate() { connectionBinding.OnChanged(null); }, () => entity.EditorSelected, transform.Position));
					connectionLines.Add(connectionBinding);
					entity.Add(connectionLines);
				}
			};
		}

		public Property<Vector3> Position = new Property<Vector3>();
		public Property<Quaternion> Orientation = new Property<Quaternion>();
		public Property<bool> MovementEnabled = new Property<bool>();
		public ListProperty<Entity> SelectedEntities = new ListProperty<Entity>();
		public Property<Transform> SelectedTransform = new Property<Transform>();
		public Property<Voxel.t> Brush = new Property<Voxel.t>();
		public Property<Voxel.Coord> Jitter = new Property<Voxel.Coord>();
		public Property<float> JitterOctave = new Property<float> { Value = 10.0f };
		public Property<int> BrushSize = new Property<int>();
		public Property<bool> NeedsSave = new Property<bool>();

		public Func<bool> EnableCommands = () => true;

		// Input properties
		public Property<bool> VoxelEditMode = new Property<bool>();
		public Property<Vector2> Movement = new Property<Vector2>();
		public Property<Vector2> Mouse = new Property<Vector2>();
		public Property<bool> Up = new Property<bool>();
		public Property<bool> Down = new Property<bool>();
		public Property<bool> SpeedMode = new Property<bool>();
		public Property<bool> Extend = new Property<bool>();
		public Command StartFill = new Command();
		public Command StartEmpty = new Command();
		public Command StopFill = new Command();
		public Command StopEmpty = new Command();
		public Property<bool> EditSelection = new Property<bool>();
		public Property<Voxel.Coord> VoxelSelectionStart = new Property<Voxel.Coord>();
		public Property<Voxel.Coord> VoxelSelectionEnd = new Property<Voxel.Coord>();
		public Property<bool> VoxelSelectionActive = new Property<bool>();
		public Command VoxelRotateX = new Command();
		public Command VoxelRotateY = new Command();
		public Command VoxelRotateZ = new Command();

		public Property<bool> EnableCameraDistanceScroll = new Property<bool> { Value = true };
		public Property<float> CameraDistance = new Property<float> { Value = 10.0f };

		public Command<string> Spawn = new Command<string>();
		public Command Save = new Command();
		public Command Duplicate = new Command();
		public Command DeleteSelected = new Command();

		public enum TransformModes { None, Translate, Rotate };
		public Property<TransformModes> TransformMode = new Property<TransformModes> { Value = TransformModes.None };
		public enum TransformAxes { All, X, Y, Z, LocalX, LocalY, LocalZ };
		public Property<TransformAxes> TransformAxis = new Property<TransformAxes> { Value = TransformAxes.All };
		public enum BrushShapes { Sphere, Cube }
		public Property<BrushShapes> BrushShape = new Property<BrushShapes> { Value = BrushShapes.Sphere };
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

		public enum FillMode { None, Fill, Empty, ForceFill }
		public Property<FillMode> Fill = new Property<FillMode>();

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

		public Property<Voxel.Coord> Coordinate = new Property<Voxel.Coord>(); // Readonly, for displaying to the UI
		public Property<Voxel.Coord> VoxelSelectionSize = new Property<Voxel.Coord>(); // Readonly, for displaying to the UI

		private bool justCommitedOrRevertedVoxelOperation;

		public Editor()
		{
			this.BrushSize.Value = 1;
			this.MovementEnabled.Value = true;
			this.Orientation.Value = Quaternion.Identity;
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

		private void restoreVoxel(Voxel.Coord start, Voxel.Coord end, Direction dx, Direction dy, Direction dz)
		{
			Voxel map = this.SelectedEntities[0].Get<Voxel>();
			List<Voxel.Coord> removals = new List<Voxel.Coord>();

			for (int x = start.X; x < end.X; x++)
			{
				for (int y = start.Y; y < end.Y; y++)
				{
					for (int z = start.Z; z < end.Z; z++)
					{
						Voxel.Coord c = new Voxel.Coord { X = x, Y = y, Z = z };
						Voxel.State desiredState = this.mapState[c];
						if (desiredState != null)
						{
							c = c.Minus(this.coord).Reorient(dx, dy, dz).Plus(this.coord);
							map.Fill(c.X, c.Y, c.Z, desiredState);
						}
					}
				}
			}
			map.Regenerate();
		}

		private Voxel.State getBrush()
		{
			return Voxel.States.All[this.Brush];
		}

		public override void Awake()
		{
			base.Awake();
			this.generator = this.Entity.GetOrCreate<ProceduralGenerator>();

			this.Spawn.Action = delegate(string type)
			{
				if (Factory<Main>.Get(type) != null)
				{
					Entity entity = Factory<Main>.Get(type).CreateAndBind(this.main);
					Transform position = entity.Get<Transform>("Transform");
					if (position != null)
						position.Position.Value = this.Position;
					this.NeedsSave.Value = true;
					this.main.Add(entity);
					this.SelectedEntities.Clear();
					this.SelectedEntities.Add(entity);
				}
			};

			this.Save.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				IO.MapLoader.Save(this.main, null, this.main.MapFile);
				this.NeedsSave.Value = false;
			};

			this.Duplicate.Action = delegate()
			{
				if (!this.EnableCommands())
					return;

				this.NeedsSave.Value = true;

				if (this.TransformMode.Value != TransformModes.None)
					this.CommitTransform.Execute();

				IEnumerable<Entity> entities = this.SelectedEntities.ToList();
				this.SelectedEntities.Clear();
				foreach (Entity entity in entities)
				{
					if (Factory<Main>.Get(entity.Type).EditorCanSpawn)
					{
						Entity copy = Factory<Main>.Duplicate(this.main, entity);
						this.main.Add(copy);
						this.SelectedEntities.Add(copy);
					}
				}
				this.StartTranslation.Execute();
			};

			this.Add(new ChangeBinding<bool>(this.VoxelEditMode, delegate(bool old, bool value)
			{
				if (value && !old)
				{
					this.Orientation.Value = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(this.SelectedEntities[0].Get<Voxel>().Transform));
					this.lastCoord = this.coord = this.SelectedEntities[0].Get<Voxel>().GetCoordinate(this.Position);
					this.Coordinate.Value = this.coord;
				}
				else if (!value && old)
					this.Orientation.Value = Quaternion.Identity;
			}));

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

			this.Add(new ChangeBinding<bool>(this.EditSelection, delegate(bool old, bool value)
			{
				if (value && !old)
				{
					this.selectionStart = this.coord;
					this.VoxelSelectionStart.Value = this.coord;
					this.VoxelSelectionEnd.Value = this.coord.Move(1, 1, 1);
				}
				else if (!value && old)
				{
					if (this.VoxelSelectionEnd.Value.Equivalent(this.VoxelSelectionStart.Value.Move(1, 1, 1)))
						this.VoxelSelectionEnd.Value = this.VoxelSelectionStart.Value;
				}
			}));

			this.VoxelCopy.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				if (this.VoxelEditMode && this.VoxelSelectionActive && this.TransformMode.Value == Editor.TransformModes.None)
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
				if (!this.EnableCommands())
					return;
				if (this.VoxelEditMode && this.mapState != null)
				{
					Voxel m = this.SelectedEntities[0].Get<Voxel>();
					Voxel.Coord newSelectionStart = this.coord.Plus(this.originalSelectionStart.Minus(this.originalSelectionCoord));
					this.VoxelSelectionStart.Value = newSelectionStart;
					this.VoxelSelectionEnd.Value = this.coord.Plus(this.originalSelectionEnd.Minus(this.originalSelectionCoord));

					Voxel.Coord offset = this.originalSelectionStart.Minus(newSelectionStart);
					this.restoreVoxel(newSelectionStart, this.VoxelSelectionEnd, false, offset.X, offset.Y, offset.Z);
				}
			};

			this.StartVoxelTranslation.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				if (this.VoxelEditMode && this.VoxelSelectionActive)
				{
					this.VoxelCopy.Execute();
					this.TransformMode.Value = TransformModes.Translate;
				}
			};

			this.VoxelDuplicate.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				if (this.VoxelEditMode && this.VoxelSelectionActive)
				{
					this.StartVoxelTranslation.Execute();
					this.voxelDuplicate = true;
				}
			};

			this.PropagateMaterial.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
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
				if (material != Voxel.States.Empty)
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
				this.NeedsSave.Value = true;
			};

			this.IntersectMaterial.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
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
				this.NeedsSave.Value = true;
			};

			// Propagate to all cells of a certain type, including non-contiguous ones
			this.PropagateMaterialAll.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.State oldMaterial = selectedBox.Type;

				Voxel.State material = this.getBrush();
				if (material != Voxel.States.Empty)
				{
					if (material == oldMaterial)
						return;
					List<Voxel.Coord> coords = m.Chunks.SelectMany(x => x.Boxes).Where(x => x.Type == oldMaterial).SelectMany(x => x.GetCoords()).ToList();
					m.Empty(coords, true);
					foreach (Voxel.Coord c in coords)
						m.Fill(c, material);
					m.Regenerate();
				}
				this.NeedsSave.Value = true;
			};

			this.PropagateMaterialBox.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
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
				if (material != Voxel.States.Empty)
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
					this.NeedsSave.Value = true;
				}
			};

			this.StartFill.Action = delegate()
			{
				this.Fill.Value = this.Fill == FillMode.Empty ? FillMode.ForceFill : FillMode.Fill;
			};

			this.StopFill.Action = delegate()
			{
				this.Fill.Value = FillMode.None;
			};

			this.StartEmpty.Action = delegate()
			{
				this.Fill.Value = this.Fill == FillMode.Fill ? FillMode.ForceFill : FillMode.Empty;
			};

			this.StopEmpty.Action = delegate()
			{
				this.Fill.Value = this.Fill == FillMode.ForceFill ? FillMode.Fill : FillMode.None;
			};

			Action<Direction, Direction, Direction, Voxel.Coord> rotate = delegate(Direction x, Direction y, Direction z, Voxel.Coord selectionOffset)
			{
				this.VoxelCopy.Execute();
				this.NeedsSave.Value = true;

				Voxel.Coord oldSelectionStart = this.VoxelSelectionStart;
				Voxel.Coord oldSelectionEnd = this.VoxelSelectionEnd;
				Voxel.Coord newSelectionStart = this.VoxelSelectionStart.Value.Minus(this.coord).Reorient(x, y, z).Plus(this.coord).Plus(selectionOffset);
				Voxel.Coord newSelectionEnd = this.VoxelSelectionEnd.Value.Minus(this.coord).Reorient(x, y, z).Plus(this.coord).Plus(selectionOffset);
				this.VoxelSelectionStart.Value = new Voxel.Coord
				{
					X = Math.Min(newSelectionStart.X, newSelectionEnd.X),
					Y = Math.Min(newSelectionStart.Y, newSelectionEnd.Y),
					Z = Math.Min(newSelectionStart.Z, newSelectionEnd.Z),
				};
				this.VoxelSelectionEnd.Value = new Voxel.Coord
				{
					X = Math.Max(newSelectionStart.X, newSelectionEnd.X),
					Y = Math.Max(newSelectionStart.Y, newSelectionEnd.Y),
					Z = Math.Max(newSelectionStart.Z, newSelectionEnd.Z),
				};

				Voxel map = this.SelectedEntities[0].Get<Voxel>();
				foreach (Voxel.Coord c in oldSelectionStart.CoordinatesBetween(oldSelectionEnd))
					Voxel.CoordDictionaryCache[c] = true;
				foreach (Voxel.Coord c in this.VoxelSelectionStart.Value.CoordinatesBetween(this.VoxelSelectionEnd))
					Voxel.CoordDictionaryCache[c] = true;
				map.Empty(Voxel.CoordDictionaryCache.Keys, true);
				Voxel.CoordDictionaryCache.Clear();

				this.restoreVoxel(oldSelectionStart, oldSelectionEnd, x, y, z);
				this.NeedsSave.Value = true;
			};

			this.VoxelRotateX.Action = delegate()
			{
				rotate(Direction.PositiveX, Direction.PositiveZ, Direction.NegativeY, new Voxel.Coord { X = 0, Y = 1, Z = 0 });
			};

			this.VoxelRotateY.Action = delegate()
			{
				rotate(Direction.NegativeZ, Direction.PositiveY, Direction.PositiveX, new Voxel.Coord { X = 0, Y = 0, Z = 1 });
			};

			this.VoxelRotateZ.Action = delegate()
			{
				rotate(Direction.NegativeY, Direction.PositiveX, Direction.PositiveZ, new Voxel.Coord { X = 0, Y = 1, Z = 0 });
			};

			this.SampleMaterial.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				this.Brush.Value = selectedBox.Type.ID;
			};

			this.DeleteMaterial.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
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
				this.NeedsSave.Value = true;
			};

			// Delete all cells of a certain type in the current map, including non-contiguous ones
			this.DeleteMaterialAll.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				if (!this.VoxelEditMode)
					return;

				Voxel m = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Box selectedBox = m.GetBox(this.coord);
				if (selectedBox == null)
					return;

				Voxel.State material = selectedBox.Type;

				m.Empty(m.Chunks.SelectMany(x => x.Boxes).Where(x => x.Type == material).SelectMany(x => x.GetCoords()).ToList(), true);
				m.Regenerate();
				this.NeedsSave.Value = true;
			};

			Action<TransformModes> startTransform = delegate(TransformModes mode)
			{
				if (this.SelectedEntities.Length == 0)
					return;

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
						Transform transform = entity.Get<Transform>("Transform");
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
				if (!this.EnableCommands())
					return;
				startTransform(TransformModes.Translate);
			};

			this.StartRotation.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				startTransform(TransformModes.Rotate);
			};

			this.CommitTransform.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
				this.NeedsSave.Value = true;
				this.TransformMode.Value = TransformModes.None;
				this.TransformAxis.Value = TransformAxes.All;
				if (this.VoxelEditMode)
					this.justCommitedOrRevertedVoxelOperation = true;
				this.offsetTransforms.Clear();
			};

			this.RevertTransform.Action = delegate()
			{
				if (!this.EnableCommands())
					return;
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
							Transform transform = entity.Get<Transform>("Transform");
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
				if (!this.EnableCommands())
					return;
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

			this.Add(new NotifyBinding(delegate()
			{
				bool active;
				if (!this.VoxelEditMode)
					active = false;
				else
				{
					Voxel.Coord start = this.VoxelSelectionStart, end = this.VoxelSelectionEnd;
					active = start.X != end.X && start.Y != end.Y && start.Z != end.Z;
				}
				if (this.VoxelSelectionActive != active)
					this.VoxelSelectionActive.Value = active;
			}, this.VoxelEditMode, this.VoxelSelectionStart, this.VoxelSelectionEnd));

			this.Add(new Binding<Voxel.Coord>(this.VoxelSelectionSize, delegate()
			{
				Voxel.Coord a = this.VoxelSelectionStart, b = this.VoxelSelectionEnd;
				return new Voxel.Coord { X = b.X - a.X, Y = b.Y - a.Y, Z = b.Z - a.Z };
			}, this.VoxelSelectionStart, this.VoxelSelectionEnd));
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
						this.movementInterval = 100.0f;

					Voxel map = this.SelectedEntities[0].Get<Voxel>();
					if (this.movementInterval > (this.SpeedMode ? 0.5f : 1.0f) * Math.Min(0.15f, map.Scale / this.CameraDistance))
					{
						if (moving)
							this.movementInterval = 0.0f;
						if (movementDir.LengthSquared() > 0.0f)
						{
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
						}
					}
					this.movementInterval += elapsedTime;
					this.Position.Value = map.GetAbsolutePosition(this.coord);
				}
				else
					this.Position.Value = this.Position.Value + movementDir * (this.SpeedMode ? 5.0f : 2.5f) * this.CameraDistance * elapsedTime;
			}

			if (this.VoxelEditMode)
			{
				if (this.Fill == FillMode.None)
					this.justCommitedOrRevertedVoxelOperation = false;

				Voxel map = this.SelectedEntities[0].Get<Voxel>();
				Voxel.Coord coord = map.GetCoordinate(this.Position);
				if (this.TransformMode.Value == TransformModes.None && (this.Fill != FillMode.None || this.Extend) && !this.justCommitedOrRevertedVoxelOperation)
				{
					this.NeedsSave.Value = true;
					if (this.Fill != FillMode.None)
					{
						if (this.VoxelSelectionActive)
						{
							if (this.Jitter.Value.Equivalent(new Voxel.Coord { X = 0, Y = 0, Z = 0 }) || this.BrushSize <= 1)
							{
								switch (this.Fill.Value)
								{
									case FillMode.Fill:
										map.Fill(this.VoxelSelectionStart, this.VoxelSelectionEnd, this.getBrush());
										break;
									case FillMode.Empty:
										map.Empty(this.VoxelSelectionStart, this.VoxelSelectionEnd, true);
										break;
									default:
										break;
								}
							}
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
											this.brushStroke(map, new Voxel.Coord { X = x, Y = y, Z = z });
									}
								}
							}
						}
						else
							this.brushStroke(map, coord);
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
						Transform transform = entity.Get<Transform>("Transform");
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
				Matrix localRotation = this.offsetTransforms.Count == 1 ? this.offsetTransforms[0] : Matrix.Identity;
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
					case TransformAxes.LocalX:
						axis = Vector3.Normalize(localRotation.Right);
						break;
					case TransformAxes.LocalY:
						axis = Vector3.Normalize(localRotation.Up);
						break;
					case TransformAxes.LocalZ:
						axis = Vector3.Normalize(localRotation.Forward);
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
						Transform transform = entity.Get<Transform>("Transform");
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
			Voxel.Coord jitter = this.Jitter;
			Voxel.Coord sample = coord.Clone();
			coord.X += (int)Math.Round(this.generator.Sample(map, sample.Move(0, 0, map.ChunkSize * 2), this.JitterOctave) * jitter.X);
			coord.Y += (int)Math.Round(this.generator.Sample(map, sample.Move(map.ChunkSize * 2, 0, 0), this.JitterOctave) * jitter.Y);
			coord.Z += (int)Math.Round(this.generator.Sample(map, sample.Move(0, map.ChunkSize * 2, 0), this.JitterOctave) * jitter.Z);
			return coord;
		}

		protected void brushStroke(Voxel map, Voxel.Coord center)
		{
			int size = this.BrushSize;
			center = this.jitter(map, center);

			Voxel.State state = this.getBrush();
			if (this.Fill == FillMode.Empty)
				state = Voxel.States.Empty;

			BrushShapes shape = this.BrushShape;
			Vector3 pos = map.GetRelativePosition(center);
			List<Voxel.Coord> coords = new List<Voxel.Coord>();
			for (Voxel.Coord x = center.Move(Direction.NegativeX, size - 1); x.X < center.X + size; x.X++)
			{
				for (Voxel.Coord y = x.Move(Direction.NegativeY, size - 1); y.Y < center.Y + size; y.Y++)
				{
					for (Voxel.Coord z = y.Move(Direction.NegativeZ, size - 1); z.Z < center.Z + size; z.Z++)
					{
						if ((shape == BrushShapes.Cube || (pos - map.GetRelativePosition(z)).Length() <= size)
							&& map[z] != state)
							coords.Add(z);
					}
				}
			}

			switch (this.Fill.Value)
			{
				case FillMode.Empty:
					map.Empty(coords, true);
					break;
				case FillMode.Fill:
					foreach (Voxel.Coord coord in coords)
						map.Fill(coord, state);
					break;
				case FillMode.ForceFill:
					map.Empty(coords, true);
					foreach (Voxel.Coord coord in coords)
						map.Fill(coord, state);
					break;
				default:
					break;
			}
		}
	}
}
