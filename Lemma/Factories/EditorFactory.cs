using System;
using System.Windows.Forms;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.GInterfaces;
using Microsoft.Xna.Framework;
using Lemma.Components;
using BEPUphysics;
using System.Xml.Serialization;
using System.IO;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using GeeUI.Managers;

namespace Lemma.Factories
{
	public class EditorFactory : Factory<Main>
	{
		public EditorFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
			this.EditorCanSpawn = false;
		}

		public override Entity Create(Main main)
		{
			Entity entity = new Entity(main, "Editor");
			entity.Serialize = false;
			return entity;
		}

		private void raycast(Main main, Vector3 rayStart, Vector3 ray, out Entity closestEntity, out Transform closestTransform)
		{
			closestEntity = null;
			float closestEntityDistance = main.Camera.FarPlaneDistance;
			closestTransform = null;
			foreach (Entity entity in main.Entities)
			{
				foreach (Transform transform in entity.GetAll<Transform>())
				{
					if (transform.Selectable)
					{
						Vector3 entityPos = transform.Position;
						float distance = (entityPos - rayStart).Length();
						Vector3 closestToEntity = rayStart + ray * distance;
						if ((distance < closestEntityDistance) && (closestToEntity - entityPos).Length() < 0.5f)
						{
							closestEntityDistance = distance;
							closestEntity = entity;
							closestTransform = transform;
						}
					}
				}
			}

			Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(rayStart, ray, closestEntityDistance, null, true);
			if (hit.Coordinate != null)
			{
				closestEntity = hit.Voxel.Entity;
				closestTransform = null;
			}
		}

