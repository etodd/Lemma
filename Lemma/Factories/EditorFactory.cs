using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Input;
using BEPUphysics;

namespace Lemma.Factories
{
	public class EditorFactory : Factory
	{
		public EditorFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Editor");
			result.Serialize = false;
			Editor editor = new Editor();
			EditorUI ui = new EditorUI { Editable = false };
			Model model = new Model { Editable = false, Filename = new Property<string> { Value = "Models\\selector" }, Scale = new Property<Vector3> { Value = new Vector3(0.5f) } };

			UIRenderer uiRenderer = new UIRenderer { Editable = false };
			FPSInput input = new FPSInput { Editable = false };
			input.EnabledWhenPaused.Value = true;
			AudioListener audioListener = new AudioListener { Editable = false };

			Scroller scroller = new Scroller();
			scroller.Position.Value = new Vector2(10, 10);
			scroller.AnchorPoint.Value = new Vector2(0, 0);
			scroller.Name.Value = "Scroller";
			uiRenderer.Root.Children.Add(scroller);

			ListContainer uiList = new ListContainer();
			uiList.Name.Value = "PropertyList";
			uiList.AnchorPoint.Value = new Vector2(0, 0);
			scroller.Children.Add(uiList);

			Container popup = new Container();
			popup.Name.Value = "Popup";
			popup.Opacity.Value = 0.5f;
			popup.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			uiRenderer.Root.Children.Add(popup);

			ListContainer popupLayout = new ListContainer();
			popup.Children.Add(popupLayout);

			Container popupSearchContainer = new Container();
			popupSearchContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			popupLayout.Children.Add(popupSearchContainer);

			TextElement popupSearch = new TextElement();
			popupSearch.Name.Value = "PopupSearch";
			popupSearch.FontFile.Value = "Font";
			popupSearchContainer.Children.Add(popupSearch);

			Scroller popupScroller = new Scroller();
			popupScroller.Size.Value = new Vector2(200.0f, 300.0f);
			popupLayout.Children.Add(popupScroller);

			ListContainer popupList = new ListContainer();
			popupList.Name.Value = "PopupList";
			popupScroller.Children.Add(popupList);

			result.Add("Editor", editor);
			result.Add("UI", ui);
			result.Add("UIRenderer", uiRenderer);
			result.Add("Model", model);
			result.Add("Input", input);
			result.Add("AudioListener", audioListener);
			result.Add("StartSpawnPoint", new Property<string>());

			return result;
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

			Map.GlobalRaycastResult hit = Map.GlobalRaycast(rayStart, ray, closestEntityDistance);
			if (hit.Coordinate != null)
			{
				closestEntity = hit.Map.Entity;
				closestTransform = null;
			}
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			this.SetMain(result, main);
			Editor editor = result.Get<Editor>();
			EditorUI ui = result.Get<EditorUI>();
			Model model = result.Get<Model>("Model");
			FPSInput input = result.Get<FPSInput>("Input");
			UIRenderer uiRenderer = result.Get<UIRenderer>();

			ModelAlpha radiusVisual = new ModelAlpha();
			radiusVisual.Filename.Value = "Models\\alpha-sphere";
			radiusVisual.Color.Value = new Vector3(1.0f);
			radiusVisual.Alpha.Value = 0.1f;
			radiusVisual.Editable = false;
			radiusVisual.Serialize = false;
			radiusVisual.DrawOrder.Value = 11; // In front of water
			radiusVisual.DisableCulling.Value = true;
			result.Add(radiusVisual);
			radiusVisual.Add(new Binding<Matrix, Vector3>(radiusVisual.Transform, x => Matrix.CreateTranslation(x), editor.Position));
			radiusVisual.Add(new Binding<Vector3, int>(radiusVisual.Scale, x => new Vector3(x), editor.BrushSize));
			radiusVisual.Add(new Binding<bool>(radiusVisual.Enabled, () => editor.BrushSize > 1 && editor.MapEditMode, editor.BrushSize, editor.MapEditMode));
			radiusVisual.CullBoundingBox.Value = false;

