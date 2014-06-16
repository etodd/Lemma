﻿using System;
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

			Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(rayStart, ray, closestEntityDistance, true);
			if (hit.Coordinate != null)
			{
				closestEntity = hit.Voxel.Entity;
				closestTransform = null;
			}
		}

		public static void AddCommand(Entity entity, Main main, string description, PCInput.Chord chord, Func<bool> enabled, Command action, ListProperty<Lemma.Components.EditorGeeUI.PopupCommand> list = null)
		{
			if (list != null)
				list.Add(new EditorGeeUI.PopupCommand { Description = description, Chord = chord, Enabled = enabled, Action = action });

			PCInput input = entity.Get<PCInput>();
			if (chord.Modifier != Keys.None)
				input.Add(new CommandBinding(input.GetChord(chord), enabled, action));
			else
				input.Add(new CommandBinding(input.GetKeyDown(chord.Key), enabled, action));

			entity.Add(new CommandBinding(action, delegate()
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
			}));
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);

			Editor editor = new Editor();
			EditorGeeUI gui = new EditorGeeUI();
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
			radiusVisual.Filename.Value = "Models\\alpha-sphere";
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
			selection.Filename.Value = "Models\\alpha-box";
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
				() => !gui.AnyTextFieldViewsSelected() && !input.EnableLook && !editor.VoxelEditMode
				&& editor.TransformMode.Value == Editor.TransformModes.None,
				new Command
				{
					Action = () =>
					{
						var dialog = new System.Windows.Forms.OpenFileDialog();
						dialog.Filter = "Map files|*.map";
						dialog.InitialDirectory = Directory.GetCurrentDirectory();
						var result = dialog.ShowDialog();
						string file = result == DialogResult.OK ? dialog.FileName : "";
						if (file != "") IO.MapLoader.Load(main, "", file);
					}
				},
				gui.MapCommands
			);

			AddCommand
			(
				entity, main, "Workshop Publish",
				new PCInput.Chord(Keys.W, Keys.LeftControl),
				() => !gui.AnyTextFieldViewsSelected() && !input.EnableLook && !editor.VoxelEditMode
				&& editor.TransformMode.Value == Editor.TransformModes.None,
				new Command
				{
					Action = delegate()
					{
						main.AddComponent(new WorkShopInterface("content/game/" + main.MapFile.Value + ".map"));
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
					gui.AddEntityCommands.Add(new EditorGeeUI.PopupCommand
					{
						Description = "Add " + entityType,
						Enabled = () => true,
						Action = new Command { Action = () => editor.Spawn.Execute(entityType) },
					});
				}
			}

			input.Add(new CommandBinding(input.GetKeyUp(Keys.Space), () => !editor.VoxelEditMode && !editor.MovementEnabled, gui.ShowAddMenu));
			editor.Add(new Binding<bool>(main.GeeUI.KeyboardEnabled, () => !editor.VoxelEditMode && !editor.MovementEnabled, editor.VoxelEditMode, editor.MovementEnabled));

			model.Add(new Binding<bool>(model.Enabled, editor.VoxelEditMode));
			model.Add(new Binding<Matrix>(model.Transform, () => editor.Orientation.Value * Matrix.CreateTranslation(editor.Position), editor.Position, editor.Orientation));

			editor.Add(new TwoWayBinding<string>(main.MapFile, editor.MapFile));
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

			entity.Add(new TwoWayBinding<string>(main.Spawner.StartSpawnPoint, editor.StartSpawnPoint));

			gui.Add(new ListBinding<Entity>(gui.SelectedEntities, editor.SelectedEntities));
			gui.Add(new Binding<bool>(gui.MapEditMode, editor.VoxelEditMode));
			gui.Add(new Binding<bool>(gui.EnablePrecision, x => !x, input.GetKey(Keys.LeftShift)));
			gui.Add(new TwoWayBinding<bool>(editor.NeedsSave, gui.NeedsSave));

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

			AddCommand(entity, main, "Delete", new PCInput.Chord { Key = Keys.X }, () => !gui.AnyTextFieldViewsSelected() && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None && editor.SelectedEntities.Count > 0, editor.DeleteSelected, gui.EntityCommands);
			AddCommand(entity, main, "Duplicate", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.D }, () => !gui.AnyTextFieldViewsSelected() && !editor.MovementEnabled && editor.SelectedEntities.Count > 0, editor.Duplicate, gui.EntityCommands);

			// Start playing
			AddCommand(entity, main, "Run", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.R }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					if (editor.NeedsSave)
						editor.Save.Execute();
					main.EditorEnabled.Value = false;
					IO.MapLoader.Load(main, null, main.MapFile);
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

			// Save
			AddCommand(entity, main, "Save", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.S }, () => !editor.MovementEnabled && !gui.AnyTextFieldViewsSelected(), editor.Save, gui.MapCommands);

			// Deselect all entities
			AddCommand(entity, main, "Deselect all", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.A }, () => !editor.MovementEnabled && !gui.AnyTextFieldViewsSelected(), new Command
			{
				Action = delegate()
				{
					editor.SelectedEntities.Clear();
				}
			}, gui.MapCommands);

			int brush = 1;
			Action<int> changeBrush = delegate(int delta)
			{
				int foundIndex = Voxel.StateList.FindIndex(x => x.ToString() == editor.Brush);
				if (foundIndex != -1)
					brush = foundIndex;
				int stateCount = Voxel.States.Count;
				brush = 1 + ((brush - 1 + delta) % (stateCount - 1));
				if (brush < 1)
					brush = stateCount + ((brush - 1) % stateCount);
				editor.Brush.Value = Voxel.StateList[brush].ToString();
			};
			entity.Add(new CommandBinding(input.GetKeyDown(Keys.Q), () => editor.VoxelEditMode, delegate()
			{
				changeBrush(-1);
			}));
			entity.Add(new CommandBinding(input.GetKeyDown(Keys.E), () => editor.VoxelEditMode && !input.GetKey(Keys.LeftShift), delegate()
			{
				changeBrush(1);
			}));
			entity.Add(new CommandBinding<int>(input.MouseScrolled, () => editor.VoxelEditMode && !input.GetKey(Keys.LeftAlt), delegate(int delta)
			{
				editor.BrushSize.Value = Math.Max(1, editor.BrushSize.Value + delta);
			}));

			AddCommand(entity, main, "Propagate current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.E }, () => editor.VoxelEditMode, editor.PropagateMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Intersect current material with selection", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.I }, () => editor.VoxelEditMode, editor.IntersectMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Propagate current material to selected box", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.R }, () => editor.VoxelEditMode, editor.PropagateMaterialBox, gui.VoxelCommands);
			AddCommand(entity, main, "Propagate current material (non-contiguous)", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.T }, () => editor.VoxelEditMode, editor.PropagateMaterialAll, gui.VoxelCommands);
			AddCommand(entity, main, "Sample current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Q }, () => editor.VoxelEditMode, editor.SampleMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Delete current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.X }, () => editor.VoxelEditMode, editor.DeleteMaterial, gui.VoxelCommands);
			AddCommand(entity, main, "Delete current material (non-contiguous)", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Z }, () => editor.VoxelEditMode, editor.DeleteMaterialAll, gui.VoxelCommands);

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

				if (closestEntity != null)
				{
					if (editor.SelectedEntities.Count == 1 && input.GetKey(Keys.LeftControl).Value)
					{
						// The user is trying to connect the two entities
						Entity selectedEntity = editor.SelectedEntities.First();
						selectedEntity.ToggleEntityConnection.Execute(closestEntity);
						editor.NeedsSave.Value = true;
						return;
					}

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
			}));

			AddCommand(entity, main, "Toggle voxel edit", new PCInput.Chord { Key = Keys.Tab }, delegate()
			{
				if (editor.TransformMode.Value != Editor.TransformModes.None)
					return false;

				if (input.GetKey(Keys.LeftControl))
					return false;

				if (gui.AnyTextFieldViewsSelected())
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
					editor.VoxelEditMode.Value = !editor.VoxelEditMode;
					model.Scale.Value = new Vector3(editor.SelectedEntities[0].Get<Voxel>().Scale * 0.5f);
				}
			}, gui.VoxelCommands);

			AddCommand
			(
				entity,
				main,
				"Grab",
				new PCInput.Chord { Key = Keys.G },
				() => !gui.AnyTextFieldViewsSelected() && editor.SelectedEntities.Count > 0 && !input.EnableLook && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartTranslation,
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel Grab (move)",
				new PCInput.Chord { Key = Keys.G },
				() => !gui.AnyTextFieldViewsSelected() && editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartVoxelTranslation,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel duplicate",
				new PCInput.Chord { Key = Keys.C },
				() => !gui.AnyTextFieldViewsSelected() && editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelDuplicate,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel yank",
				new PCInput.Chord { Key = Keys.Y },
				() => !gui.AnyTextFieldViewsSelected() && editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelCopy,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Voxel paste",
				new PCInput.Chord { Key = Keys.P },
				() => !gui.AnyTextFieldViewsSelected() && editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelPaste,
				gui.VoxelCommands
			);
			AddCommand
			(
				entity,
				main,
				"Rotate",
				new PCInput.Chord { Key = Keys.R },
				() => !gui.AnyTextFieldViewsSelected() && editor.SelectedEntities.Count > 0 && !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartRotation,
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Lock X axis",
				new PCInput.Chord { Key = Keys.X },
				() => !gui.AnyTextFieldViewsSelected() && !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.X },
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Lock Y axis",
				new PCInput.Chord { Key = Keys.Y },
				() => !gui.AnyTextFieldViewsSelected() && !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Y },
				gui.EntityCommands
			);
			AddCommand
			(
				entity,
				main,
				"Lock Z axis",
				new PCInput.Chord { Key = Keys.Z },
				() => !gui.AnyTextFieldViewsSelected() && !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Z },
				gui.EntityCommands
			);

			AddCommand
			(
				entity,
				main,
				"Clear rotation",
				new PCInput.Chord { },
				() => !editor.VoxelEditMode && editor.SelectedEntities.Count > 0,
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
				() => !editor.VoxelEditMode && editor.SelectedEntities.Count > 0,
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
				"Yank",
				new PCInput.Chord { Key = Keys.Y },
				() => !gui.AnyTextFieldViewsSelected() && !editor.VoxelEditMode && !input.EnableLook && editor.SelectedEntities.Count > 0 && editor.TransformMode.Value == Editor.TransformModes.None,
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
				new PCInput.Chord { Key = Keys.P },
				() => !gui.AnyTextFieldViewsSelected() && !editor.VoxelEditMode && !input.EnableLook,
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

			editor.Add(new CommandBinding
			(
				input.LeftMouseButtonDown,
				() => editor.TransformMode.Value != Editor.TransformModes.None && !main.GeeUI.LastClickCaptured,
				editor.CommitTransform
			));
			editor.Add(new CommandBinding
			(
				input.RightMouseButtonDown,
				() => editor.TransformMode.Value != Editor.TransformModes.None && !main.GeeUI.LastClickCaptured,
				editor.RevertTransform
			));
			
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
