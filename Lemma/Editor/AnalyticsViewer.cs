using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Lemma.Factories
{
	public class AnalyticsViewer
	{
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

		private static Vector4 colorHash(string eventName)
		{
			byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(ASCIIEncoding.UTF8.GetBytes(eventName));
			Vector3 color = new Vector3(hash[0] / 255.0f, hash[1] / 255.0f, hash[2] / 255.0f);
			color.Normalize();
			return new Vector4(color, 1.0f);
		}

		public static void Bind(Entity entity, Main main, ListContainer commandQueueContainer)
		{
			PCInput input = entity.Get<PCInput>();
			Editor editor = entity.Get<Editor>();
			EditorGeeUI gui = entity.Get<EditorGeeUI>();
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
			entity.Add(new CommandBinding(entity.Delete, timelineScroller.Delete));
			main.UI.Root.Children.Add(timelineScroller);

			timelineScroller.Add(new Binding<bool>(editor.EnableCameraDistanceScroll, () => !timelineScroller.Highlighted || editor.VoxelEditMode, timelineScroller.Highlighted, editor.VoxelEditMode));
			timelineScroller.Add(new CommandBinding(timelineScroller.Delete, delegate()
			{
				editor.EnableCameraDistanceScroll.Value = true;
			}));

			ListContainer timelines = new ListContainer();
			timelines.Alignment.Value = ListContainer.ListAlignment.Min;
			timelines.Orientation.Value = ListContainer.ListOrientation.Vertical;
			timelines.Reversed.Value = true;
			timelineScroller.Children.Add(timelines);

			input.Add(new CommandBinding<int>(input.MouseScrolled, () => input.GetKey(Keys.LeftAlt) && timelineScroller.Highlighted && !editor.VoxelEditMode, delegate(int delta)
			{
				float newScale = Math.Max(timelines.Scale.Value.X + delta * 6.0f, timelineScroller.Size.Value.X / timelines.Size.Value.X);
				Matrix absoluteTransform = timelines.GetAbsoluteTransform();
				float x = input.Mouse.Value.X + ((absoluteTransform.Translation.X - input.Mouse.Value.X) * (newScale / timelines.Scale.Value.X));
				timelines.Position.Value = new Vector2(x, 0.0f);
				timelines.Scale.Value = new Vector2(newScale, 1.0f);
			}));

			Container timeline = new Container();
			timeline.Size.Value = new Vector2(0, timelineHeight);
			timeline.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			timeline.ResizeHorizontal.Value = false;
			timeline.ResizeVertical.Value = false;
			timelines.Children.Add(timeline);

			EditorFactory.AddCommand
			(
				entity, main, commandQueueContainer, "Load analytics data", new PCInput.Chord(),
				new Command
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
				gui.MapCommands,
				() => !analyticsEnable && !string.IsNullOrEmpty(main.MapFile),
				analyticsEnable, main.MapFile
			);

			ListContainer sessionsSidebar = new ListContainer();
			sessionsSidebar.AnchorPoint.Value = new Vector2(1, 1);
			sessionsSidebar.Add(new Binding<Vector2>(sessionsSidebar.Position, () => new Vector2(main.ScreenSize.Value.X - 10, main.ScreenSize.Value.Y - timelineScroller.ScaledSize.Value.Y - 10), main.ScreenSize, timelineScroller.ScaledSize));
			sessionsSidebar.Add(new Binding<bool>(sessionsSidebar.Visible, analyticsEnable));
			sessionsSidebar.Alignment.Value = ListContainer.ListAlignment.Max;
			sessionsSidebar.Reversed.Value = true;
			main.UI.Root.Children.Add(sessionsSidebar);
			entity.Add(new CommandBinding(entity.Delete, sessionsSidebar.Delete));

			Func<string, ListContainer> createCheckboxListItem = delegate(string text)
			{
				ListContainer layout = new ListContainer();
				layout.Orientation.Value = ListContainer.ListOrientation.Horizontal;

				TextElement label = new TextElement();
				label.FontFile.Value = main.MainFont;
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
				label.Tint.Value = new Microsoft.Xna.Framework.Color(colorHash(e.Name));

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
				label.Tint.Value = new Microsoft.Xna.Framework.Color(colorHash(e.Name));

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
				line.Color.Value = colorHash(el.Name);
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
							Vector4 color = colorHash(el.Name);
							int hash = (int)(new Color(color).PackedValue);
							foreach (Session.Event e in el.Events)
							{
								ModelInstance i = new ModelInstance();
								i.Serialize = false;
								i.Setup("InstancedModels\\position-model", hash);
								if (i.IsFirstInstance)
									i.Model.Color.Value = new Vector3(color.X, color.Y, color.Z);
								i.Scale.Value = new Vector3(0.25f);
								i.Transform.Value = Matrix.CreateTranslation(positionProperty.GetLastRecordedPosition(e.Time));
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
				timeline.Children.RemoveAll(timeline.Children.Where(x => x.UserData.Value != null && ((Session.EventList)x.UserData.Value).Name == e.Name).ToList());
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

			Action<Container> refreshPropertyGraph = delegate(Container container)
			{
				TextElement label = (TextElement)container.GetChildByName("Label");
				LineDrawer2D lines = (LineDrawer2D)container.GetChildByName("Graph");
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
					label.Text.Value = max.ToString("F");
				}
			};

			Action refreshPropertyGraphs = delegate()
			{
				foreach (Container propertyTimeline in propertyTimelines.Children)
					refreshPropertyGraph(propertyTimeline);
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
				line.Name.Value = "Graph";
				line.Color.Value = colorHash(e.Name);
				line.UserData.Value = e;
				propertyTimeline.Children.Add(line);

				TextElement label = new TextElement();
				label.FontFile.Value = main.MainFont;
				label.Name.Value = "Label";
				label.Add(new Binding<Vector2>(label.Scale, x => new Vector2(1.0f / x.X, 1.0f / x.Y), timelines.Scale));
				label.AnchorPoint.Value = new Vector2(0, 0);
				label.Position.Value = new Vector2(0, 0);
				propertyTimeline.Children.Add(label);

				refreshPropertyGraph(propertyTimeline);

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
						Vector4 color = colorHash(el.Name);
						int hash = (int)(new Color(color).PackedValue);
						foreach (Session.Event e in el.Events)
						{
							ModelInstance i = new ModelInstance();
							i.Serialize = false;
							i.Setup("InstancedModels\\position-model", hash);
							if (i.IsFirstInstance)
								i.Model.Color.Value = new Vector3(color.X, color.Y, color.Z);
							i.Scale.Value = new Vector3(0.25f);
							i.Transform.Value = Matrix.CreateTranslation(positionProperty.GetLastRecordedPosition(e.Time));
							entity.Add(i);
							models.Add(i);
						}
						eventPositionModels[el] = models;
					}
				}

				ModelInstance instance = new ModelInstance();
				instance.Setup("InstancedModels\\position-model", 0);
				instance.Serialize = false;
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
				timeline.Children.RemoveAll(timeline.Children.Where(x => x.UserData.Value != null && ((Session.EventList)x.UserData.Value).Session == s.Session).ToList());

				refreshPropertyGraphs();
			};

			entity.Add(new SetBinding<float>(playbackLocation, delegate(float value)
			{
				if (analyticsActiveSessions.Length == 0)
					return;

				if (value < 0.0f)
					playbackLocation.Value = 0.0f;
				float end = analyticsActiveSessions.Max(x => x.Session.TotalTime);
				if (value > end)
				{
					playbackLocation.Value = end;
					analyticsPlaying.Value = false;
				}

				foreach (KeyValuePair<Session, ModelInstance> pair in sessionPositionModels)
					pair.Value.Transform.Value = Matrix.CreateTranslation(pair.Key.PositionProperties[0][playbackLocation]);
			}));

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

			EditorFactory.AddCommand
			(
				entity, main, commandQueueContainer, "Toggle analytics playback", new PCInput.Chord { Modifier = Keys.LeftAlt, Key = Keys.A },  new Command
				{
					Action = delegate()
					{
						analyticsPlaying.Value = !analyticsPlaying;
					}
				},
				gui.MapCommands,
				() => analyticsEnable && !editor.MovementEnabled && analyticsActiveSessions.Length > 0,
				analyticsEnable, editor.MovementEnabled, analyticsActiveSessions.Length
			);

			EditorFactory.AddCommand
			(
				entity, main, commandQueueContainer, "Stop analytics playback", new PCInput.Chord { Key = Keys.Escape }, new Command
				{
					Action = delegate()
					{
						analyticsPlaying.Value = false;
					}
				}, gui.MapCommands, () => analyticsPlaying, analyticsPlaying
			);

			Container playbackContainer = new Container();
			playbackContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			playbackContainer.Opacity.Value = 0.5f;
			sessionsSidebar.Children.Add(playbackContainer);
			playbackContainer.Add(new CommandBinding<int>(playbackContainer.MouseScrolled, delegate(int delta)
			{
				playbackSpeed.Value = Math.Max(1.0f, Math.Min(10.0f, playbackSpeed.Value + delta));
			}));

			TextElement playbackLabel = new TextElement();
			playbackLabel.FontFile.Value = main.MainFont;
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
							foreach (UIComponent component in timeline.Children)
							{
								LineDrawer2D lines = component as LineDrawer2D;

								if (lines == null)
									continue;

								Session.EventList el = lines.UserData.Value as Session.EventList;
								if (el != null)
								{
									bool stop = false;
									foreach (Session.Event e in el.Events)
									{
										if (el != null && (float)Math.Abs(e.Time - mouseRelative) < threshold)
										{
											descriptionContainer = new Container();
											descriptionContainer.AnchorPoint.Value = new Vector2(0.5f, 1.0f);
											descriptionContainer.Position.Value = new Vector2(e.Time, 0.0f);
											descriptionContainer.Opacity.Value = 1.0f;
											descriptionContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
											descriptionContainer.Add(new Binding<Vector2>(descriptionContainer.Scale, x => new Vector2(1.0f / x.X, 1.0f / x.Y), timelines.Scale));
											timeline.Children.Add(descriptionContainer);
											TextElement description = new TextElement();
											description.WrapWidth.Value = 256;

											if (string.IsNullOrEmpty(e.Data))
												description.Text.Value = el.Name;
											else
												description.Text.Value = string.Format("{0}\n{1}", el.Name, e.Data);

											description.FontFile.Value = main.MainFont;
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
					}

					if (analyticsPlaying && !setTimelinePosition)
					{
						if (analyticsActiveSessions.Length == 0)
							analyticsPlaying.Value = false;
						else
							playbackLocation.Value += dt * playbackSpeed;
					}
				}
			};
			timelineUpdate.EnabledInEditMode = true;
			entity.Add(timelineUpdate);

		}
	}
}