			ModelAlpha selection = new ModelAlpha();
			selection.Filename.Value = "Models\\alpha-box";
			selection.Color.Value = new Vector3(1.0f, 0.7f, 0.4f);
			selection.Alpha.Value = 0.25f;
			selection.Editable = false;
			selection.Serialize = false;
			selection.DrawOrder.Value = 12; // In front of water and radius visualizer
			selection.DisableCulling.Value = true;
			result.Add(selection);
			selection.Add(new Binding<bool>(selection.Enabled, editor.VoxelSelectionActive));
			selection.Add(new Binding<Matrix>(selection.Transform, delegate()
			{
				const float padding = 0.1f;
				Map map = editor.SelectedEntities[0].Get<Map>();
				Vector3 start = map.GetRelativePosition(editor.VoxelSelectionStart) - new Vector3(0.5f), end = map.GetRelativePosition(editor.VoxelSelectionEnd) - new Vector3(0.5f);
				return Matrix.CreateScale((end - start) + new Vector3(padding)) * Matrix.CreateTranslation((start + end) * 0.5f) * map.Transform;
			}, () => selection.Enabled, editor.VoxelSelectionStart, editor.VoxelSelectionEnd));
			selection.CullBoundingBox.Value = false;

			Action<string, PCInput.Chord, Func<bool>, Command> addCommand = delegate(string description, PCInput.Chord chord, Func<bool> enabled, Command action)
			{
				ui.PopupCommands.Add(new EditorUI.PopupCommand { Description = description, Chord = chord, Enabled = enabled, Action = action });

				if (chord.Modifier != Keys.None)
					input.Add(new CommandBinding(input.GetChord(chord), () => enabled() && !ui.StringPropertyLocked, action));
				else
					input.Add(new CommandBinding(input.GetKeyDown(chord.Key), () => enabled() && !ui.StringPropertyLocked, action));
			};

			foreach (string key in Factory.factories.Keys)
			{
				string entityType = key;
				ui.PopupCommands.Add(new EditorUI.PopupCommand
				{
					Description = "Add " + entityType,
					Enabled = () => editor.SelectedEntities.Count == 0 && !editor.MapEditMode,
					Action = new Command { Action = () => editor.Spawn.Execute(entityType) },
				});
			}

			Scroller scroller = (Scroller)uiRenderer.Root.GetChildByName("Scroller");
			scroller.Add(new Binding<Vector2, Point>(scroller.Size, x => new Vector2(x.X - 10, x.Y - 20), main.ScreenSize));

			Container popup = (Container)uiRenderer.Root.GetChildByName("Popup");
			ListContainer popupList = (ListContainer)popup.GetChildByName("PopupList");

			input.Add(new CommandBinding(input.GetKeyUp(Keys.Space), () => !editor.MapEditMode && !ui.StringPropertyLocked && !editor.MovementEnabled, delegate()
			{
				Vector2 pos = input.Mouse;
				pos.X = Math.Min(main.ScreenSize.Value.X - popup.Size.Value.X, pos.X);
				pos.Y = Math.Min(main.ScreenSize.Value.Y - popup.Size.Value.Y, pos.Y);
				popup.Position.Value = pos;
				ui.PopupVisible.Value = true;
			}));

			input.Add(new CommandBinding(input.GetKeyUp(Keys.Escape), () => ui.PopupVisible, delegate()
			{
				if (ui.PopupSearchText.Value == "_")
					ui.PopupVisible.Value = false;
				else
					ui.ClearSelectedStringProperty();
			}));

			input.Add(new CommandBinding(input.RightMouseButtonUp, () => ui.PopupVisible, delegate()
			{
				ui.PopupVisible.Value = false;
			}));

			uiRenderer.Add(new Binding<bool>(popup.Visible, ui.PopupVisible));
			uiRenderer.Add(new Binding<string>(((TextElement)popup.GetChildByName("PopupSearch")).Text, ui.PopupSearchText));
			uiRenderer.Add(new ListBinding<UIComponent>(popupList.Children, ui.PopupElements));

			AudioListener audioListener = result.Get<AudioListener>();
			audioListener.Add(new Binding<Vector3>(audioListener.Position, main.Camera.Position));
			audioListener.Add(new Binding<Vector3>(audioListener.Forward, main.Camera.Forward));

			model.Add(new Binding<bool>(model.Enabled, editor.MapEditMode));
			model.Add(new Binding<Matrix>(model.Transform, () => editor.Orientation.Value * Matrix.CreateTranslation(editor.Position), editor.Position, editor.Orientation));

