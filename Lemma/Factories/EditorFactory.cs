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

		private class EventEntry
		{
			public string Name;
			public Property<bool> Active = new Property<bool>();
		}

		private class SessionEntry
		{
			public Session Session;
			public Property<bool> Active = new Property<bool>();
		}

		private class PropertyEntry
		{
			public string Name;
			public Property<bool> Active = new Property<bool>();
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
			scroller.ResizeHorizontal.Value = true;
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
			result.Add("ProceduralGenerator", new ProceduralGenerator());

			return result;
		}

		private Vector4 colorHash(string eventName)
		{
			byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(ASCIIEncoding.UTF8.GetBytes(eventName));
			Vector3 color = new Vector3(hash[0] / 255.0f, hash[1] / 255.0f, hash[2] / 255.0f);
			color.Normalize();
			return new Vector4(color, 1.0f);
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

			Map.GlobalRaycastResult hit = Map.GlobalRaycast(rayStart, ray, closestEntityDistance, true);
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
			radiusVisual.Add(new Binding<Vector3, int>(radiusVisual.Scale, delegate(int brushSize)
			{
				float s = 1.0f;
				if (editor.MapEditMode)
					s = editor.SelectedEntities[0].Get<Map>().Scale;
				return new Vector3(s * brushSize);
			}, editor.BrushSize));
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

				ui.Add(new CommandBinding(action, delegate()
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

			addCommand("Delete", new PCInput.Chord { Key = Keys.X }, () => !editor.MapEditMode && editor.TransformMode.Value == Editor.TransformModes.None && editor.SelectedEntities.Count > 0, editor.DeleteSelected);
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

			addCommand("Join voxels", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.J }, () => !editor.MapEditMode && editor.SelectedEntities.Count == 2 && editor.SelectedEntities[0].Get<Map>() != null && editor.SelectedEntities[1].Get<Map>() != null, new Command
			{
				Action = delegate()
				{
					Entity entity1 = editor.SelectedEntities[0];
					Entity entity2 = editor.SelectedEntities[1];

					Map map1 = entity1.Get<Map>();
					Map map2 = entity2.Get<Map>();

					foreach (Map.Chunk chunk in map1.Chunks)
					{
						foreach (Map.Box box in chunk.Boxes)
						{
							foreach (Map.Coordinate coord in box.GetCoords())
								map2.Fill(map1.GetAbsolutePosition(coord), box.Type, false);
						}
					}
					map2.Regenerate();
					entity1.Delete.Execute();
					editor.NeedsSave.Value = true;
				}
			});

			result.Add(new CommandBinding(main.MapLoaded, delegate()
			{
				editor.Position.Value = Vector3.Zero;
				editor.NeedsSave.Value = false;
			}));

			addCommand("Quit", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.Q }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					throw new GameMain.ExitException();
				}
			});

			Property<bool> analyticsEnable = new Property<bool>();
			ListProperty<SessionEntry> analyticsSessions = new ListProperty<SessionEntry>();
			ListProperty<SessionEntry> analyticsActiveSessions = new ListProperty<SessionEntry>();
			ListProperty<EventEntry> analyticsEvents = new ListProperty<EventEntry>();
			ListProperty<EventEntry> analyticsActiveEvents = new ListProperty<EventEntry>();
			ListProperty<PropertyEntry> analyticsProperties = new ListProperty<PropertyEntry>();
			ListProperty<PropertyEntry> analyticsActiveProperties = new ListProperty<PropertyEntry>();
			Dictionary<Session, ModelInstance> sessionPositionModels = new Dictionary<Session, ModelInstance>();
			Dictionary<Session.EventList, List<ModelInstance>> eventPositionModels = new Dictionary<Session.EventList, List<ModelInstance>>();
			Property<bool> analyticsPlaying = new Property<bool>();
			Property<float> playbackSpeed = new Property<float> { Value = 1.0f };
			Property<float> playbackLocation = new Property<float>();

			const float timelineHeight = 32.0f;
			
			Scroller timelineScroller = new Scroller();
			timelineScroller.ScrollAmount.Value = 60.0f;
			timelineScroller.EnableScissor.Value = false;
			timelineScroller.DefaultScrollHorizontal.Value = true;
			timelineScroller.AnchorPoint.Value = new Vector2(0, 1);
			timelineScroller.ResizeVertical.Value = true;
			timelineScroller.Add(new Binding<Vector2, Point>(timelineScroller.Position, x => new Vector2(0, x.Y), main.ScreenSize));
			timelineScroller.Add(new Binding<Vector2, Point>(timelineScroller.Size, x => new Vector2(x.X, timelineHeight), main.ScreenSize));
			timelineScroller.Add(new Binding<bool>(timelineScroller.Visible, analyticsEnable));
			timelineScroller.Add(new Binding<bool>(timelineScroller.EnableScroll, x => !x, input.GetKey(Keys.LeftAlt)));
			uiRenderer.Root.Children.Add(timelineScroller);

			scroller.Add(new Binding<Vector2>(scroller.Size, () => new Vector2(scroller.Size.Value.X, main.ScreenSize.Value.Y - 20 - timelineScroller.ScaledSize.Value.Y), main.ScreenSize, timelineScroller.ScaledSize));

			ListContainer timelines = new ListContainer();
			timelines.Alignment.Value = ListContainer.ListAlignment.Min;
			timelines.Orientation.Value = ListContainer.ListOrientation.Vertical;
			timelines.Reversed.Value = true;
			timelineScroller.Children.Add(timelines);

			Container timeline = new Container();
			timeline.Size.Value = new Vector2(0, timelineHeight);
			timeline.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			timeline.ResizeHorizontal.Value = false;
			timeline.ResizeVertical.Value = false;
			timelines.Children.Add(timeline);

			ui.PopupCommands.Add(new EditorUI.PopupCommand
			{
				Description = "Load analytics data",
				Enabled = () => editor.SelectedEntities.Count == 0 && !editor.MapEditMode && !analyticsEnable,
				Action = new Command
				{
					Action = delegate()
					{
						if (main.MapFile.Value != null)
						{
							List<Session> sessions = ((GameMain)main).LoadAnalytics(main.MapFile);
							if (sessions.Count > 0)
							{
								analyticsEnable.Value = true;
								Dictionary<string, bool> distinctEventNames = new Dictionary<string, bool>();
								Dictionary<string, bool> distinctPropertyNames = new Dictionary<string, bool>();
								foreach (Session s in sessions)
								{
									foreach (Session.EventList el in s.Events)
									{
										distinctEventNames[el.Name] = true;
										s.TotalTime = Math.Max(s.TotalTime, el.Events[el.Events.Count - 1].Time);
									}
									foreach (Session.ContinuousProperty p in s.ContinuousProperties)
									{
										if (p.Independent)
											distinctPropertyNames[p.Name] = true;
									}
									analyticsSessions.Add(new SessionEntry { Session = s });
								}
								analyticsEvents.Add(distinctEventNames.Keys.Select(x => new EventEntry { Name = x }));
								analyticsProperties.Add(distinctPropertyNames.Keys.Select(x => new PropertyEntry { Name = x }));
								timeline.Size.Value = new Vector2(analyticsSessions.Max(x => x.Session.TotalTime), timelineScroller.Size.Value.Y);
								timelines.Scale.Value = new Vector2(timelineScroller.Size.Value.X / timeline.Size.Value.X, 1.0f);
							}
						}
					}
				},
			});

			ListContainer sessionsSidebar = new ListContainer();
			sessionsSidebar.AnchorPoint.Value = new Vector2(1, 1);
			sessionsSidebar.Add(new Binding<Vector2>(sessionsSidebar.Position, () => new Vector2(main.ScreenSize.Value.X - 10, main.ScreenSize.Value.Y - timelineScroller.ScaledSize.Value.Y - 10), main.ScreenSize, timelineScroller.ScaledSize));
			sessionsSidebar.Add(new Binding<bool>(sessionsSidebar.Visible, analyticsEnable));
			sessionsSidebar.Alignment.Value = ListContainer.ListAlignment.Max;
			sessionsSidebar.Reversed.Value = true;
			uiRenderer.Root.Children.Add(sessionsSidebar);

			Func<string, ListContainer> createCheckboxListItem = delegate(string text)
			{
				ListContainer layout = new ListContainer();
				layout.Orientation.Value = ListContainer.ListOrientation.Horizontal;

				TextElement label = new TextElement();
				label.FontFile.Value = "Font";
				label.Text.Value = text;
				label.Name.Value = "Label";
				layout.Children.Add(label);

				Container checkboxContainer = new Container();
				checkboxContainer.PaddingBottom.Value = checkboxContainer.PaddingLeft.Value = checkboxContainer.PaddingRight.Value = checkboxContainer.PaddingTop.Value = 1.0f;
				layout.Children.Add(checkboxContainer);

				Container checkbox = new Container();
				checkbox.Name.Value = "Checkbox";
				checkbox.ResizeHorizontal.Value = checkbox.ResizeVertical.Value = false;
				checkbox.Size.Value = new Vector2(16.0f, 16.0f);
				checkboxContainer.Children.Add(checkbox);
				return layout;
			};

			Container sessionsContainer = new Container();
			sessionsContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			sessionsContainer.Opacity.Value = 0.5f;
			sessionsContainer.AnchorPoint.Value = new Vector2(1, 1);
			sessionsSidebar.Children.Add(sessionsContainer);

			Scroller sessionsScroller = new Scroller();
			sessionsScroller.ResizeHorizontal.Value = true;
			sessionsScroller.ResizeVertical.Value = true;
			sessionsScroller.MaxVerticalSize.Value = 256;
			sessionsContainer.Children.Add(sessionsScroller);

			ListContainer sessionList = new ListContainer();
			sessionList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			sessionList.Alignment.Value = ListContainer.ListAlignment.Max;
			sessionsScroller.Children.Add(sessionList);

			Property<bool> allSessions = new Property<bool>();

			sessionList.Add(new ListBinding<UIComponent, SessionEntry>(sessionList.Children, analyticsSessions, delegate(SessionEntry entry)
			{
				ListContainer item = createCheckboxListItem(entry.Session.Date.ToShortDateString() + " (" + new TimeSpan(0, 0, (int)entry.Session.TotalTime).ToString() + ")");

				Container checkbox = (Container)item.GetChildByName("Checkbox");
				checkbox.Add(new Binding<Microsoft.Xna.Framework.Color, bool>(checkbox.Tint, x => x ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Black, entry.Active));

				item.Add(new CommandBinding<Point>(item.MouseLeftDown, delegate(Point p)
				{
					if (entry.Active)
					{
						allSessions.Value = false;
						analyticsActiveSessions.Remove(entry);
					}
					else
						analyticsActiveSessions.Add(entry);
				}));

				return new[] { item };
			}));

			ListContainer allSessionsButton = createCheckboxListItem("[All]");
			allSessionsButton.Add(new CommandBinding<Point>(allSessionsButton.MouseLeftDown, delegate(Point p)
			{
				if (allSessions)
				{
					allSessions.Value = false;
					foreach (SessionEntry s in analyticsActiveSessions.ToList())
						analyticsActiveSessions.Remove(s);
				}
				else
				{
					allSessions.Value = true;
					foreach (SessionEntry s in analyticsSessions)
					{
						if (!s.Active)
							analyticsActiveSessions.Add(s);
					}
				}
			}));

			Container allSessionsCheckbox = (Container)allSessionsButton.GetChildByName("Checkbox");
			allSessionsCheckbox.Add(new Binding<Microsoft.Xna.Framework.Color, bool>(allSessionsCheckbox.Tint, x => x ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Black, allSessions));
			sessionList.Children.Add(allSessionsButton);

			Container eventsContainer = new Container();
			eventsContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			eventsContainer.Opacity.Value = 0.5f;
			eventsContainer.AnchorPoint.Value = new Vector2(1, 1);
			sessionsSidebar.Children.Add(eventsContainer);

			Scroller eventsScroller = new Scroller();
			eventsScroller.ResizeHorizontal.Value = true;
			eventsScroller.ResizeVertical.Value = true;
			eventsScroller.MaxVerticalSize.Value = 256;
			eventsContainer.Children.Add(eventsScroller);

			ListContainer eventList = new ListContainer();
			eventList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			eventList.Alignment.Value = ListContainer.ListAlignment.Max;
			eventsScroller.Children.Add(eventList);

			Property<bool> allEvents = new Property<bool>();

			eventList.Add(new ListBinding<UIComponent, EventEntry>(eventList.Children, analyticsEvents, delegate(EventEntry e)
			{
				ListContainer item = createCheckboxListItem(e.Name);

				Container checkbox = (Container)item.GetChildByName("Checkbox");
				checkbox.Add(new Binding<Microsoft.Xna.Framework.Color, bool>(checkbox.Tint, x => x ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Black, e.Active));

				TextElement label = (TextElement)item.GetChildByName("Label");
				label.Tint.Value = new Microsoft.Xna.Framework.Color(this.colorHash(e.Name));

				item.Add(new CommandBinding<Point>(item.MouseLeftDown, delegate(Point p)
				{
					if (e.Active)
					{
						allEvents.Value = false;
						analyticsActiveEvents.Remove(e);
					}
					else
						analyticsActiveEvents.Add(e);
				}));

				return new[] { item };
			}));

			ListContainer allEventsButton = createCheckboxListItem("[All]");
			allEventsButton.Add(new CommandBinding<Point>(allEventsButton.MouseLeftDown, delegate(Point p)
			{
				if (allEvents)
				{
					allEvents.Value = false;
					foreach (EventEntry e in analyticsActiveEvents.ToList())
						analyticsActiveEvents.Remove(e);
				}
				else
				{
					allEvents.Value = true;
					foreach (EventEntry e in analyticsEvents)
					{
						if (!e.Active)
							analyticsActiveEvents.Add(e);
					}
				}
			}));
			Container allEventsCheckbox = (Container)allEventsButton.GetChildByName("Checkbox");
			allEventsCheckbox.Add(new Binding<Microsoft.Xna.Framework.Color, bool>(allEventsCheckbox.Tint, x => x ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Black, allEvents));
			eventList.Children.Add(allEventsButton);

			Container propertiesContainer = new Container();
			propertiesContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			propertiesContainer.Opacity.Value = 0.5f;
			propertiesContainer.AnchorPoint.Value = new Vector2(1, 1);
			sessionsSidebar.Children.Add(propertiesContainer);

			Scroller propertiesScroller = new Scroller();
			propertiesScroller.ResizeHorizontal.Value = true;
			propertiesScroller.ResizeVertical.Value = true;
			propertiesScroller.MaxVerticalSize.Value = 256;
			propertiesContainer.Children.Add(propertiesScroller);

			ListContainer propertiesList = new ListContainer();
			propertiesList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			propertiesList.Alignment.Value = ListContainer.ListAlignment.Max;
			propertiesScroller.Children.Add(propertiesList);

			Property<bool> allProperties = new Property<bool>();

			propertiesList.Add(new ListBinding<UIComponent, PropertyEntry>(propertiesList.Children, analyticsProperties, delegate(PropertyEntry e)
			{
				ListContainer item = createCheckboxListItem(e.Name);

				Container checkbox = (Container)item.GetChildByName("Checkbox");
				checkbox.Add(new Binding<Microsoft.Xna.Framework.Color, bool>(checkbox.Tint, x => x ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Black, e.Active));

				TextElement label = (TextElement)item.GetChildByName("Label");
				label.Tint.Value = new Microsoft.Xna.Framework.Color(this.colorHash(e.Name));

				item.Add(new CommandBinding<Point>(item.MouseLeftDown, delegate(Point p)
				{
					if (e.Active)
					{
						allProperties.Value = false;
						analyticsActiveProperties.Remove(e);
					}
					else
						analyticsActiveProperties.Add(e);
				}));

				return new[] { item };
			}));

			ListContainer allPropertiesButton = createCheckboxListItem("[All]");
			allPropertiesButton.Add(new CommandBinding<Point>(allPropertiesButton.MouseLeftDown, delegate(Point p)
			{
				if (allProperties)
				{
					allProperties.Value = false;
					foreach (PropertyEntry e in analyticsActiveProperties.ToList())
						analyticsActiveProperties.Remove(e);
				}
				else
				{
					allProperties.Value = true;
					foreach (PropertyEntry e in analyticsProperties)
					{
						if (!e.Active)
							analyticsActiveProperties.Add(e);
					}
				}
			}));
			Container allPropertiesCheckbox = (Container)allPropertiesButton.GetChildByName("Checkbox");
			allPropertiesCheckbox.Add(new Binding<Microsoft.Xna.Framework.Color, bool>(allPropertiesCheckbox.Tint, x => x ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Black, allProperties));
			propertiesList.Children.Add(allPropertiesButton);

			Func<Session.EventList, LineDrawer2D> createEventLines = delegate(Session.EventList el)
			{
				LineDrawer2D line = new LineDrawer2D();
				line.Color.Value = this.colorHash(el.Name);
				line.UserData.Value = el;

				foreach (Session.Event e in el.Events)
				{
					line.Lines.Add(new LineDrawer2D.Line
					{
						A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(new Vector3(e.Time, 0.0f, 0.0f), Microsoft.Xna.Framework.Color.White),
						B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor(new Vector3(e.Time, timeline.Size.Value.Y, 0.0f), Microsoft.Xna.Framework.Color.White),
					});
				}
				return line;
			};

			analyticsActiveEvents.ItemAdded += delegate(int index, EventEntry ee)
			{
				ee.Active.Value = true;
				foreach (SessionEntry s in analyticsActiveSessions)
				{
					Session.PositionProperty positionProperty = s.Session.PositionProperties[0];
					foreach (Session.EventList el in s.Session.Events)
					{
						if (el.Name == ee.Name)
						{
							List<ModelInstance> models = new List<ModelInstance>();
							Vector4 color = this.colorHash(el.Name);
							int hash = (int)(new Color(color).PackedValue);
							foreach (Session.Event e in el.Events)
							{
								ModelInstance i = new ModelInstance();
								i.Setup("Models\\position-model", hash);
								if (i.IsFirstInstance)
									i.Model.Color.Value = new Vector3(color.X, color.Y, color.Z);
								i.Scale.Value = new Vector3(0.25f);
								i.Transform.Value = Matrix.CreateTranslation(positionProperty[e.Time]);
								models.Add(i);
								result.Add(i);
							}
							eventPositionModels[el] = models;
						}
					}

					timeline.Children.Add(s.Session.Events.Where(x => x.Name == ee.Name).Select(createEventLines));
				}
			};

			analyticsActiveEvents.ItemRemoved += delegate(int index, EventEntry e)
			{
				e.Active.Value = false;
				foreach (KeyValuePair<Session.EventList, List<ModelInstance>> pair in eventPositionModels.ToList())
				{
					if (pair.Key.Name == e.Name)
					{
						foreach (ModelInstance instance in pair.Value)
							instance.Delete.Execute();
						eventPositionModels.Remove(pair.Key);
					}
				}
				timeline.Children.Remove(timeline.Children.Where(x => x.UserData.Value != null && ((Session.EventList)x.UserData.Value).Name == e.Name).ToList());
			};

			analyticsActiveProperties.ItemAdded += delegate(int index, PropertyEntry e)
			{
				e.Active.Value = true;
			};

			analyticsActiveProperties.ItemRemoved += delegate(int index, PropertyEntry e)
			{
				e.Active.Value = false;
			};

			ListContainer propertyTimelines = new ListContainer();
			propertyTimelines.Alignment.Value = ListContainer.ListAlignment.Min;
			propertyTimelines.Orientation.Value = ListContainer.ListOrientation.Vertical;
			timelines.Children.Add(propertyTimelines);

			Action<LineDrawer2D> refreshPropertyGraph = delegate(LineDrawer2D lines)
			{
				string propertyName = ((PropertyEntry)lines.UserData.Value).Name;
				lines.Lines.Clear();
				float time = 0.0f, lastTime = 0.0f;
				float lastValue = 0.0f;
				bool firstLine = true;
				float max = float.MinValue, min = float.MaxValue;
				while (true)
				{
					bool stop = true;

					// Calculate average
					int count = 0;
					float sum = 0.0f;
					foreach (SessionEntry s in analyticsActiveSessions)
					{
						if (time < s.Session.TotalTime)
						{
							Session.ContinuousProperty prop = s.Session.GetContinuousProperty(propertyName);
							if (prop != null)
							{
								stop = false;
								sum += prop[time];
								count++;
							}
						}
					}

					if (stop)
						break;
					else
					{
						float value = sum / (float)count;
						if (firstLine)
							firstLine = false;
						else
						{
							lines.Lines.Add(new LineDrawer2D.Line
							{
								A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor
								{
									Color = Microsoft.Xna.Framework.Color.White,
									Position = new Vector3(lastTime, lastValue, 0.0f),
								},
								B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor
								{
									Color = Microsoft.Xna.Framework.Color.White,
									Position = new Vector3(time, value, 0.0f),
								},
							});
						}
						min = Math.Min(min, value);
						max = Math.Max(max, value);
						lastValue = value;
						lastTime = time;
						time += Session.Recorder.Interval;
					}

					if (min < max)
					{
						float scale = -timelineHeight / (max - min);
						lines.Scale.Value = new Vector2(1, scale);
						lines.Position.Value = new Vector2(0, max * -scale);
					}
					else
					{
						lines.AnchorPoint.Value = Vector2.Zero;
						if (min <= 0.0f)
							lines.Position.Value = new Vector2(0, timelineHeight);
						else
							lines.Position.Value = new Vector2(0, timelineHeight * 0.5f);
					}
				}
			};

			Action refreshPropertyGraphs = delegate()
			{
				foreach (LineDrawer2D line in propertyTimelines.Children.Select(x => x.Children.First()))
					refreshPropertyGraph(line);
			};

			propertyTimelines.Add(new ListBinding<UIComponent, PropertyEntry>(propertyTimelines.Children, analyticsActiveProperties, delegate(PropertyEntry e)
			{
				Container propertyTimeline = new Container();
				propertyTimeline.Add(new Binding<Vector2>(propertyTimeline.Size, timeline.Size));
				propertyTimeline.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				propertyTimeline.Opacity.Value = 0.5f;
				propertyTimeline.ResizeHorizontal.Value = false;
				propertyTimeline.ResizeVertical.Value = false;

				LineDrawer2D line = new LineDrawer2D();
				line.Color.Value = this.colorHash(e.Name);
				line.UserData.Value = e;
				propertyTimeline.Children.Add(line);

				refreshPropertyGraph(line);

				return new[] { propertyTimeline };
			}));

			analyticsActiveSessions.ItemAdded += delegate(int index, SessionEntry s)
			{
				Session.PositionProperty positionProperty = s.Session.PositionProperties[0];
				foreach (Session.EventList el in s.Session.Events)
				{
					if (analyticsActiveEvents.FirstOrDefault(x => x.Name == el.Name) != null)
					{
						List<ModelInstance> models = new List<ModelInstance>();
						Vector4 color = this.colorHash(el.Name);
						int hash = (int)(new Color(color).PackedValue);
						foreach (Session.Event e in el.Events)
						{
							ModelInstance i = new ModelInstance();
							i.Setup("Models\\position-model", hash);
							if (i.IsFirstInstance)
								i.Model.Color.Value = new Vector3(color.X, color.Y, color.Z);
							i.Scale.Value = new Vector3(0.25f);
							i.Transform.Value = Matrix.CreateTranslation(positionProperty[e.Time]);
							result.Add(i);
							models.Add(i);
						}
						eventPositionModels[el] = models;
					}
				}

				ModelInstance instance = new ModelInstance();
				instance.Setup("Models\\position-model", 0);
				instance.Scale.Value = new Vector3(0.25f);
				result.Add(instance);
				sessionPositionModels.Add(s.Session, instance);
				s.Active.Value = true;
				timeline.Children.Add(s.Session.Events.Where(x => analyticsActiveEvents.FirstOrDefault(y => y.Name == x.Name) != null).Select(createEventLines));
				playbackLocation.Reset();

				refreshPropertyGraphs();
			};

			analyticsActiveSessions.ItemRemoved += delegate(int index, SessionEntry s)
			{
				ModelInstance instance = sessionPositionModels[s.Session];
				instance.Delete.Execute();

				foreach (KeyValuePair<Session.EventList, List<ModelInstance>> pair in eventPositionModels.ToList())
				{
					if (pair.Key.Session == s.Session)
					{
						foreach (ModelInstance i in pair.Value)
							i.Delete.Execute();
						eventPositionModels.Remove(pair.Key);
					}
				}

				sessionPositionModels.Remove(s.Session);
				s.Active.Value = false;
				timeline.Children.Remove(timeline.Children.Where(x => x.UserData.Value != null && ((Session.EventList)x.UserData.Value).Session == s.Session).ToList());

				refreshPropertyGraphs();
			};

			playbackLocation.Set = delegate(float value)
			{
				if (analyticsActiveSessions.Count == 0)
					return;

				value = Math.Max(0.0f, value);
				float end = analyticsActiveSessions.Max(x => x.Session.TotalTime);
				if (value > end)
				{
					playbackLocation.InternalValue = end;
					analyticsPlaying.Value = false;
				}
				else
					playbackLocation.InternalValue = value;

				foreach (KeyValuePair<Session, ModelInstance> pair in sessionPositionModels)
					pair.Value.Transform.Value = Matrix.CreateTranslation(pair.Key.PositionProperties[0][playbackLocation]);
			};

			LineDrawer2D playbackLine = new LineDrawer2D();
			playbackLine.Color.Value = Vector4.One;
			playbackLine.Lines.Add(new LineDrawer2D.Line
			{
				A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor
				{
					Color = Microsoft.Xna.Framework.Color.White,
					Position = new Vector3(0.0f, -10.0f, 0.0f),
				},
				B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor
				{
					Color = Microsoft.Xna.Framework.Color.White,
					Position = new Vector3(0.0f, timeline.Size.Value.Y, 0.0f),
				},
			});
			playbackLine.Add(new Binding<Vector2, float>(playbackLine.Position, x => new Vector2(x, 0.0f), playbackLocation));
			timeline.Children.Add(playbackLine);

			result.Add(new NotifyBinding(delegate()
			{
				allEventsButton.Detach();
				allSessionsButton.Detach();
				allPropertiesButton.Detach();
				analyticsSessions.Clear();
				analyticsEvents.Clear();
				analyticsProperties.Clear();
				eventList.Children.Add(allEventsButton);
				sessionList.Children.Add(allSessionsButton);
				propertiesList.Children.Add(allPropertiesButton);

				foreach (ModelInstance instance in sessionPositionModels.Values)
					instance.Delete.Execute();
				sessionPositionModels.Clear();

				foreach (ModelInstance instance in eventPositionModels.Values.SelectMany(x => x))
					instance.Delete.Execute();
				eventPositionModels.Clear();

				allEvents.Value = false;
				allSessions.Value = false;
				allProperties.Value = false;
				analyticsEnable.Value = false;
				
				analyticsActiveEvents.Clear();
				analyticsActiveSessions.Clear();
				analyticsActiveProperties.Clear();

				propertyTimelines.Children.Clear();

				playbackLine.Detach();
				timeline.Children.Clear();
				timeline.Children.Add(playbackLine);

				analyticsPlaying.Value = false;
				playbackLocation.Value = 0.0f;
			}, main.MapFile));

			addCommand("Toggle analytics playback", new PCInput.Chord { Modifier = Keys.LeftAlt, Key = Keys.A }, () => analyticsEnable && !editor.MovementEnabled && analyticsActiveSessions.Count > 0, new Command
			{
				Action = delegate()
				{
					analyticsPlaying.Value = !analyticsPlaying;
				}
			});

			addCommand("Stop analytics playback", new PCInput.Chord { Key = Keys.Escape }, () => analyticsPlaying, new Command
			{
				Action = delegate()
				{
					analyticsPlaying.Value = false;
				}
			});

			Container playbackContainer = new Container();
			playbackContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			playbackContainer.Opacity.Value = 0.5f;
			sessionsSidebar.Children.Add(playbackContainer);
			playbackContainer.Add(new CommandBinding<Point, int>(playbackContainer.MouseScrolled, delegate(Point p, int delta)
			{
				playbackSpeed.Value = Math.Max(1.0f, Math.Min(10.0f, playbackSpeed.Value + delta));
			}));

			TextElement playbackLabel = new TextElement();
			playbackLabel.FontFile.Value = "Font";
			playbackLabel.Add(new Binding<string>(playbackLabel.Text, delegate()
			{
				return playbackLocation.Value.ToString("F") + " " + (analyticsPlaying ? "Playing" : "Stopped") + " " + playbackSpeed.Value.ToString("F") + "x";
			}, playbackLocation, playbackSpeed, analyticsPlaying));
			playbackContainer.Children.Add(playbackLabel);

			Container descriptionContainer = null;

			Updater timelineUpdate = new Updater
			{
				delegate(float dt)
				{
					bool setTimelinePosition = false;

					if (timelines.Highlighted || descriptionContainer != null)
					{
						if (input.LeftMouseButton)
						{
							setTimelinePosition = true;
							playbackLocation.Value = Vector3.Transform(new Vector3(input.Mouse.Value.X, 0.0f, 0.0f), Matrix.Invert(timeline.GetAbsoluteTransform())).X;
						}

						float threshold = 3.0f / timelines.Scale.Value.X;
						float mouseRelative = Vector3.Transform(new Vector3(input.Mouse, 0.0f), Matrix.Invert(timelines.GetAbsoluteTransform())).X;

						if (descriptionContainer != null)
						{
							if (!timelines.Highlighted || (float)Math.Abs(descriptionContainer.Position.Value.X - mouseRelative) > threshold)
							{
								descriptionContainer.Delete.Execute();
								descriptionContainer = null;
							}
						}

						if (descriptionContainer == null && timeline.Highlighted)
						{
							bool stop = false;
							foreach (UIComponent component in timeline.Children)
							{
								LineDrawer2D lines = component as LineDrawer2D;

								if (lines == null)
									continue;

								foreach (LineDrawer2D.Line line in lines.Lines)
								{
									Session.EventList el = lines.UserData.Value as Session.EventList;
									if (el != null && (float)Math.Abs(line.A.Position.X - mouseRelative) < threshold)
									{
										descriptionContainer = new Container();
										descriptionContainer.AnchorPoint.Value = new Vector2(0.5f, 1.0f);
										descriptionContainer.Position.Value = new Vector2(line.A.Position.X, 0.0f);
										descriptionContainer.Opacity.Value = 1.0f;
										descriptionContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
										descriptionContainer.Add(new Binding<Vector2>(descriptionContainer.Scale, x => new Vector2(1.0f / x.X, 1.0f / x.Y), timelines.Scale));
										timeline.Children.Add(descriptionContainer);
										TextElement description = new TextElement();
										description.WrapWidth.Value = 256;
										description.Text.Value = el.Name;
										description.FontFile.Value = "Font";
										descriptionContainer.Children.Add(description);
										stop = true;
										break;
									}
								}
								if (stop)
									break;
							}
						}
					}

					if (analyticsPlaying && !setTimelinePosition)
					{
						if (analyticsActiveSessions.Count == 0)
							analyticsPlaying.Value = false;
						else
							playbackLocation.Value += dt * playbackSpeed;
					}
				}
			};
			timelineUpdate.EnabledInEditMode.Value = true;
			result.Add(timelineUpdate);

			// Save
			addCommand("Save", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.S }, () => !editor.MovementEnabled, editor.Save);

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
				int foundIndex = WorldFactory.StateList.FindIndex(x => x.Name == editor.Brush);
				if (foundIndex != -1)
					brush = foundIndex;
				int stateCount = WorldFactory.States.Count + 1;
				brush = 1 + ((brush - 1 + delta) % (stateCount - 1));
				if (brush < 1)
					brush = stateCount + ((brush - 1) % stateCount);
				if (brush == stateCount - 1)
					editor.Brush.Value = "[Procedural]";
				else
					editor.Brush.Value = WorldFactory.StateList[brush].Name;
			};
			result.Add(new CommandBinding(input.GetKeyDown(Keys.Q), () => editor.MapEditMode, delegate()
			{
				changeBrush(-1);
			}));
			result.Add(new CommandBinding(input.GetKeyDown(Keys.E), () => editor.MapEditMode && !input.GetKey(Keys.LeftShift), delegate()
			{
				changeBrush(1);
			}));
			result.Add(new CommandBinding<int>(input.MouseScrolled, () => editor.MapEditMode && !input.GetKey(Keys.LeftAlt), delegate(int delta)
			{
				editor.BrushSize.Value = Math.Max(1, editor.BrushSize.Value + delta);
			}));

			addCommand("Propagate current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.E }, () => editor.MapEditMode, editor.PropagateMaterial);
			addCommand("Propagate current material to selected box", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.R }, () => editor.MapEditMode, editor.PropagateMaterialBox);
			addCommand("Sample current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Q }, () => editor.MapEditMode, editor.SampleMaterial);
			addCommand("Delete current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.X }, () => editor.MapEditMode, editor.DeleteMaterial);

			editor.Add(new Binding<Vector2>(editor.Mouse, input.Mouse, () => !input.EnableLook));

			uiRenderer.Add(new CommandBinding(uiRenderer.SwallowMouseEvents, (Action)input.SwallowEvents));

			Camera camera = main.Camera;

			Property<float> cameraDistance = new Property<float> { Value = 10.0f };
			scroller.Add(new Binding<bool>(scroller.EnableScroll, x => !x, input.GetKey(Keys.LeftAlt)));
			input.Add(new CommandBinding<int>(input.MouseScrolled, () => input.GetKey(Keys.LeftAlt), delegate(int delta)
			{
				if (timelineScroller.Highlighted && !editor.MapEditMode)
				{
					float newScale = Math.Max(timelines.Scale.Value.X + delta * 6.0f, timelineScroller.Size.Value.X / timelines.Size.Value.X);
					Matrix absoluteTransform = timelines.GetAbsoluteTransform();
					float x = input.Mouse.Value.X + ((absoluteTransform.Translation.X - input.Mouse.Value.X) * (newScale / timelines.Scale.Value.X));
					timelines.Position.Value = new Vector2(x, 0.0f);
					timelines.Scale.Value = new Vector2(newScale, 1.0f);
				}
				else
					cameraDistance.Value = Math.Max(5, cameraDistance.Value + delta * -2.0f);
			}));
			input.Add(new Binding<bool>(input.EnableLook, () => editor.MapEditMode || (input.MiddleMouseButton && editor.TransformMode.Value == Editor.TransformModes.None), input.MiddleMouseButton, editor.MapEditMode, editor.TransformMode));
			input.Add(new Binding<Vector3, Vector2>(camera.Angles, x => new Vector3(-x.Y, x.X, 0.0f), input.Mouse, () => input.EnableLook));
			input.Add(new Binding<bool>(main.IsMouseVisible, x => !x, input.EnableLook));
			editor.Add(new Binding<Vector3>(camera.Position, () => editor.Position.Value - (camera.Forward.Value * cameraDistance), editor.Position, input.Mouse, cameraDistance));

			PointLight editorLight = result.GetOrCreate<PointLight>("EditorLight");
			editorLight.Serialize = false;
			editorLight.Editable = false;
			editorLight.Shadowed.Value = false;
			editorLight.Add(new Binding<float>(editorLight.Attenuation, main.Camera.FarPlaneDistance));
			editorLight.Color.Value = new Vector3(1.5f, 1.5f, 1.5f);
			editorLight.Add(new Binding<Vector3>(editorLight.Position, main.Camera.Position));
			editorLight.Enabled.Value = false;

			ui.PopupCommands.Add(new EditorUI.PopupCommand
			{
				Description = "Toggle editor light",
				Enabled = () => editor.SelectedEntities.Count == 0 && !editor.MapEditMode,
				Action = new Command { Action = () => editorLight.Enabled.Value = !editorLight.Enabled },
			});

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
				if (editor.TransformMode.Value != Editor.TransformModes.None || ui.StringPropertyLocked)
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
					model.Scale.Value = new Vector3(editor.SelectedEntities[0].Get<Map>().Scale * 0.5f);
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
				"Voxel duplicate",
				new PCInput.Chord { Key = Keys.C },
				() => editor.MapEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelDuplicate
			);
			addCommand
			(
				"Voxel yank",
				new PCInput.Chord { Key = Keys.Y },
				() => editor.MapEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelCopy
			);
			addCommand
			(
				"Voxel paste",
				new PCInput.Chord { Key = Keys.P },
				() => editor.MapEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelPaste
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
