using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Lemma.Util;
using System.Xml.Serialization;
using Lemma.Factories;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace Lemma.Components
{
	public class VoiceActor : Component, IUpdateableComponent, IEditorUIComponent
	{
		private AnimatedModel model;

		public enum Shape
		{
			E,
			C,
			M,
			A,
			F,
			O,
			Q,
			L,
		}

		public class Trigger
		{
			public Shape Shape;
			public float Time;
		}

		public class Clip
		{
			public Property<float> Duration = new Property<float>();
			public Property<string> Name = new Property<string>();
			public Property<string> Sound = new Property<string>();
			public ListProperty<Trigger> Triggers = new ListProperty<Trigger>();
		}

		public ListProperty<Clip> Clips = new ListProperty<Clip>();

		public VoiceActor()
		{
			this.EnabledInEditMode.Value = false;
			this.EnabledWhenPaused.Value = false;
			this.Enabled.Editable = true;
		}

		private float time = 0.0f;

		public void Update(float elapsedTime)
		{
			if (this.model == null)
				this.model = this.Entity.Get<AnimatedModel>();

			this.time += elapsedTime;
		}

		void IEditorUIComponent.AddEditorElements(UIComponent propertyList, EditorUI ui)
		{
			const float timelineHeight = 40.0f;

			ListContainer container = new ListContainer();
			propertyList.Children.Add(container);

			FPSInput input = ui.Entity.Get<FPSInput>();

			UIComponent uiRoot = ui.Entity.Get<UIRenderer>().Root;

			Container popupContainer = new Container();
			popupContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			popupContainer.Opacity.Value = 0.5f;
			popupContainer.Visible.Value = false;
			uiRoot.Children.Add(popupContainer);

			Clip selectedClip = null;
			Trigger selectedTrigger = null;
			float selectedTime = 0.0f;

			Action hidePopup = delegate()
			{
				popupContainer.Visible.Value = false;
				popupContainer.SwallowMouseEvents.Value = false;
				popupContainer.MouseLocked.Value = false;
				selectedClip = null;
			};

			Action<Shape> popupClicked = delegate(Shape s)
			{
				if (selectedTrigger != null)
				{
					selectedTrigger.Shape = s;
					selectedClip.Triggers.Changed(selectedTrigger);
				}
				else if (selectedClip != null)
					selectedClip.Triggers.Add(new Trigger { Shape = s, Time = selectedTime });
				hidePopup();
			};

			popupContainer.Add(new CommandBinding<Point>(popupContainer.MouseLeftUp, delegate(Point p)
			{
				hidePopup();
			}));

			popupContainer.Add(new CommandBinding<Point>(popupContainer.MouseRightDown, delegate(Point p)
			{
				hidePopup();
			}));

			ListContainer popup = new ListContainer();

			UIComponent deleteTriggerButton = ui.BuildButton(new Command
			{
				Action = delegate()
				{
					if (selectedTrigger != null)
					{
						selectedClip.Triggers.Remove(selectedTrigger);
						selectedTrigger = null;
					}
					hidePopup();
				}
			}, "[X]");

			popup.Children.Add(deleteTriggerButton);

			Color[] shapeColors = new[] { Color.Tan, Color.Green, Color.Blue, Color.Orange, Color.Teal, Color.Purple, Color.White, Color.Yellow };
			foreach (Shape shape in new[] { Shape.E, Shape.C, Shape.M, Shape.A, Shape.F, Shape.O, Shape.Q, Shape.L, })
			{
				Shape s = shape;
				popup.Children.Add(ui.BuildButton(new Command { Action = delegate() { popupClicked(s); } }, s.ToString(), shapeColors[(int)s]));
			}

			popupContainer.Children.Add(popup);

			popupContainer.Add(new CommandBinding(container.Delete, popupContainer.Delete));

			container.Add(new ListBinding<UIComponent, Clip>(container.Children, this.Clips, delegate(Clip clip)
			{
				UIComponent name = ui.BuildValueField(clip.Name);
				UIComponent soundFile = ui.BuildValueField(clip.Sound);

				Scroller timelineScroller = new Scroller();
				timelineScroller.ScrollAmount.Value = 60.0f;
				timelineScroller.DefaultScrollHorizontal.Value = true;
				timelineScroller.AnchorPoint.Value = new Vector2(0, 1);
				timelineScroller.SwallowMouseEvents.Value = true;
				timelineScroller.Add(new Binding<Vector2, Point>(timelineScroller.Size, x => new Vector2(x.X * 0.25f, timelineHeight), main.ScreenSize));
				timelineScroller.Add(new Binding<bool>(timelineScroller.EnableScroll, x => !x, input.GetKey(Keys.LeftAlt)));

				Container timeline = new Container();
				timeline.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				timeline.Opacity.Value = 0.5f;
				timeline.Size.Value = new Vector2(100.0f, timelineHeight);
				timeline.ResizeHorizontal.Value = false;
				timeline.ResizeVertical.Value = false;
				timelineScroller.Children.Add(timeline);

				float currentTime = 0.0f;

				timeline.Add(new CommandBinding<Point>(timeline.MouseRightDown, delegate(Point p)
				{
					popupContainer.Visible.Value = true;
					popupContainer.SwallowMouseEvents.Value = true;
					popupContainer.MouseLocked.Value = true;
					popupContainer.Position.Value = new Vector2(p.X, p.Y);
					selectedClip = clip;
					selectedTime = currentTime;
					deleteTriggerButton.Visible.Value = selectedTrigger != null;
				}));

				timeline.Add(new Binding<float, string>(clip.Duration, delegate(string sound)
				{
					if (string.IsNullOrEmpty(sound))
						return 1.0f;
					else
						return (float)this.main.Content.Load<SoundEffect>(sound).Duration.TotalSeconds;
				}, clip.Sound));

				timeline.Add(new Binding<Vector2, float>(timeline.Size, x => new Vector2(x, timelineHeight), clip.Duration));

				timeline.Add(new Binding<Vector2, float>(timeline.Scale, x => new Vector2(timelineScroller.Size.Value.X / x, 1.0f), clip.Duration));

				LineDrawer2D lines = new LineDrawer2D();
				lines.Color.Value = Vector4.One;
				lines.Add(new ListBinding<LineDrawer2D.Line, Trigger>(lines.Lines, clip.Triggers, delegate(Trigger t)
				{
					return new[]
					{
						new LineDrawer2D.Line
						{
							A = new Microsoft.Xna.Framework.Graphics.VertexPositionColor
							{
								Color = shapeColors[(int)t.Shape],
								Position = new Vector3(t.Time, 0.0f, 0.0f),
							},
							B = new Microsoft.Xna.Framework.Graphics.VertexPositionColor
							{
								Color = shapeColors[(int)t.Shape],
								Position = new Vector3(t.Time, timeline.Size.Value.Y, 0.0f),
							},
						},
					};
				}));
				timeline.Children.Add(lines);

				Container descriptionContainer = new Container();
				descriptionContainer.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
				descriptionContainer.Opacity.Value = 1.0f;
				descriptionContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
				descriptionContainer.Visible.Value = false;
				descriptionContainer.Add(new Binding<Vector2>(descriptionContainer.Scale, x => new Vector2(1.0f / x.X, 1.0f / x.Y), timeline.Scale));
				timeline.Children.Add(descriptionContainer);
				TextElement description = new TextElement();
				description.WrapWidth.Value = 256;
				description.FontFile.Value = "Font";
				descriptionContainer.Children.Add(description);

				Updater update = new Updater
				{
					delegate(float dt)
					{
						if (timeline.Highlighted && !popupContainer.Visible)
						{
							currentTime = Vector3.Transform(new Vector3(input.Mouse.Value.X, 0.0f, 0.0f), Matrix.Invert(timeline.GetAbsoluteTransform())).X;
							currentTime = Math.Min(Math.Max(0.0f, currentTime), clip.Duration);

							if (input.LeftMouseButton)
							{
								if (selectedTrigger != null)
								{
									selectedTrigger.Time = currentTime;
									clip.Triggers.Changed(selectedTrigger);
									descriptionContainer.Position.Value = new Vector2(currentTime, timelineHeight * 0.5f);
								}
							}
							else
							{
								selectedTrigger = null;
								descriptionContainer.Visible.Value = false;

								float threshold = 6.0f / timeline.Scale.Value.X;
							
								foreach (Trigger t in clip.Triggers)
								{
									if (Math.Abs(t.Time - currentTime) < threshold)
									{
										descriptionContainer.Visible.Value = true;
										descriptionContainer.Position.Value = new Vector2(t.Time, timelineHeight * 0.5f);
										description.Text.Value = t.Shape.ToString();
										selectedTrigger = t;
										break;
									}
								}
							}
						}
						else
							descriptionContainer.Visible.Value = false;
					}
				};
				update.EnabledInEditMode.Value = true;
				update.Add(new CommandBinding(timelineScroller.Delete, update.Delete));
				ui.Entity.Add(update);

				timelineScroller.Add(new Binding<bool>(timelineScroller.EnableScroll, x => !x, input.GetKey(Keys.LeftAlt)));
				timelineScroller.Add(new CommandBinding<Point, int>(timelineScroller.MouseScrolled, () => input.GetKey(Keys.LeftAlt), delegate(Point point, int delta)
				{
					float newScale = Math.Max(timeline.Scale.Value.X + delta * 6.0f, timelineScroller.Size.Value.X / timeline.Size.Value.X);
					Matrix absoluteTransform = timeline.GetAbsoluteTransform();
					float x = input.Mouse.Value.X + ((absoluteTransform.Translation.X - input.Mouse.Value.X) * (newScale / timeline.Scale.Value.X));
					timeline.Position.Value = new Vector2(x, 0.0f);
					timeline.Scale.Value = new Vector2(newScale, 1.0f);
				}));

				UIComponent delete = ui.BuildButton(new Command
				{
					Action = delegate()
					{
						this.Clips.Remove(clip);
					}
				}, "[Delete]");
				return new[] { name, soundFile, timelineScroller, delete };
			}));

			propertyList.Children.Add(ui.BuildButton(new Command
			{
				Action = delegate()
				{
					this.Clips.Add(new Clip());
				}
			},
			"[Add new]"));
		}
	}
}