			editor.Add(new TwoWayBinding<string>(main.MapFile, editor.MapFile));

			result.Add(new TwoWayBinding<string>(((GameMain)main).StartSpawnPoint, result.GetProperty<string>("StartSpawnPoint")));

			uiRenderer.Add(new ListBinding<UIComponent>(uiRenderer.Root.GetChildByName("PropertyList").Children, ui.UIElements));
			ui.Add(new ListBinding<Entity>(ui.SelectedEntities, editor.SelectedEntities));
			ui.Add(new Binding<bool>(ui.MapEditMode, editor.MapEditMode));
			ui.Add(new Binding<bool>(ui.EnablePrecision, x => !x, input.GetKey(Keys.LeftShift)));
			editor.Add(new Binding<bool>(editor.MovementEnabled, () => !ui.StringPropertyLocked && (input.MiddleMouseButton || editor.MapEditMode), ui.StringPropertyLocked, input.MiddleMouseButton, editor.MapEditMode));
			ui.Add(new TwoWayBinding<bool>(editor.NeedsSave, ui.NeedsSave));

			editor.Add(new Binding<Vector2>(editor.Movement, input.Movement));
			editor.Add(new Binding<bool>(editor.Up, input.GetKey(Keys.Space)));
			editor.Add(new Binding<bool>(editor.Down, input.GetKey(Keys.LeftControl)));
			editor.Add(new Binding<bool>(editor.Empty, input.RightMouseButton));
			editor.Add(new Binding<bool>(editor.SpeedMode, input.GetKey(Keys.LeftShift)));
			editor.Add(new Binding<bool>(editor.Extend, input.GetKey(Keys.F)));
			editor.Add(new Binding<bool>(editor.Fill, input.LeftMouseButton));
			editor.Add(new Binding<bool>(editor.EditSelection, () => input.MiddleMouseButton && editor.MapEditMode, input.MiddleMouseButton, editor.MapEditMode));

			addCommand("Delete", new PCInput.Chord { Key = Keys.X }, () => editor.VoxelSelectionActive || (!editor.MapEditMode && editor.TransformMode.Value == Editor.TransformModes.None && editor.SelectedEntities.Count > 0), editor.DeleteSelected);
			addCommand("Duplicate", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.D }, () => !editor.MovementEnabled && editor.SelectedEntities.Count > 0, editor.Duplicate);

