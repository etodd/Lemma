using System;
using System.Windows.Forms;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using BEPUphysics;
using System.Xml.Serialization;
using System.IO;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace Lemma.Factories
{
	public class EditorFactory : Factory<Main>
	{
		public EditorFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
			this.EditorCanSpawn = false;
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
			Entity entity = new Entity(main, "Editor");
			entity.Serialize = false;
			return entity;
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

			Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(rayStart, ray, closestEntityDistance, true);
			if (hit.Coordinate != null)
			{
				closestEntity = hit.Voxel.Entity;
				closestTransform = null;
			}
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			this.SetMain(entity, main);
			entity.ID.Editable = false;

			Editor editor = new Editor();
			EditorGeeUI gui = new EditorGeeUI() { Editable = false };
			Model model = new Model { Editable = false };
			model.Filename.Value = "Models\\selector";
			model.Scale.Value = new Vector3(0.5f);

			UIRenderer uiRenderer = new UIRenderer { Editable = false };
			FPSInput input = new FPSInput { Editable = false };
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
			radiusVisual.Editable = false;
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
			selection.Editable = false;
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

			Action<string, PCInput.Chord, Func<bool>, Command> addCommand = delegate(string description, PCInput.Chord chord, Func<bool> enabled, Command action)
			{
				gui.PopupCommands.Add(new EditorGeeUI.PopupCommand { Description = description, Chord = chord, Enabled = enabled, Action = action });

				if (chord.Modifier != Keys.None)
					input.Add(new CommandBinding(input.GetChord(chord), enabled, action));
				else
					input.Add(new CommandBinding(input.GetKeyDown(chord.Key), enabled, action));

				gui.Add(new CommandBinding(action, delegate()
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
				Factory factory = Factory.Get(entityType);
				if (factory.EditorCanSpawn)
				{
					gui.PopupCommands.Add(new EditorGeeUI.PopupCommand
					{
						Description = "Add " + entityType,
						Enabled = () => editor.SelectedEntities.Count == 0 && !editor.VoxelEditMode,
						Action = new Command { Action = () => editor.Spawn.Execute(entityType) },
					});
				}
			}

			model.Add(new Binding<bool>(model.Enabled, editor.VoxelEditMode));
			model.Add(new Binding<Matrix>(model.Transform, () => editor.Orientation.Value * Matrix.CreateTranslation(editor.Position), editor.Position, editor.Orientation));

			editor.Add(new TwoWayBinding<string>(main.MapFile, editor.MapFile));

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

			addCommand("Delete", new PCInput.Chord { Key = Keys.X }, () => !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None && editor.SelectedEntities.Count > 0, editor.DeleteSelected);
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

			addCommand("Join voxels", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.J }, () => !editor.VoxelEditMode && editor.SelectedEntities.Count == 2 && editor.SelectedEntities[0].Get<Voxel>() != null && editor.SelectedEntities[1].Get<Voxel>() != null, new Command
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
			});

			entity.Add(new CommandBinding(main.MapLoaded, delegate()
			{
				editor.Position.Value = Vector3.Zero;
				editor.NeedsSave.Value = false;
			}));

			addCommand("Quit", new PCInput.Chord { Modifier = Keys.LeftControl, Key = Keys.Q }, () => !editor.MovementEnabled, new Command
			{
				Action = delegate()
				{
					throw new Main.ExitException();
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

			gui.PopupCommands.Add(new EditorGeeUI.PopupCommand
			{
				Description = "Load analytics data",
				Enabled = () => editor.SelectedEntities.Count == 0 && !editor.VoxelEditMode && !analyticsEnable,
				Action = new Command
				{
					Action = delegate()
					{
						if (main.MapFile.Value != null)
						{
							List<Session> sessions = main.LoadAnalytics(main.MapFile);
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
								analyticsEvents.AddAll(distinctEventNames.Keys.Select(x => new EventEntry { Name = x }));
								analyticsProperties.AddAll(distinctPropertyNames.Keys.Select(x => new PropertyEntry { Name = x }));
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

				item.Add(new CommandBinding(item.MouseLeftDown, delegate()
				{
					if (entry.Active)
					{
						allSessions.Value = false;
						analyticsActiveSessions.Remove(entry);
					}
					else
						analyticsActiveSessions.Add(entry);
				}));

				return item;
			}));

			ListContainer allSessionsButton = createCheckboxListItem("[All]");
			allSessionsButton.Add(new CommandBinding(allSessionsButton.MouseLeftDown, delegate()
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

				item.Add(new CommandBinding(item.MouseLeftDown, delegate()
				{
					if (e.Active)
					{
						allEvents.Value = false;
						analyticsActiveEvents.Remove(e);
					}
					else
						analyticsActiveEvents.Add(e);
				}));

				return item;
			}));

			ListContainer allEventsButton = createCheckboxListItem("[All]");
			allEventsButton.Add(new CommandBinding(allEventsButton.MouseLeftDown, delegate()
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

				item.Add(new CommandBinding(item.MouseLeftDown, delegate()
				{
					if (e.Active)
					{
						allProperties.Value = false;
						analyticsActiveProperties.Remove(e);
					}
					else
						analyticsActiveProperties.Add(e);
				}));

				return item;
			}));

			ListContainer allPropertiesButton = createCheckboxListItem("[All]");
			allPropertiesButton.Add(new CommandBinding(allPropertiesButton.MouseLeftDown, delegate()
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
								entity.Add(i);
							}
							eventPositionModels[el] = models;
						}
					}

					timeline.Children.AddAll(s.Session.Events.Where(x => x.Name == ee.Name).Select(createEventLines));
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

				return propertyTimeline;
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
							entity.Add(i);
							models.Add(i);
						}
						eventPositionModels[el] = models;
					}
				}

				ModelInstance instance = new ModelInstance();
				instance.Setup("Models\\position-model", 0);
				instance.Scale.Value = new Vector3(0.25f);
				entity.Add(instance);
				sessionPositionModels.Add(s.Session, instance);
				s.Active.Value = true;
				timeline.Children.AddAll(s.Session.Events.Where(x => analyticsActiveEvents.FirstOrDefault(y => y.Name == x.Name) != null).Select(createEventLines));
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

			entity.Add(new NotifyBinding(delegate()
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
			playbackContainer.Add(new CommandBinding<int>(playbackContainer.MouseScrolled, delegate(int delta)
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
			timelineUpdate.EnabledInEditMode = true;
			entity.Add(timelineUpdate);

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

			addCommand("Propagate current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.E }, () => editor.VoxelEditMode, editor.PropagateMaterial);
			addCommand("Intersect current material with selection", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.I }, () => editor.VoxelEditMode, editor.IntersectMaterial);
			addCommand("Propagate current material to selected box", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.R }, () => editor.VoxelEditMode, editor.PropagateMaterialBox);
			addCommand("Propagate current material (non-contiguous)", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.T }, () => editor.VoxelEditMode, editor.PropagateMaterialAll);
			addCommand("Sample current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Q }, () => editor.VoxelEditMode, editor.SampleMaterial);
			addCommand("Delete current material", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.X }, () => editor.VoxelEditMode, editor.DeleteMaterial);
			addCommand("Delete current material (non-contiguous)", new PCInput.Chord { Modifier = Keys.LeftShift, Key = Keys.Z }, () => editor.VoxelEditMode, editor.DeleteMaterialAll);

			editor.Add(new Binding<Vector2>(editor.Mouse, input.Mouse, () => !input.EnableLook));

			uiRenderer.Add(new CommandBinding(uiRenderer.SwallowMouseEvents, (Action)input.SwallowEvents));

			Camera camera = main.Camera;

			input.Add(new CommandBinding<int>(input.MouseScrolled, () => input.GetKey(Keys.LeftAlt), delegate(int delta)
			{
				if (timelineScroller.Highlighted && !editor.VoxelEditMode)
				{
					float newScale = Math.Max(timelines.Scale.Value.X + delta * 6.0f, timelineScroller.Size.Value.X / timelines.Size.Value.X);
					Matrix absoluteTransform = timelines.GetAbsoluteTransform();
					float x = input.Mouse.Value.X + ((absoluteTransform.Translation.X - input.Mouse.Value.X) * (newScale / timelines.Scale.Value.X));
					timelines.Position.Value = new Vector2(x, 0.0f);
					timelines.Scale.Value = new Vector2(newScale, 1.0f);
				}
				else
					editor.CameraDistance.Value = Math.Max(1, editor.CameraDistance.Value + delta * -2.0f);
			}));
			input.Add(new Binding<bool>(input.EnableLook, () => editor.VoxelEditMode || (movementEnabled && editor.TransformMode.Value == Editor.TransformModes.None), movementEnabled, editor.VoxelEditMode, editor.TransformMode));
			input.Add(new Binding<Vector3, Vector2>(camera.Angles, x => new Vector3(-x.Y, x.X, 0.0f), input.Mouse, () => input.EnableLook));
			input.Add(new Binding<bool>(main.IsMouseVisible, x => !x, input.EnableLook));
			editor.Add(new Binding<Vector3>(camera.Position, () => editor.Position.Value - (camera.Forward.Value * editor.CameraDistance), editor.Position, camera.Forward, editor.CameraDistance));

			PointLight editorLight = entity.GetOrCreate<PointLight>("EditorLight");
			editorLight.Serialize = false;
			editorLight.Editable = false;
			editorLight.Add(new Binding<float>(editorLight.Attenuation, main.Camera.FarPlaneDistance));
			editorLight.Color.Value = new Vector3(1.5f, 1.5f, 1.5f);
			editorLight.Add(new Binding<Vector3>(editorLight.Position, main.Camera.Position));
			editorLight.Enabled.Value = false;

			gui.PopupCommands.Add(new EditorGeeUI.PopupCommand
			{
				Description = "Toggle editor light",
				Enabled = () => editor.SelectedEntities.Count == 0 && !editor.VoxelEditMode,
				Action = new Command { Action = () => editorLight.Enabled.Value = !editorLight.Enabled },
			});

			editor.Add(new CommandBinding(input.RightMouseButtonDown, () => !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None, delegate()
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
						Command<Entity> toggleConnection = selectedEntity.GetCommand<Entity>("ToggleEntityConnected");
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
				else if (editor.VoxelEditMode)
					editor.VoxelEditMode.Value = false;
			}));

			addCommand("Toggle voxel edit", new PCInput.Chord { Key = Keys.Tab }, delegate()
			{
				if (editor.TransformMode.Value != Editor.TransformModes.None)
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
			});

			addCommand
			(
				"Grab (move)",
				new PCInput.Chord { Key = Keys.G },
				() => editor.SelectedEntities.Count > 0 && !input.EnableLook && !editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartTranslation
			);
			addCommand
			(
				"Grab (move)",
				new PCInput.Chord { Key = Keys.G },
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartVoxelTranslation
			);
			addCommand
			(
				"Voxel duplicate",
				new PCInput.Chord { Key = Keys.C },
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelDuplicate
			);
			addCommand
			(
				"Voxel yank",
				new PCInput.Chord { Key = Keys.Y },
				() => editor.VoxelEditMode && editor.VoxelSelectionActive && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelCopy
			);
			addCommand
			(
				"Voxel paste",
				new PCInput.Chord { Key = Keys.P },
				() => editor.VoxelEditMode && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.VoxelPaste
			);
			addCommand
			(
				"Rotate",
				new PCInput.Chord { Key = Keys.R },
				() => editor.SelectedEntities.Count > 0 && !editor.VoxelEditMode && !input.EnableLook && editor.TransformMode.Value == Editor.TransformModes.None,
				editor.StartRotation
			);
			addCommand
			(
				"Lock X axis",
				new PCInput.Chord { Key = Keys.X },
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.X }
			);
			addCommand
			(
				"Lock Y axis",
				new PCInput.Chord { Key = Keys.Y },
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Y }
			);
			addCommand
			(
				"Lock Z axis",
				new PCInput.Chord { Key = Keys.Z },
				() => !editor.VoxelEditMode && editor.TransformMode.Value != Editor.TransformModes.None,
				new Command { Action = () => editor.TransformAxis.Value = Editor.TransformAxes.Z }
			);

			addCommand
			(
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
				}
			);

			addCommand
			(
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
				}
			);

			MemoryStream yankBuffer = null;
			addCommand
			(
				"Yank",
				new PCInput.Chord { Key = Keys.Y },
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
				}
			);

			addCommand
			(
				"Paste",
				new PCInput.Chord { Key = Keys.P },
				() => !editor.VoxelEditMode && !input.EnableLook && yankBuffer != null,
				new Command
				{
					Action = delegate()
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
			//main.MapFile.Value = "monolith";
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			// Cancel editor components
		}
	}
}
