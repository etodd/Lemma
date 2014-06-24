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

		private void raycast(Main main, Vector3 ray, out Entity closestEntity, out Transform closestTransform)
		{
			closestEntity = null;
			float closestEntityDistance = main.Camera.FarPlaneDistance;
			closestTransform = null;
			Vector3 rayStart = main.Camera.Position;
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

		public static void AddCommand(Entity entity, Main main, string description, PCInput.Chord chord, Func<bool> enabled, Command action, ListProperty<Lemma.Components.EditorGeeUI.EditorCommand> list = null)
		{
			if (list != null)
				list.Add(new EditorGeeUI.EditorCommand { Description = description, Chord = chord, Enabled = enabled, Action = action });

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

			Func<bool> editorCommandsEnabled = entity.Get<Editor>().EnableCommands;
			entity.Add(new CommandBinding(action, delegate()
			{
				if (editorCommandsEnabled())
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

					UIRenderer uiRenderer = entity.Get<UIRenderer>();
					uiRenderer.Root.Children.Add(container);
					main.AddComponent(new Animation
					(
						new Animation.Parallel
						(
							new Animation.FloatMoveTo(container.Opacity, 0.0f, 1.0f),
							new Animation.FloatMoveTo(display.Opacity, 0.0f, 1.0f)
						),
						new Animation.Execute(delegate() { uiRenderer.Root.Children.Remove(container); })
					));
				}
			}));
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);

			Editor editor = new Editor();
			EditorGeeUI gui = new EditorGeeUI();
			gui.Add(new Binding<bool>(gui.Visible, x => !x, ConsoleUI.Showing));

			editor.EnableCommands = () => !gui.AnyTextFieldViewsSelected();

			Model model = new Model();
			model.Filename.Value = "Models\\selector";
			model.Scale.Value = new Vector3(0.5f);

			UIRenderer uiRenderer = new UIRenderer();
			FPSInput input = new FPSInput();
			input.EnabledWhenPaused = true;

			entity.Add("Editor", editor);
			entity.Add("GUI", gui);
			entity.Add("UIRenderer", uiRenderer);
			entity.Add("Model", model);
			entity.Add("Input", input);
			ModelAlpha radiusVisual = new ModelAlpha();
			radiusVisual.Filename.Value = "AlphaModels\\sphere";
			radiusVisual.Color.Value = new Vector3(1.0f);
			radiusVisual.Alpha.Value = 0.1f;
			radiusVisual.Serialize = false;
			radiusVisual.DrawOrder.Value = 11; // In front of water
			radiusVisual.DisableCulling.Value = true;
			entity.Add(radiusVisual);
			radiusVisual.Add(new Binding<Matrix, Vector3>(radiusVisual.Transform, x => Matrix.CreateTranslation(x), editor.Position));
			Action refreshRadius = delegate()
			{
				float s = 1.0f;
				if (editor.VoxelEditMode)
					s = editor.SelectedEntities[0].Get<Voxel>().Scale;
				radiusVisual.Scale.Value = new Vector3(s * editor.BrushSize);
			};
			radiusVisual.Add(new NotifyBinding(refreshRadius, editor.BrushSize));
			radiusVisual.Add(new ListNotifyBinding<Entity>(refreshRadius, editor.SelectedEntities));
			radiusVisual.Add(new Binding<bool>(radiusVisual.Enabled, () => editor.BrushSize > 1 && editor.VoxelEditMode, editor.BrushSize, editor.VoxelEditMode));
			radiusVisual.CullBoundingBox.Value = false;

			ModelAlpha selection = new ModelAlpha();
			selection.Filename.Value = "AlphaModels\\box";
			selection.Color.Value = new Vector3(1.0f, 0.7f, 0.4f);
			selection.Alpha.Value = 0.25f;
			selection.Serialize = false;
			selection.DrawOrder.Value = 12; // In front of water and radius visualizer
			selection.DisableCulling.Value = true;
			entity.Add(selection);
			selection.Add(new Binding<bool>(selection.Enabled, editor.VoxelSelectionActive));
			selection.Add(new Binding<Matrix>(selection.Transform, delegate()
			{
				const float padding = 0.1f;
				Voxel map = editor.SelectedEntities[0].Get<Voxel>();
				Vector3 start = map.GetRelativePosition(editor.VoxelSelectionStart) - new Vector3(0.5f), end = map.GetRelativePosition(editor.VoxelSelectionEnd) - new Vector3(0.5f);
				return Matrix.CreateScale((end - start) + new Vector3(padding)) * Matrix.CreateTranslation((start + end) * 0.5f) * map.Transform;
			}, () => selection.Enabled, editor.VoxelSelectionStart, editor.VoxelSelectionEnd));
			selection.CullBoundingBox.Value = false;

			AddCommand
			(
				entity, main, "Open", new PCInput.Chord(Keys.O, Keys.LeftControl),
				() => !input.EnableLook && !editor.VoxelEditMode
				&& editor.TransformMode.Value == Editor.TransformModes.None,
				new Command
				{
					Action = () =>
					{
						var dialog = new System.Windows.Forms.OpenFileDialog();
						dialog.Filter = "Map files|*.map";
						dialog.InitialDirectory = Path.Combine(main.Content.RootDirectory, IO.MapLoader.MapDirectory);
						if (dialog.ShowDialog() == DialogResult.OK)
							IO.MapLoader.Load(main, dialog.FileName, false);
					}
				},
				gui.MapCommands
			);

			AddCommand
			(
				entity, main, "New", new PCInput.Chord(Keys.N, Keys.LeftControl),
				() => !input.EnableLook && !editor.VoxelEditMode
				&& editor.TransformMode.Value == Editor.TransformModes.None,
				new Command
				{
					Action = () =>
					{
						var dialog = new System.Windows.Forms.SaveFileDialog();
						dialog.Filter = "Map files|*.map";
						dialog.InitialDirectory = Path.Combine(main.Content.RootDirectory, IO.MapLoader.MapDirectory);
						if (dialog.ShowDialog() == DialogResult.OK)
							IO.MapLoader.New(main, dialog.FileName);
					}
				},
				gui.MapCommands
			);

			AddCommand
			(
				entity, main, "Workshop Publish",
				new PCInput.Chord(Keys.W, Keys.LeftControl),
				() => !input.EnableLook && !editor.VoxelEditMode
				&& editor.TransformMode.Value == Editor.TransformModes.None,
				new Command
				{
					Action = delegate()
					{
						string f = main.MapFile;
						if (Path.GetExtension(f) != ".map")
							f += ".map";
						if (!Path.IsPathRooted(f))
							f = Path.GetFullPath(Path.Combine(main.Content.RootDirectory, IO.MapLoader.MapDirectory, f));
						main.AddComponent(new WorkShopInterface(f));
					}
				},
				gui.MapCommands
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
						Enabled = () => true,
						Action = new Command { Action = () => editor.Spawn.Execute(entityType) },
					});
				}
			}

			input.Add(new CommandBinding(input.GetKeyUp(Keys.Space), () => !editor.MovementEnabled && !gui.AnyTextFieldViewsSelected(), gui.ShowContextMenu));
			editor.Add(new Binding<bool>(main.GeeUI.KeyboardEnabled, () => !editor.VoxelEditMode && !editor.MovementEnabled, editor.VoxelEditMode, editor.MovementEnabled));

			model.Add(new Binding<bool>(model.Enabled, editor.VoxelEditMode));
			model.Add(new Binding<Matrix>(model.Transform, () => editor.Orientation.Value * Matrix.CreateTranslation(editor.Position), editor.Position, editor.Orientation));

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
			gui.Add(new Binding<bool>(gui.VoxelEditMode, editor.VoxelEditMode));
			gui.Add(new TwoWayBinding<bool>(editor.NeedsSave, gui.NeedsSave));
			gui.Add(new Binding<Editor.TransformModes>(gui.TransformMode, editor.TransformMode));

			Property<bool> movementEnabled = new Property<bool>();
			Property<bool> capslockKey = input.GetKey(Keys.CapsLock);
			entity.Add(new Binding<bool>(movementEnabled, () => input.MiddleMouseButton || capslockKey, input.MiddleMouseButton, capslockKey));

			editor.Add(new Binding<bool>(editor.MovementEnabled, () => movementEnabled || editor.VoxelEditMode, movementEnabled, editor.VoxelEditMode));

			editor.Add(new Binding<Vector2>(editor.Movement, input.Movement));
			editor.Add(new Binding<bool>(editor.Up, input.GetKey(Keys.Space)));
			editor.Add(new Binding<bool>(editor.Down, input.GetKey(Keys.LeftControl)));
			editor.Add(new Binding<bool>(editor.Empty, input.RightMouseButton));
			editor.Add(new Binding<bool>(editor.SpeedMode, input.GetKey(Keys.LeftShift)));
			editor.Add(new Binding<bool>(editor.Extend, input.GetKey(Keys.F)));
			editor.Add(new Binding<bool>(editor.Fill, input.LeftMouseButton));
			editor.Add(new Binding<bool>(editor.EditSelection, () => movementEnabled && editor.VoxelEditMode, movementEnabled, editor.VoxelEditMode));

			AddCommand(entity, main, "Delete", new PCInput.Chord { Key = Keys.X }, () => !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None && editor.SelectedEntities.Count > 0, editor.DeleteSelected, gui.EntityCommands);
			AddCommand(entity, main, "Duplicate", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.D }, () => !editor.MovementEnabled && editor.SelectedEntities.Count > 0 && editor.TransformMode.Value == Editor.TransformModes.None, editor.Duplicate, gui.EntityCommands);

			// Start playing
			AddCommand(entity, main, "Run", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.R }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					if (editor.NeedsSave)
						editor.Save.Execute();
					main.EditorEnabled.Value = false;
					IO.MapLoader.Load(main, main.MapFile);
				}
			}, gui.MapCommands);

			AddCommand(entity, main, "Join voxels", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.J }, () => !editor.VoxelEditMode && editor.SelectedEntities.Count == 2 && editor.SelectedEntities[0].Get<Voxel>() != null && editor.SelectedEntities[1].Get<Voxel>() != null, new Command
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
			}, gui.EntityCommands);

			entity.Add(new CommandBinding(main.MapLoaded, delegate()
			{
				editor.Position.Value = Vector3.Zero;
				editor.NeedsSave.Value = false;
			}));

			AddCommand(entity, main, "Quit", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.Q }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					throw new Main.ExitException();
				}
			}, gui.MapCommands);

			AddCommand(entity, main, "Help", new PCInput.Chord(), () => true, new Command
			{
				Action = delegate()
				{
					UIFactory.OpenURL("http://steamcommunity.com/sharedfiles/filedetails/?id=273022369");
				}
			}, gui.MapCommands);

			// Save
			AddCommand(entity, main, "Save", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.S }, () => !editor.MovementEnabled, editor.Save, gui.MapCommands);

			// Deselect all entities
			AddCommand(entity, main, "Deselect all", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.A }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					if (!gui.AnyTextFieldViewsSelected())
						editor.SelectedEntities.Clear();
				}
			}, gui.MapCommands);

			entity.Add(new CommandBinding<int>(input.MouseScrolled, () => editor.VoxelEditMode && !input.GetKey(Keys.LeftAlt), delegate(int delta)
			{
				editor.BrushSize.Value = Math.Max(1, editor.BrushSize.Value + delta);
			}));

			AddCommand(entity, main, "Voxel edit mode", new PCInput.Chord { Key = Keys.Tab }, delegate()
			{
				if (editor.TransformMode.Value != Editor.TransformModes.None)
					return false;

				if (InputManager.IsKeyPressed(Keys.LeftShift))
					return false;

				if (editor.VoxelEditMode)
					return true;
				else
					return editor.SelectedEntities.Count == 1 && editor.SelectedEntities[0].Get<Voxel>() != null;
			},
			new Command
			{
				Action = delegate()
				{
					if (!input.GetKey(Keys.LeftControl))
					{
						editor.VoxelEditMode.Value = !editor.VoxelEditMode;
						model.Scale.Value = new Vector3(editor.SelectedEntities[0].Get<Voxel>().Scale * 0.5f);
					}
				}
			}, gui.VoxelCommands);

			int brush = 0;
			Action<int> changeBrush = delegate(int delta)
			{
				int foundIndex = Voxel.StateList.FindIndex(x => x.ID == editor.Brush);
				if (foundIndex != -1)
					brush = foundIndex;
				int stateCount = Voxel.States.Count;
				brush = 1 + ((brush - 1 + delta) % (stateCount - 1));
				if (brush < 1)
					brush = stateCount + ((brush - 1) % stateCount);
				editor.Brush.Value = Voxel.StateList[brush].ID;
			};
			AddCommand(entity, main, "Previous material", new PCInput.Chord { Key = Keys.Q }, () => editor.VoxelEditMode, new Command
			{
				Action = delegate()
				{
					if (!input.GetKey(Keys.LeftShift))
						changeBrush(-1);
				}
			}, gui.VoxelCommands);

			AddCommand(entity, main, "Next material", new PCInput.Chord { Key = Keys.E }, () => editor.VoxelEditMode, new Command
			{
				Action = delegate()
				{
					if (!input.GetKey(Keys.LeftShift))
						changeBrush(1);
				}
			}, gui.VoxelCommands);

			AddCommand(entity, main, "Propagate", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.E }, () => editor.VoxelEditMode, editor.PropagateMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Intersect with selection", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.I }, () => editor.VoxelEditMode, editor.IntersectMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Propagate to selected box", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.R }, () => editor.VoxelEditMode, editor.PropagateMaterialBox, gui.VoxelCommands);
			AddCommand(entity, main, "Propagate non-contiguous", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.T }, () => editor.VoxelEditMode, editor.PropagateMaterialAll, gui.VoxelCommands);
			AddCommand(entity, main, "Sample", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Q }, () => editor.VoxelEditMode, editor.SampleMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Delete", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.X }, () => editor.VoxelEditMode, editor.DeleteMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Delete non-contiguous", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Z }, () => editor.VoxelEditMode, editor.DeleteMaterialAll, gui.VoxelCommands);

			gui.VoxelProperties.Add("Brush", new PropertyEntry(editor.Brush, "Brush"));
			gui.VoxelProperties.Add("Jitter", new PropertyEntry(editor.Jitter, "Jitter"));
			gui.VoxelProperties.Add("JitterOctave", new PropertyEntry(editor.JitterOctave, "Jitter Octave"));
			gui.VoxelProperties.Add("JitterOctaveMultiplier", new PropertyEntry(editor.JitterOctaveMultiplier, "Octave Multiplier"));
			gui.VoxelProperties.Add("BrushSize [scrollwheel]", new PropertyEntry(editor.BrushSize, "Brush size"));
			gui.VoxelProperties.Add("Coordinate", new PropertyEntry(editor.Coordinate, "Editor Coordinate") { Readonly = true, Visible = editor.VoxelEditMode });

			editor.Add(new Binding<Vector2>(editor.Mouse, input.Mouse, () => !input.EnableLook));

			uiRenderer.Add(new CommandBinding(uiRenderer.SwallowMouseEvents, (Action)input.SwallowEvents));

			Camera camera = main.Camera;

			input.Add(new CommandBinding<int>(input.MouseScrolled, () => input.GetKey(Keys.LeftAlt), delegate(int delta)
			{
				editor.CameraDistance.Value = Math.Max(1, editor.CameraDistance.Value + delta * -2.0f);
			}));
			input.Add(new Binding<bool>(input.EnableLook, () => editor.VoxelEditMode || (movementEnabled && editor.TransformMode.Value == Editor.TransformModes.None), movementEnabled, editor.VoxelEditMode, editor.TransformMode));
			input.Add(new Binding<Vector3, Vector2>(camera.Angles, x => new Vector3(-x.Y, x.X, 0.0f), input.Mouse, () => input.EnableLook));
			input.Add(new Binding<bool>(main.IsMouseVisible, x => !x, input.EnableLook));
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
				() => true,
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
				Microsoft.Xna.Framework.Graphics.Viewport viewport = main.GraphicsDevice.Viewport;
				Vector3 ray = Vector3.Normalize(viewport.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.Projection, camera.View, Matrix.Identity) - viewport.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.Projection, camera.View, Matrix.Identity));

				Entity closestEntity;
				Transform closestTransform;
				this.raycast(main, ray, out closestEntity, out closestTransform);

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
				() => editor.SelectedEntities.Count > 0 && !input.EnableLook && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartTranslation,
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel Grab (move)",
				new PCInput.Chord { Key = Keys.G },
				() => editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartVoxelTranslation,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel duplicate",
				new PCInput.Chord { Key = Keys.C },
				() => editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelDuplicate,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel copy",
				new PCInput.Chord { Key = Keys.Y },
				() => editor.VoxelEditMode,
				editor.VoxelCopy,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel paste",
				new PCInput.Chord { Key = Keys.P },
				() => editor.VoxelEditMode,
				editor.VoxelPaste,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Rotate",
				new PCInput.Chord { Key = Keys.R },
				() => editor.SelectedEntities.Count > 0 && !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartRotation,
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Lock X axis",
				new PCInput.Chord { Key = Keys.X },
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.X },
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Lock Y axis",
				new PCInput.Chord { Key = Keys.Y },
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Y },
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Lock Z axis",
				new PCInput.Chord { Key = Keys.Z },
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Z },
				gui.EntityCommands
			);

			AddCommand
			(
				entity,
				main,
				"Clear rotation",
				new PCInput.Chord { },
				() => !editor.VoxelEditMode && editor.SelectedEntities.Count > 0 && editor.TransformMode.Value == Editor.TransformModes.None,
				new Command
				{
					Action = delegate()
					{
						foreach (Entity e in editor.SelectedEntities)
							e.Get<Transform>().Orientation.Value = Matrix.Identity;
					}
				},
				gui.EntityCommands
			);

			AddCommand
			(
				entity,
				main,
				"Clear translation",
				new PCInput.Chord { },
				() => !editor.VoxelEditMode && editor.SelectedEntities.Count > 0 && editor.TransformMode.Value == Editor.TransformModes.None,
				new Command
				{
					Action = delegate()
					{
						foreach (Entity e in editor.SelectedEntities)
							e.Get<Transform>().Position.Value = Vector3.Zero;
					}
				},
				gui.EntityCommands
			);

			MemoryStream yankBuffer = null;
			AddCommand
			(
				entity,
				main,
				"Copy",
				new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.C },
				() => !editor.VoxelEditMode && !input.EnableLook && editor.SelectedEntities.Count > 0 && editor.TransformMode.Value == Editor.TransformModes.None,
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
				gui.EntityCommands
			);

			AddCommand
			(
				entity,
				main,
				"Paste",
				new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.V },
				() => !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode == Editor.TransformModes.None,
				new Command
				{
					Action = delegate()
					{
						if (yankBuffer != null)
						{
							yankBuffer.Seek(0, SeekOrigin.Begin);
							List<Entity> entities = (List<Entity>)IO.MapLoader.Serializer.Deserialize(yankBuffer);

							foreach (Entity e in entities)
							{
								e.ClearGUID();
								e.ID.Value = "";
								Factory<Main> factory = Factory<Main>.Get(e.Type);
								factory.Bind(e, main);
								main.Add(e);
							}

							editor.SelectedEntities.Clear();
							foreach (Entity e in entities)
								editor.SelectedEntities.Add(e);
							editor.StartTranslation.Execute();
						}
					}
				},
				gui.EntityCommands
			);

			AddCommand
			(
				entity, main, "Commit transform",
				new PCInput.Chord { Mouse = PCInput.MouseButton.LeftMouseButton },
				() => editor.TransformMode.Value != Editor.TransformModes.None,
				editor.CommitTransform,
				gui.EntityCommands
			);

			AddCommand
			(
				entity, main, "Cancel transform",
				new PCInput.Chord { Mouse = PCInput.MouseButton.RightMouseButton },
				() => editor.TransformMode.Value != Editor.TransformModes.None,
				editor.RevertTransform,
				gui.EntityCommands
			);

#if DEVELOPMENT
			AnalyticsViewer.Bind(entity, main);
#endif
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			// Cancel editor components
		}
	}
}