			// Start playing
			addCommand("Run", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.R }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					if (editor.NeedsSave)
						editor.Save.Execute();
					main.EditorEnabled.Value = false;
					IO.MapLoader.Load(main, null, main.MapFile);
				}
			});

			result.Add(new CommandBinding(main.MapLoaded, delegate()
			{
				editor.NeedsSave.Value = false;
			}));

			addCommand("Quit", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.Q }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					main.Exit();
				}
			});

			// Save
			addCommand("Save", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.S }, () => !editor.MovementEnabled, editor.Save);
			editor.Add(new CommandBinding(editor.Save, delegate()
			{
				Container container = new Container();
				container.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				container.Opacity.Value = 0.2f;
				container.AnchorPoint.Value = new Vector2(1.0f, 0.0f);
				container.Add(new Binding<Vector2, Point>(container.Position, x => new Vector2(x.X - 10.0f, 10.0f), main.ScreenSize));
				TextElement display = new TextElement();
				display.FontFile.Value = "Font";
				display.Text.Value = "Saved";
				container.Children.Add(display);
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

			// Deselect all entities
			addCommand("Deselect all", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.A }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					editor.SelectedEntities.Clear();
				}
			});

			int brush = 1;
			Action<int> changeBrush = delegate(int delta)
			{
				brush = 1 + ((brush - 1 + delta) % (WorldFactory.States.Count - 1));
				if (brush < 1)
					brush = WorldFactory.States.Count + ((brush - 1) % WorldFactory.States.Count);
				editor.Brush.Value = WorldFactory.StateList[brush].Name;
			};
			result.Add(new CommandBinding(input.GetKeyDown(Keys.Q), () => editor.MapEditMode, delegate()
			{
				changeBrush(-1);
			}));
			result.Add(new CommandBinding(input.GetKeyDown(Keys.E), () => editor.MapEditMode, delegate()
			{
				changeBrush(1);
			}));
			result.Add(new CommandBinding<int>(input.MouseScrolled, () => editor.MapEditMode && !input.GetKey(Keys.LeftAlt), delegate(int delta)
			{
				editor.BrushSize.Value = Math.Max(1, editor.BrushSize.Value + delta);
			}));

			editor.Add(new Binding<Vector2>(editor.Mouse, input.Mouse, () => !input.EnableLook));

			Camera camera = main.Camera;

			Property<float> cameraDistance = new Property<float> { Value = 10.0f };
			input.Add(new CommandBinding<int>(input.MouseScrolled, () => input.GetKey(Keys.LeftAlt), x => cameraDistance.Value = Math.Max(5, cameraDistance.Value + x * -2.0f)));
			input.Add(new Binding<bool>(input.EnableLook, () => editor.MapEditMode || (input.MiddleMouseButton && editor.TransformMode.Value == Editor.TransformModes.None), input.MiddleMouseButton, editor.MapEditMode, editor.TransformMode));
			input.Add(new Binding<Vector3, Vector2>(camera.Angles, x => new Vector3(-x.Y, x.X, 0.0f), input.Mouse, () => input.EnableLook));
			input.Add(new Binding<bool>(main.IsMouseVisible, x => !x, input.EnableLook));
			editor.Add(new Binding<Vector3>(camera.Position, () => editor.Position.Value - (camera.Forward.Value * cameraDistance), editor.Position, input.Mouse, cameraDistance));

			editor.Add(new CommandBinding(input.RightMouseButtonDown, () => !ui.PopupVisible && !editor.MapEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None, delegate()
			{
				// We're not editing a map
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
						Entity entity = editor.SelectedEntities.First();
						Command<Entity> toggleConnection = entity.GetCommand<Entity>("ToggleEntityConnected");
						if (toggleConnection != null)
						{
							toggleConnection.Execute(closestEntity);
							editor.NeedsSave.Value = true;
						}
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
				else if (editor.MapEditMode)
					editor.MapEditMode.Value = false;
			}));

			addCommand("Toggle voxel edit", new PCInput.Chord { Key = Keys.Tab }, delegate()
			{
				if (editor.TransformMode.Value != Editor.TransformModes.None)
					return false;

				if (editor.MapEditMode)
					return true;
				else
					return editor.SelectedEntities.Count == 1 && editor.SelectedEntities[0].Get<Map>() != null;
			},
			new Command
			{
				Action = delegate()
				{
					editor.MapEditMode.Value = !editor.MapEditMode;
				}
			});

			addCommand
			(
				"Grab (move)",
				new PCInput.Chord { Key = Keys.G },
				() => editor.SelectedEntities.Count > 0 && !input.EnableLook && !editor.MapEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartTranslation
			);
			addCommand
			(
				"Grab (move)",
				new PCInput.Chord { Key = Keys.G },
				() => editor.MapEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartVoxelTranslation
			);
			addCommand
			(
				"Duplicate",
				new PCInput.Chord { Key = Keys.C },
				() => editor.MapEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelDuplicate
			);
			addCommand
			(
				"Rotate",
				new PCInput.Chord { Key = Keys.R },
				() => editor.SelectedEntities.Count > 0 && !editor.MapEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartRotation
			);
			addCommand
			(
				"Lock X axis",
				new PCInput.Chord { Key = Keys.X },
				() => !editor.MapEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.X }
			);
			addCommand
			(
				"Lock Y axis",
				new PCInput.Chord { Key = Keys.Y },
				() => !editor.MapEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Y }
			);
			addCommand
			(
				"Lock Z axis",
				new PCInput.Chord { Key = Keys.Z },
				() => !editor.MapEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Z }
			);

			editor.Add(new CommandBinding
			(
				input.LeftMouseButtonDown,
				() => editor.TransformMode.Value != Editor.TransformModes.None,
				editor.CommitTransform
			));
			editor.Add(new CommandBinding
			(
				input.RightMouseButtonDown,
				() => editor.TransformMode.Value != Editor.TransformModes.None,
				editor.RevertTransform
			));
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			// Cancel editor components
		}
	}
}