		public static void AddCommand(Entity entity, Main main, string description, PCInput.Chord chord, Command action, ListProperty<Lemma.Components.EditorGeeUI.EditorCommand> list, Func<bool> enabled = null, params IProperty[] dependencies)
		{
			EditorGeeUI.EditorCommand cmd = new EditorGeeUI.EditorCommand { Description = description, Chord = chord, Action = action, Enabled = new Property<bool> { Value = true } };
			if (enabled != null)
				entity.Add(new Binding<bool>(cmd.Enabled, enabled, dependencies));
			else
				enabled = () => true;

			list.Add(cmd);

			PCInput input = entity.Get<PCInput>();
			if (chord.Modifier != Keys.None)
				input.Add(new CommandBinding(input.GetChord(chord), enabled, action));
			else if (chord.Mouse == PCInput.MouseButton.LeftMouseButton)
				input.Add(new CommandBinding(input.LeftMouseButtonDown, enabled, action));
			else if (chord.Mouse == PCInput.MouseButton.RightMouseButton)
				input.Add(new CommandBinding(input.RightMouseButtonDown, enabled, action));
			else if (chord.Mouse == PCInput.MouseButton.MiddleMouseButton)
				input.Add(new CommandBinding(input.MiddleMouseButtonDown, enabled, action));
			else
				input.Add(new CommandBinding(input.GetKeyDown(chord.Key), enabled, action));

			entity.Add(new CommandBinding(action, entity.Get<Editor>().EnableCommands, delegate()
			{
				Container container = new Container();
				container.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				container.Opacity.Value = 0.2f;
				container.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
				container.Add(new Binding<Vector2, Point>(container.Position, x => new Vector2(x.X - 10.0f, 10.0f), main.ScreenSize));
				TextElement display = new TextElement();
				display.FontFile.Value = "Font";
				display.Text.Value = description;
				container.Children.Add(display);

				main.UI.Root.Children.Add(container);
				main.AddComponent(new Animation
				(
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(container.Opacity, 0.0f, 1.0f),
						new Animation.FloatMoveTo(display.Opacity, 0.0f, 1.0f)
					),
					new Animation.Execute(container.Delete)
				));
			}));
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);

			Editor editor = entity.GetOrCreate<Editor>();
			EditorGeeUI gui = entity.Create<EditorGeeUI>();
			gui.Add(new Binding<bool>(gui.Visible, x => !x, ConsoleUI.Showing));
			gui.Add(new Binding<bool>(gui.MovementEnabled, editor.MovementEnabled));

			Property<bool> coordinateVisible = new Property<bool>();
			gui.Add(new Binding<bool>(coordinateVisible, () => editor.VoxelEditMode && !editor.VoxelSelectionActive, editor.VoxelEditMode, editor.VoxelSelectionActive));
			gui.SetVoxelProperties(new Dictionary<string, PropertyEntry>
			{
				{ "Brush [Space]", new PropertyEntry(editor.Brush, new PropertyEntry.EditorData()) },
				{ "BrushSize", new PropertyEntry(editor.BrushSize, "[Scrollwheel]") },
				{ "BrushShape", new PropertyEntry(editor.BrushShape, new PropertyEntry.EditorData()) },
				{ "Jitter", new PropertyEntry(editor.Jitter, "[Shift+Scrollwheel]") },
				{ "JitterOctave", new PropertyEntry(editor.JitterOctave, new PropertyEntry.EditorData()) },
				{
					"Coordinate",
					new PropertyEntry
					(
						editor.Coordinate, new PropertyEntry.EditorData
						{
							Readonly = true,
							Visible = coordinateVisible,
						}
					)
				},
				{
					"Selection",
					new PropertyEntry
					(
						editor.VoxelSelectionSize, new PropertyEntry.EditorData
						{
							Readonly = true,
							Visible = editor.VoxelSelectionActive,
						}
					)
				},
			});

			editor.EnableCommands = () => !gui.AnyTextFieldViewsSelected();

			ModelAlpha model = new ModelAlpha();
			model.Filename.Value = "AlphaModels\\selector";
			model.Scale.Value = new Vector3(0.5f);
			model.Serialize = false;
			entity.Add(model);

			FPSInput input = entity.Create<FPSInput>();
			input.EnabledWhenPaused = true;

			ModelAlpha brushVisual = new ModelAlpha();
			brushVisual.Add(new Binding<string, Editor.BrushShapes>(brushVisual.Filename, x => x == Editor.BrushShapes.Cube ? "AlphaModels\\box" : "AlphaModels\\sphere", editor.BrushShape));
			brushVisual.Color.Value = new Vector3(1.0f);
			brushVisual.Alpha.Value = 0.1f;
			brushVisual.Serialize = false;
			brushVisual.DrawOrder.Value = 11; // In front of water
			brushVisual.DisableCulling.Value = true;
			entity.Add(brushVisual);
			brushVisual.Add(new NotifyBinding(delegate()
			{
				float s = 1.0f;
				if (editor.VoxelEditMode)
					s = editor.SelectedEntities[0].Get<Voxel>().Scale;
				if (editor.BrushShape == Editor.BrushShapes.Sphere)
					brushVisual.Scale.Value = new Vector3(s * editor.BrushSize);
				else
					brushVisual.Scale.Value = new Vector3(s * (editor.BrushSize - 0.4f) * 2.0f);
			}, editor.BrushSize, editor.BrushShape, editor.VoxelEditMode));
			brushVisual.Add(new Binding<bool>(brushVisual.Enabled, () => editor.BrushSize > 1 && editor.VoxelEditMode, editor.BrushSize, editor.VoxelEditMode));
			brushVisual.CullBoundingBox.Value = false;

			ModelAlpha selection = new ModelAlpha();
			selection.Filename.Value = "AlphaModels\\box";
			selection.Color.Value = new Vector3(1.0f, 0.7f, 0.4f);
			selection.Alpha.Value = 0.25f;
			selection.Serialize = false;
			selection.DrawOrder.Value = 12; // In front of water and radius visualizer
			selection.DisableCulling.Value = true;
			entity.Add(selection);
			selection.Add(new Binding<bool>(selection.Enabled, editor.VoxelSelectionActive));
			selection.Add(new NotifyBinding(delegate()
			{
				const float padding = 0.1f;
				Voxel map = editor.SelectedEntities[0].Get<Voxel>();
				Vector3 start = map.GetRelativePosition(editor.VoxelSelectionStart) - new Vector3(0.5f), end = map.GetRelativePosition(editor.VoxelSelectionEnd) - new Vector3(0.5f);
				selection.Transform.Value = Matrix.CreateScale((end - start) + new Vector3(padding)) * Matrix.CreateTranslation((start + end) * 0.5f) * map.Transform;
			}, () => editor.VoxelSelectionActive, editor.VoxelSelectionActive, editor.VoxelSelectionStart, editor.VoxelSelectionEnd));
			selection.CullBoundingBox.Value = false;

			AddCommand
			(
				entity, main, "Menu", new PCInput.Chord(Keys.F1),
				new Command
				{
					Action = main.Menu.Toggle,
				},
				gui.MapCommands
			);

			input.Add(new CommandBinding(input.GetChord(new PCInput.Chord(Keys.O, Keys.LeftControl)), gui.ShowOpenMenu));

			AddCommand
			(
				entity, main, "New", new PCInput.Chord(Keys.N, Keys.LeftControl),
				new Command
				{
					Action = () =>
					{
						main.AddComponent(new TextPrompt(delegate(string name)
						{
							IO.MapLoader.New(main, Path.Combine(main.CustomMapDirectory, name));
						}, "", "Map name:", "New map"));
					}
				},
				gui.MapCommands,
				() => !input.EnableLook && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				input.EnableLook, editor.VoxelEditMode, editor.TransformMode
			);

			AddCommand
			(
				entity, main, "Workshop Publish",
				new PCInput.Chord(Keys.W, Keys.LeftControl),
				new Command
				{
					Action = delegate()
					{
						string f = main.MapFile;
						if (Path.GetExtension(f) != ".map")
							f += ".map";
						if (!Path.IsPathRooted(f))
							f = Path.GetFullPath(Path.Combine(main.MapDirectory, f));
						main.AddComponent(new WorkShopInterface());
					}
				},
				gui.MapCommands,
				() => !input.EnableLook && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				input.EnableLook, editor.VoxelEditMode, editor.TransformMode
			);

			AddCommand
			(
				entity, main, "Workshop Update",
				new PCInput.Chord(),
				new Command
				{
					Action = delegate()
					{
						string f = main.MapFile;
						if (Path.GetExtension(f) != IO.MapLoader.MapExtension)
							f += IO.MapLoader.MapExtension;
						if (!Path.IsPathRooted(f))
							f = Path.GetFullPath(Path.Combine(main.MapDirectory, f));
						main.AddComponent(new UpdateWorkShopInterface());
					}
				},
				gui.MapCommands,
				() => !input.EnableLook && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				input.EnableLook, editor.VoxelEditMode, editor.TransformMode
			);

			foreach (string key in Factory.factories.Keys)
			{
				string entityType = key;
				Factory factory = Factory.Get(entityType);
				if (factory.EditorCanSpawn)
				{
					gui.AddEntityCommands.Add(new EditorGeeUI.EditorCommand
					{
						Description = "Add " + entityType,
						Enabled = new Property<bool> { Value = true },
						Action = new Command { Action = () => editor.Spawn.Execute(entityType) },
					});
				}
			}

			input.Add(new CommandBinding(input.GetKeyUp(Keys.Space), () => !editor.MovementEnabled && !gui.AnyTextFieldViewsSelected() && editor.TransformMode == Editor.TransformModes.None, gui.ShowContextMenu));
			editor.Add(new Binding<bool>(main.GeeUI.KeyboardEnabled, () => !editor.VoxelEditMode && !editor.MovementEnabled, editor.VoxelEditMode, editor.MovementEnabled));

			model.Add(new Binding<bool>(model.Enabled, editor.VoxelEditMode));
			model.Add(new Binding<Matrix>(model.Transform, () => Matrix.CreateFromQuaternion(editor.Orientation) * Matrix.CreateTranslation(editor.Position), editor.Position, editor.Orientation));
			brushVisual.Add(new Binding<Matrix>(brushVisual.Transform, model.Transform));

			// When transferring between maps we need to clear our GUID to make way for the entities on the new map,
			// then assign ourselves a new GUID.
			entity.Add(new CommandBinding<string>(main.LoadingMap, delegate(string map)
			{
				editor.SelectedEntities.Clear();
				if (editor.VoxelEditMode)
					editor.VoxelEditMode.Value = false;
				editor.TransformMode.Value = Editor.TransformModes.None;
				entity.ClearGUID();
			}));
			entity.Add(new CommandBinding(main.MapLoaded, (Action)entity.NewGUID));

			gui.Add(new ListBinding<Entity>(gui.SelectedEntities, editor.SelectedEntities));
			gui.Add(new CommandBinding<Entity>(gui.SelectEntity, delegate(Entity e)
			{
				editor.SelectedEntities.Clear();
				editor.SelectedEntities.Add(e);
			}));
			gui.Add(new Binding<bool>(gui.VoxelEditMode, editor.VoxelEditMode));
			gui.Add(new TwoWayBinding<bool>(editor.NeedsSave, gui.NeedsSave));
			gui.Add(new Binding<Editor.TransformModes>(gui.TransformMode, editor.TransformMode));
			gui.Add(new Binding<bool>(gui.VoxelSelectionActive, editor.VoxelSelectionActive));

			Property<bool> movementEnabled = new Property<bool>();
			Property<bool> capslockKey = input.GetKey(Keys.CapsLock);
			entity.Add(new Binding<bool>(movementEnabled, () => input.MiddleMouseButton || capslockKey, input.MiddleMouseButton, capslockKey));

			editor.Add(new Binding<bool>(editor.MovementEnabled, () => movementEnabled || editor.VoxelEditMode, movementEnabled, editor.VoxelEditMode));

			editor.Add(new Binding<Vector2>(editor.Movement, input.Movement));
			editor.Add(new Binding<bool>(editor.Up, input.GetKey(Keys.Space)));
			editor.Add(new Binding<bool>(editor.Down, input.GetKey(Keys.LeftControl)));
			editor.Add(new Binding<bool>(editor.SpeedMode, input.GetKey(Keys.LeftShift)));
			editor.Add(new Binding<bool>(editor.Extend, input.GetKey(Keys.F)));
			editor.Add(new CommandBinding(input.LeftMouseButtonDown, editor.StartFill));
			editor.Add(new CommandBinding(input.LeftMouseButtonUp, editor.StopFill));
			editor.Add(new CommandBinding(input.RightMouseButtonDown, editor.StartEmpty));
			editor.Add(new CommandBinding(input.RightMouseButtonUp, editor.StopEmpty));
			editor.Add(new Binding<bool>(editor.EditSelection, () => movementEnabled && editor.VoxelEditMode, movementEnabled, editor.VoxelEditMode));

			AddCommand
			(
				entity, main, "Delete", new PCInput.Chord { Key = Keys.X }, editor.DeleteSelected, gui.EntityCommands,
				() => !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None && editor.SelectedEntities.Length > 0 && !editor.MovementEnabled,
				editor.VoxelEditMode, editor.TransformMode, editor.SelectedEntities.Length, editor.MovementEnabled
			);
			AddCommand
			(
				entity, main, "Duplicate", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.D }, editor.Duplicate, gui.EntityCommands,
				() => !editor.MovementEnabled && editor.SelectedEntities.Length > 0 && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.MovementEnabled, editor.SelectedEntities.Length, editor.TransformMode
			);

			// Start playing
			AddCommand
			(
				entity, main, "Run", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.R },
				new Command
				{
					Action = delegate()
					{
						if (editor.NeedsSave)
							editor.Save.Execute();
						main.EditorEnabled.Value = false;
						IO.MapLoader.Load(main, main.MapFile);
					}
				},
				gui.MapCommands,
				() => !editor.MovementEnabled,
				editor.MovementEnabled
			);

			AddCommand
			(
				entity, main, "Join voxels", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.J },
				new Command
				{
					Action = delegate()
					{
						Entity entity1 = editor.SelectedEntities[0];
						Entity entity2 = editor.SelectedEntities[1];

						Voxel map1 = entity1.Get<Voxel>();
						Voxel map2 = entity2.Get<Voxel>();

						foreach (Voxel.Chunk chunk in map1.Chunks)
						{
							foreach (Voxel.Box box in chunk.Boxes)
							{
								foreach (Voxel.Coord coord in box.GetCoords())
									map2.Fill(map1.GetAbsolutePosition(coord), box.Type, false);
							}
						}
						map2.Regenerate();
						entity1.Delete.Execute();
						editor.NeedsSave.Value = true;
					}
				},
				gui.VoxelCommands,
				() => !editor.VoxelEditMode && editor.SelectedEntities.Length == 2 && editor.SelectedEntities[0].Get<Voxel>() != null && editor.SelectedEntities[1].Get<Voxel>() != null,
				editor.VoxelEditMode, editor.SelectedEntities.Length
			);

			AddCommand
			(
				entity, main, "Create VoxelFill", new PCInput.Chord(),
				new Command
				{
					Action = delegate()
					{
						Entity currentVoxel = editor.SelectedEntities[0];
						Voxel map1 = currentVoxel.Get<Voxel>();

						Entity voxelFill = Factory<Main>.Get<VoxelFillFactory>().CreateAndBind(main);
						Transform oldPosition = currentVoxel.Get<Transform>("Transform");
						Transform position = voxelFill.Get<Transform>("Transform");
						position.Position.Value = oldPosition.Position.Value;
						position.Quaternion.Value = oldPosition.Quaternion.Value;
						voxelFill.Get<VoxelFill>().Target.Value = map1.Entity;
						main.Add(voxelFill);

						Voxel map2 = voxelFill.Get<Voxel>();

						foreach (Voxel.Chunk chunk in map1.Chunks)
						{
							foreach (Voxel.Box box in chunk.Boxes)
							{
								foreach (Voxel.Coord coord in box.GetCoords())
									map2.Fill(map1.GetAbsolutePosition(coord), box.Type, false);
							}
						}
						map2.Regenerate();

						List<Voxel.Coord> toRemove = new List<Voxel.Coord>();
						foreach (Voxel.Chunk chunk in map1.Chunks)
						{
							foreach (Voxel.Box box in chunk.Boxes)
							{
								foreach (Voxel.Coord coord in box.GetCoords())
									toRemove.Add(coord);
							}
						}
						map1.Empty(toRemove, true, notify: false);
						map1.Regenerate();

						editor.NeedsSave.Value = true;
						editor.SelectedEntities.Clear();
						editor.SelectedEntities.Add(voxelFill);
					}
				},
				gui.VoxelCommands,
				() => !editor.VoxelEditMode && editor.SelectedEntities.Length == 1 && editor.SelectedEntities[0].Type == "Voxel",
				editor.VoxelEditMode, editor.SelectedEntities.Length
			);

			entity.Add(new CommandBinding(main.MapLoaded, delegate()
			{
				editor.Position.Value = Vector3.Zero;
				editor.NeedsSave.Value = false;
			}));

			AddCommand
			(
				entity, main, "Quit", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.Q },
				new Command
				{
					Action = delegate()
					{
						throw new Main.ExitException();
					}
				},
				gui.MapCommands,
				() => !editor.MovementEnabled,
				editor.MovementEnabled
			);

			AddCommand
			(
				entity, main, "Help", new PCInput.Chord(),
				new Command
				{
					Action = delegate()
					{
						UIFactory.OpenURL("http://steamcommunity.com/sharedfiles/filedetails/?id=273022369");
					}
				},
				gui.MapCommands
			);

			// Save
			AddCommand
			(
				entity, main, "Save", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.S }, editor.Save, gui.MapCommands,
				() => !editor.MovementEnabled,
				editor.MovementEnabled
			);

			// Deselect all entities
			AddCommand
			(
				entity, main, "Deselect all", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.D },
				new Command
				{
					Action = delegate()
					{
						if (!gui.AnyTextFieldViewsSelected())
							editor.SelectedEntities.Clear();
					}
				},
				gui.EntityCommands,
				() => !editor.MovementEnabled && editor.SelectedEntities.Length > 0,
				editor.MovementEnabled, editor.SelectedEntities.Length
			);

			entity.Add(new CommandBinding<int>(input.MouseScrolled, () => editor.VoxelEditMode && !input.GetKey(Keys.LeftAlt) && !input.GetKey(Keys.LeftShift), delegate(int delta)
			{
				editor.BrushSize.Value = Math.Max(1, editor.BrushSize.Value + delta);
			}));

			AddCommand
			(
				entity, main, "Voxel edit mode", new PCInput.Chord { Key = Keys.Tab },
				new Command
				{
					Action = delegate()
					{
						editor.VoxelEditMode.Value = !editor.VoxelEditMode;
						model.Scale.Value = new Vector3(editor.SelectedEntities[0].Get<Voxel>().Scale * 0.5f);
					}
				},
				gui.VoxelCommands,
				delegate()
				{
					if (editor.TransformMode.Value != Editor.TransformModes.None)
						return false;

					if (input.GetKey(Keys.LeftShift) || input.GetKey(Keys.LeftControl))
						return false;
					
					if (gui.PickNextEntity)
						return false;

					if (editor.VoxelEditMode)
						return true;
					else
						return editor.SelectedEntities.Length == 1 && editor.SelectedEntities[0].Get<Voxel>() != null;
				},
				editor.TransformMode, input.GetKey(Keys.LeftShift), input.GetKey(Keys.LeftControl), editor.VoxelEditMode, editor.SelectedEntities.Length, gui.PickNextEntity
			);

			int brush = 0;
			Action<int> changeBrush = delegate(int delta)
			{
				int foundIndex = Voxel.States.List.FindIndex(x => x.ID == editor.Brush);
				if (foundIndex != -1)
					brush = foundIndex;
				int stateCount = Voxel.States.List.Count;
				brush = 1 + ((brush - 1 + delta) % (stateCount - 1));
				if (brush < 1)
					brush = stateCount + ((brush - 1) % stateCount);
				editor.Brush.Value = Voxel.States.List[brush].ID;
			};
			AddCommand
			(
				entity, main, "Previous brush", new PCInput.Chord { Key = Keys.Q },
				new Command
				{
					Action = delegate()
					{
						if (!input.GetKey(Keys.LeftShift))
							changeBrush(-1);
					}
				},
				gui.VoxelCommands,
				() => editor.VoxelEditMode,
				editor.VoxelEditMode
			);

			AddCommand
			(
				entity, main, "Next brush", new PCInput.Chord { Key = Keys.E },
				new Command
				{
					Action = delegate()
					{
						if (!input.GetKey(Keys.LeftShift))
							changeBrush(1);
					}
				}, gui.VoxelCommands,
				() => editor.VoxelEditMode,
				editor.VoxelEditMode
			);

			AddCommand
			(
				entity, main, "Sample", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Q }, editor.SampleMaterial, gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity, main, "Propagate", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.E }, editor.PropagateMaterial, gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity, main, "Propagate box", new PCInput.Chord { Key = Keys.R }, editor.PropagateMaterialBox, gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity, main, "Propagate non-contiguous", new PCInput.Chord { Key = Keys.T }, editor.PropagateMaterialAll, gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity, main, "Delete", new PCInput.Chord { Key = Keys.OemComma }, editor.DeleteMaterial, gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity, main, "Delete non-contiguous", new PCInput.Chord { Key = Keys.OemPeriod }, editor.DeleteMaterialAll, gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity, main, "Intersect", new PCInput.Chord { Key = Keys.I }, editor.IntersectMaterial, gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);

			editor.Add(new Binding<Vector2>(editor.Mouse, main.UI.Mouse));

			input.Add(new CommandBinding(main.UI.SwallowMouseEvents, (Action)input.SwallowEvents));

			Camera camera = main.Camera;

			input.Add(new CommandBinding<int>(input.MouseScrolled, () => input.GetKey(Keys.LeftAlt) && editor.EnableCameraDistanceScroll, delegate(int delta)
			{
				editor.CameraDistance.Value = Math.Max(1, editor.CameraDistance.Value + delta * -2.0f);
			}));

			input.Add(new CommandBinding<int>(input.MouseScrolled, () => input.GetKey(Keys.LeftShift) && editor.EnableCameraDistanceScroll, delegate(int delta)
			{
				Voxel.Coord j = editor.Jitter;
				j.X = Math.Max(j.X + delta, 0);
				j.Y = Math.Max(j.Y + delta, 0);
				j.Z = Math.Max(j.Z + delta, 0);
				editor.Jitter.Value = j;
			}));

			input.Add(new Binding<bool>(input.EnableLook, () => editor.VoxelEditMode || (movementEnabled && editor.TransformMode.Value == Editor.TransformModes.None), movementEnabled, editor.VoxelEditMode, editor.TransformMode));

			input.Add(new NotifyBinding(delegate()
			{
				Vector2 x = input.Mouse;
				camera.Angles.Value = new Vector3(-x.Y, x.X, 0.0f);
			}, () => input.EnableLook, input.Mouse));

			input.Add(new Binding<bool>(main.UI.IsMouseVisible, x => !x, input.EnableLook));
			editor.Add(new Binding<Vector3>(camera.Position, () => editor.Position.Value - (camera.Forward.Value * editor.CameraDistance), editor.Position, camera.Forward, editor.CameraDistance));

			PointLight editorLight = entity.GetOrCreate<PointLight>("EditorLight");
			editorLight.Serialize = false;
			editorLight.Add(new Binding<float>(editorLight.Attenuation, main.Camera.FarPlaneDistance));
			editorLight.Color.Value = new Vector3(1.5f, 1.5f, 1.5f);
			editorLight.Add(new Binding<Vector3>(editorLight.Position, main.Camera.Position));
			editorLight.Enabled.Value = false;

			AddCommand
			(
				entity, main, "Toggle editor light", new PCInput.Chord(),
				new Command
				{
					Action = () => editorLight.Enabled.Value = !editorLight.Enabled
				},
				gui.MapCommands
			);

			editor.Add(new CommandBinding(input.RightMouseButtonDown, () => !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None && !main.GeeUI.LastClickCaptured, delegate()
			{
				// We're not editing a voxel
				// And we're not transforming entities
				// So we must be selecting / deselecting entities
				bool multiselect = input.GetKey(Keys.LeftShift);

				Vector2 mouse = input.Mouse;
				Vector3 rayStart;
				Vector3 ray;
#if VR
				if (main.VR)
				{
					Vector2 size = main.UI.Root.Size;
					mouse = main.UI.Mouse;
					mouse.X /= size.X;
					mouse.Y /= size.Y;
					mouse *= 2.0f;
					mouse -= new Vector2(1.0f);
					mouse.Y *= -1.0f;
					mouse *= 0.5f;
					rayStart = main.Camera.Position;
					ray = Vector3.Normalize(Vector3.Transform(new Vector3(0, mouse.Y, -mouse.X), main.VRUI.Transform) - camera.Position);
				}
				else
#endif
				{
					Microsoft.Xna.Framework.Graphics.Viewport viewport = main.GraphicsDevice.Viewport;
					rayStart = main.Camera.Position;
					ray = Vector3.Normalize(viewport.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.Projection, camera.View, Matrix.Identity) - viewport.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.Projection, camera.View, Matrix.Identity));
				}

				Entity closestEntity;
				Transform closestTransform;
				this.raycast(main, rayStart, ray, out closestEntity, out closestTransform);

				if (gui.PickNextEntity)
					gui.EntityPicked.Execute(closestEntity);
				else if (closestEntity != null)
				{
					if (multiselect)
					{
						if (editor.SelectedEntities.Contains(closestEntity))
							editor.SelectedEntities.Remove(closestEntity);
						else
							editor.SelectedEntities.Add(closestEntity);
					}
					else
					{
						editor.SelectedEntities.Clear();
						editor.SelectedEntities.Add(closestEntity);
						editor.SelectedTransform.Value = closestTransform;
					}
				}
				else
				{
					editor.SelectedEntities.Clear();
					editor.SelectedTransform.Value = null;
				}
			}));

			editor.Add(new CommandBinding(input.GetKeyDown(Keys.Escape), delegate()
			{
				if (editor.TransformMode.Value != Editor.TransformModes.None)
					editor.RevertTransform.Execute();
				else if (editor.VoxelEditMode)
					editor.VoxelEditMode.Value = false;
				else if (gui.PickNextEntity)
					gui.EntityPicked.Execute(null);
			}));

			AddCommand
			(
				entity,
				main,
				"Grab",
				new PCInput.Chord { Key = Keys.G },
				editor.StartTranslation,
				gui.EntityCommands,
				() => editor.SelectedEntities.Length > 0 && !input.EnableLook && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.SelectedEntities.Length, input.EnableLook, editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Grab",
				new PCInput.Chord { Key = Keys.G },
				editor.StartVoxelTranslation,
				gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.VoxelSelectionActive, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Duplicate",
				new PCInput.Chord { Key = Keys.V },
				editor.VoxelDuplicate,
				gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.VoxelSelectionActive, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Copy",
				new PCInput.Chord { Key = Keys.C },
				editor.VoxelCopy,
				gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.VoxelSelectionActive, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Paste",
				new PCInput.Chord { Key = Keys.P },
				editor.VoxelPaste,
				gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Rotate",
				new PCInput.Chord { Key = Keys.R },
				editor.StartRotation,
				gui.EntityCommands,
				() => editor.SelectedEntities.Length > 0 && !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.SelectedEntities.Length, editor.VoxelEditMode, input.EnableLook, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Lock X axis",
				new PCInput.Chord { Key = Keys.X },
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.X },
				gui.EntityCommands,
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Lock Y axis",
				new PCInput.Chord { Key = Keys.Y },
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Y },
				gui.EntityCommands,
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);
			AddCommand
			(
				entity,
				main,
				"Lock Z axis",
				new PCInput.Chord { Key = Keys.Z },
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Z },
				gui.EntityCommands,
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				editor.VoxelEditMode, editor.TransformMode
			);

			AddCommand
			(
				entity,
				main,
				"Clear rotation",
				new PCInput.Chord { },
				new Command
				{
					Action = delegate()
					{
						foreach (Entity e in editor.SelectedEntities)
							e.Get<Transform>().Quaternion.Value = Quaternion.Identity;
					}
				},
				gui.EntityCommands,
				() => !editor.VoxelEditMode && editor.SelectedEntities.Length > 0 && editor.TransformMode.Value == Editor.TransformModes.None && !editor.MovementEnabled,
				editor.VoxelEditMode, editor.SelectedEntities.Length, editor.TransformMode, editor.MovementEnabled
			);

			AddCommand
			(
				entity,
				main,
				"Clear translation",
				new PCInput.Chord { },
				new Command
				{
					Action = delegate()
					{
						foreach (Entity e in editor.SelectedEntities)
							e.Get<Transform>().Position.Value = Vector3.Zero;
					}
				},
				gui.EntityCommands,
				() => !editor.VoxelEditMode && editor.SelectedEntities.Length > 0 && editor.TransformMode.Value == Editor.TransformModes.None && !editor.MovementEnabled,
				editor.VoxelEditMode, editor.SelectedEntities.Length, editor.TransformMode, editor.MovementEnabled
			);

			MemoryStream yankBuffer = null;
			AddCommand
			(
				entity,
				main,
				"Copy",
				new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.C },
				new Command
				{
					Action = delegate()
					{
						if (yankBuffer != null)
						{
							yankBuffer.Dispose();
							yankBuffer = null;
						}
						yankBuffer = new MemoryStream();
						IO.MapLoader.Serializer.Serialize(yankBuffer, editor.SelectedEntities.ToList());
					}
				},
				gui.EntityCommands,
				() => !editor.VoxelEditMode && !input.EnableLook && editor.SelectedEntities.Length > 0 && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelEditMode, input.EnableLook, editor.SelectedEntities.Length, editor.TransformMode
			);

			AddCommand
			(
				entity,
				main,
				"Paste",
				new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.V },
				new Command
				{
					Action = delegate()
					{
						if (yankBuffer != null)
						{
							yankBuffer.Seek(0, SeekOrigin.Begin);
							List<Entity> entities = (List<Entity>)IO.MapLoader.Serializer.Deserialize(yankBuffer);

							Vector3 center = Vector3.Zero;
							int entitiesWithTransforms = 0;
							foreach (Entity e in entities)
							{
								e.GUID = 0;
								e.ID.Value = "";
								Factory<Main> factory = Factory<Main>.Get(e.Type);
								factory.Bind(e, main);
								Transform t = e.Get<Transform>();
								if (t != null)
								{
									center += t.Position;
									entitiesWithTransforms++;
								}
								main.Add(e);
							}

							center /= entitiesWithTransforms;

							// Recenter entities around the editor
							foreach (Entity e in entities)
							{
								Transform t = e.Get<Transform>();
								if (t != null)
									t.Position.Value += editor.Position - center;
							}

							editor.SelectedEntities.Clear();
							editor.SelectedEntities.AddAll(entities);
							editor.StartTranslation.Execute();
						}
					}
				},
				gui.EntityCommands,
				() => !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode == Editor.TransformModes.None,
				editor.VoxelEditMode, input.EnableLook, editor.TransformMode
			);

			AddCommand
			(
				entity, main, "Commit transform",
				new PCInput.Chord { Mouse = PCInput.MouseButton.LeftMouseButton },
				editor.CommitTransform,
				gui.EntityCommands,
				() => editor.TransformMode.Value != Editor.TransformModes.None && !main.GeeUI.LastClickCaptured,
				editor.TransformMode, main.GeeUI.LastClickCaptured
			);

			AddCommand
			(
				entity, main, "Cancel transform",
				new PCInput.Chord { Mouse = PCInput.MouseButton.RightMouseButton },
				editor.RevertTransform,
				gui.EntityCommands,
				() => editor.TransformMode.Value != Editor.TransformModes.None,
				editor.TransformMode
			);

			AddCommand
			(
				entity,
				main,
				"Rotate X",
				new PCInput.Chord { Key = Keys.X },
				editor.VoxelRotateX,
				gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.VoxelSelectionActive, editor.TransformMode
			);

			AddCommand
			(
				entity,
				main,
				"Rotate Y",
				new PCInput.Chord { Key = Keys.Y },
				editor.VoxelRotateY,
				gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.VoxelSelectionActive, editor.TransformMode
			);

			AddCommand
			(
				entity,
				main,
				"Rotate Z",
				new PCInput.Chord { Key = Keys.Z },
				editor.VoxelRotateZ,
				gui.VoxelCommands,
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelEditMode, editor.VoxelSelectionActive, editor.TransformMode
			);

#if DEVELOPMENT
			AnalyticsViewer.Bind(entity, main);

			AddCommand
			(
				entity, main, "Rebuild voxel adjacency",
				new PCInput.Chord(),
				new Command
				{
					Action = delegate()
					{
						foreach (Voxel v in Voxel.Voxels)
							v.RebuildAdjacency();
					}
				},
				gui.MapCommands
			);
#endif
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			// Cancel editor components
		}
	}
}
