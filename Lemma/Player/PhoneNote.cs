using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using GeeUI.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Lemma.Components
{
	public class PhoneNote
	{
		public static void Attach(Main main, Entity entity, Player player, AnimatedModel model, FPSInput input, Phone phone, Property<bool> enableWalking, Property<bool> phoneActive, Property<bool> noteActive)
		{
			UIRenderer phoneUi = entity.GetOrCreate<UIRenderer>("PhoneUI");

			const float phoneWidth = 200.0f;

			phoneUi.RenderTargetBackground.Value = Microsoft.Xna.Framework.Color.White;
			phoneUi.RenderTargetSize.Value = new Point((int)phoneWidth, (int)(phoneWidth * 2.0f));
			phoneUi.Serialize = false;
			phoneUi.Enabled.Value = false;
#if VR
			if (main.VR)
				phoneUi.Reticle.Tint.Value = new Color(0.0f, 0.0f, 0.0f);
#endif

			Model phoneModel = entity.GetOrCreate<Model>("PhoneModel");
			phoneModel.Filename.Value = "Models\\phone";
			phoneModel.Color.Value = new Vector3(0.13f, 0.13f, 0.13f);
			phoneModel.Serialize = false;
			phoneModel.Enabled.Value = false;

			Property<Matrix> phoneBone = model.GetBoneTransform("Phone");
			phoneModel.Add(new Binding<Matrix>(phoneModel.Transform, () => phoneBone.Value * model.Transform, phoneBone, model.Transform));

			Model screen = entity.GetOrCreate<Model>("Screen");
			screen.Filename.Value = "Models\\plane";
			screen.Add(new Binding<Microsoft.Xna.Framework.Graphics.RenderTarget2D>(screen.GetRenderTarget2DParameter("Diffuse" + Model.SamplerPostfix), phoneUi.RenderTarget));
			screen.Add(new Binding<Matrix>(screen.Transform, x => Matrix.CreateTranslation(0.015f, 0.0f, 0.0f) * x, phoneModel.Transform));
			screen.Serialize = false;
			screen.Enabled.Value = false;

			PointLight phoneLight = entity.Create<PointLight>();
			phoneLight.Serialize = false;
			phoneLight.Enabled.Value = false;
			phoneLight.Attenuation.Value = 0.5f;
			phoneLight.Add(new Binding<Vector3, Matrix>(phoneLight.Position, x => x.Translation, screen.Transform));

			PointLight noteLight = entity.Create<PointLight>();
			noteLight.Serialize = false;
			noteLight.Enabled.Value = false;
			noteLight.Attenuation.Value = 1.0f;
			noteLight.Color.Value = new Vector3(0.3f);
			noteLight.Add(new Binding<Vector3>(noteLight.Position, () => Vector3.Transform(new Vector3(0.25f, 0.0f, 0.0f), phoneBone.Value * model.Transform), phoneBone, model.Transform));

			const float screenScale = 0.0007f;
			screen.Scale.Value = new Vector3(1.0f, (float)phoneUi.RenderTargetSize.Value.Y * screenScale, (float)phoneUi.RenderTargetSize.Value.X * screenScale);

			// Transform screen space mouse position into 3D, then back into the 2D space of the phone UI
			Property<Matrix> screenTransform = new Property<Matrix>();
			screen.Add(new Binding<Matrix>(screenTransform, () => Matrix.CreateScale(screen.Scale) * screen.Transform, screen.Scale, screen.Transform));
			phoneUi.Setup3D(screenTransform);

			// Phone UI

			const float padding = 8.0f;
			const float messageWidth = phoneWidth - padding * 2.0f;

			Func<Property<Color>, string, float, Container> makeButton = delegate(Property<Color> color, string text, float width)
			{
				Container bg = new Container();
				bg.Tint.Value = color;
				bg.PaddingBottom.Value = bg.PaddingLeft.Value = bg.PaddingRight.Value = bg.PaddingTop.Value = padding * 0.5f;
				bg.Add(new Binding<Color>(bg.Tint, () => bg.Highlighted ? new Color(color.Value.ToVector4() + new Vector4(0.2f, 0.2f, 0.2f, 0.0f)) : color, bg.Highlighted, color));

				TextElement msg = new TextElement();
				msg.Name.Value = "Text";
				msg.FontFile.Value = main.Font;
				msg.Text.Value = text;
				msg.WrapWidth.Value = width;
				bg.Children.Add(msg);
				return bg;
			};

			Action<Container, float> centerButton = delegate(Container button, float width)
			{
				TextElement text = (TextElement)button.Children[0];
				text.AnchorPoint.Value = new Vector2(0.5f, 0);
				text.Add(new Binding<Vector2>(text.Position, x => new Vector2(x.X * 0.5f, padding), button.Size));
				button.ResizeHorizontal.Value = false;
				button.ResizeVertical.Value = false;
				button.Size.Value = new Vector2(width, 36.0f * main.FontMultiplier);
			};

			Func<UIComponent, bool, Container> makeAlign = delegate(UIComponent component, bool right)
			{
				Container container = new Container();
				container.Opacity.Value = 0.0f;
				container.PaddingBottom.Value = container.PaddingLeft.Value = container.PaddingRight.Value = container.PaddingTop.Value = 0.0f;
				container.ResizeHorizontal.Value = false;
				container.Size.Value = new Vector2(messageWidth, 0.0f);
				component.AnchorPoint.Value = new Vector2(right ? 1.0f : 0.0f, 0.0f);
				component.Position.Value = new Vector2(right ? messageWidth : 0.0f, 0.0f);
				container.Children.Add(component);
				return container;
			};

			Property<Color> incomingColor = new Property<Color> { Value = new Color(0.0f, 0.0f, 0.0f, 1.0f) };
			Property<Color> outgoingColor = new Property<Color> { Value = new Color(0.0f, 0.175f, 0.35f, 1.0f) };
			Property<Color> composeColor = new Property<Color> { Value = new Color(0.5f, 0.0f, 0.0f, 1.0f) };
			Property<Color> disabledColor = new Property<Color> { Value = new Color(0.35f, 0.35f, 0.35f, 1.0f) };
			Property<Color> topBarColor = new Property<Color> { Value = new Color(0.15f, 0.15f, 0.15f, 1.0f) };

			Container topBarContainer = new Container();
			topBarContainer.ResizeHorizontal.Value = false;
			topBarContainer.Size.Value = new Vector2(phoneUi.RenderTargetSize.Value.X, 0.0f);
			topBarContainer.Tint.Value = topBarColor;
			phoneUi.Root.Children.Add(topBarContainer);

			ListContainer phoneTopBar = new ListContainer();
			phoneTopBar.Orientation.Value = ListContainer.ListOrientation.Horizontal;
			phoneTopBar.Spacing.Value = padding;
			topBarContainer.Children.Add(phoneTopBar);

			Sprite signalIcon = new Sprite();
			signalIcon.Image.Value = "Images\\signal";
			phoneTopBar.Children.Add(signalIcon);

			TextElement noService = new TextElement();
			noService.FontFile.Value = main.Font;
			noService.Text.Value = "\\no service";
			phoneTopBar.Children.Add(noService);

			signalIcon.Add(new Binding<bool>(signalIcon.Visible, () => player.SignalTower.Value.Target != null || phone.ActiveAnswers.Length > 0 || phone.Schedules.Length > 0, player.SignalTower, phone.ActiveAnswers.Length, phone.Schedules.Length));
			noService.Add(new Binding<bool>(noService.Visible, x => !x, signalIcon.Visible));

			ListContainer tabs = new ListContainer();
			tabs.Orientation.Value = ListContainer.ListOrientation.Horizontal;
			tabs.Spacing.Value = 0;
			phoneUi.Root.Children.Add(tabs);

			Property<Color> messageTabColor = new Property<Color> { Value = outgoingColor };
			phoneUi.Add(new Binding<Color, Phone.Mode>(messageTabColor, x => x == Phone.Mode.Messages ? outgoingColor : topBarColor, phone.CurrentMode));
			Container messageTab = makeButton(messageTabColor, "\\messages", phoneUi.RenderTargetSize.Value.X * 0.5f - padding);
			centerButton(messageTab, phoneUi.RenderTargetSize.Value.X * 0.5f);
			tabs.Children.Add(messageTab);
			messageTab.Add(new CommandBinding(messageTab.MouseLeftUp, delegate()
			{
				phone.CurrentMode.Value = Phone.Mode.Messages;
			}));

			Property<Color> photoTabColor = new Property<Color> { Value = topBarColor };
			phoneUi.Add(new Binding<Color>(photoTabColor, delegate()
			{
				if (phone.CurrentMode == Phone.Mode.Photos)
					return outgoingColor;
				else if (string.IsNullOrEmpty(phone.Photo))
					return disabledColor;
				else
					return topBarColor;
			}, phone.CurrentMode, phone.Photo));
			Container photoTab = makeButton(photoTabColor, "\\photos", phoneUi.RenderTargetSize.Value.X * 0.5f - padding);
			centerButton(photoTab, phoneUi.RenderTargetSize.Value.X * 0.5f);
			tabs.Children.Add(photoTab);
			photoTab.Add(new CommandBinding(photoTab.MouseLeftUp, delegate()
			{
				if (!string.IsNullOrEmpty(phone.Photo))
					phone.CurrentMode.Value = Phone.Mode.Photos;
			}));

			tabs.Add(new Binding<Vector2>(tabs.Position, x => new Vector2(0, x.Y), topBarContainer.Size));

			ListContainer messageLayout = new ListContainer();
			messageLayout.Spacing.Value = padding;
			messageLayout.Orientation.Value = ListContainer.ListOrientation.Vertical;
			messageLayout.Add(new Binding<Vector2>(messageLayout.Position, () => new Vector2(padding, topBarContainer.Size.Value.Y + tabs.Size.Value.Y), topBarContainer.Size, tabs.Size));
			messageLayout.Add(new Binding<Vector2>(messageLayout.Size, () => new Vector2(phoneUi.RenderTargetSize.Value.X - padding * 2.0f, phoneUi.RenderTargetSize.Value.Y - padding - topBarContainer.Size.Value.Y - tabs.Size.Value.Y), phoneUi.RenderTargetSize, topBarContainer.Size, tabs.Size));
			messageLayout.Add(new Binding<bool, Phone.Mode>(messageLayout.Visible, x => x == Phone.Mode.Messages, phone.CurrentMode));
			phoneUi.Root.Children.Add(messageLayout);

			Container photoLayout = new Container();
			photoLayout.Opacity.Value = 0;
			photoLayout.PaddingLeft.Value = photoLayout.PaddingRight.Value = photoLayout.PaddingTop.Value = photoLayout.PaddingBottom.Value = 0;
			photoLayout.Add(new Binding<Vector2>(photoLayout.Position, () => new Vector2(0, topBarContainer.Size.Value.Y + tabs.Size.Value.Y), topBarContainer.Size, tabs.Size));
			photoLayout.Add(new Binding<Vector2>(photoLayout.Size, () => new Vector2(phoneUi.RenderTargetSize.Value.X, phoneUi.RenderTargetSize.Value.Y - topBarContainer.Size.Value.Y - tabs.Size.Value.Y), phoneUi.RenderTargetSize, topBarContainer.Size, tabs.Size));
			photoLayout.Add(new Binding<bool>(photoLayout.Visible, x => !x, messageLayout.Visible));
			phoneUi.Root.Children.Add(photoLayout);

			Sprite photoImage = new Sprite();
			photoImage.AnchorPoint.Value = new Vector2(0.5f, 0.5f);
			photoImage.Add(new Binding<string>(photoImage.Image, phone.Photo));
			photoImage.Add(new Binding<Vector2>(photoImage.Position, x => x * 0.5f, photoLayout.Size));
			photoLayout.Children.Add(photoImage);

			Container composeButton = makeButton(composeColor, "\\compose", messageWidth - padding * 2.0f);
			TextElement composeText = (TextElement)composeButton.GetChildByName("Text");
			composeText.Add(new Binding<string, bool>(composeText.Text, x => x ? "\\compose gamepad" : "\\compose", main.GamePadConnected));
			UIComponent composeAlign = makeAlign(composeButton, true);

			Scroller phoneScroll = new Scroller();
			phoneScroll.ResizeVertical.Value = false;
			phoneScroll.Add(new Binding<Vector2>(phoneScroll.Size, () => new Vector2(messageLayout.Size.Value.X, messageLayout.Size.Value.Y - messageLayout.Spacing.Value - composeAlign.ScaledSize.Value.Y), messageLayout.Size, messageLayout.Spacing, composeAlign.ScaledSize));

			messageLayout.Children.Add(phoneScroll);
			messageLayout.Children.Add(composeAlign);

			ListContainer msgList = new ListContainer();
			msgList.Spacing.Value = padding * 0.5f;
			msgList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			msgList.ResizePerpendicular.Value = false;
			msgList.Size.Value = new Vector2(messageWidth, 0.0f);
			phoneScroll.Children.Add(msgList);

			Container answerContainer = new Container();
			answerContainer.PaddingBottom.Value = answerContainer.PaddingLeft.Value = answerContainer.PaddingRight.Value = answerContainer.PaddingTop.Value = padding;
			answerContainer.Tint.Value = incomingColor;
			answerContainer.AnchorPoint.Value = new Vector2(1.0f, 1.0f);
			answerContainer.Add(new Binding<Vector2>(answerContainer.Position, () => composeAlign.GetAbsolutePosition() + new Vector2(composeAlign.ScaledSize.Value.X, 0), composeAlign.Position, composeAlign.ScaledSize));
			phoneUi.Root.Children.Add(answerContainer);
			answerContainer.Visible.Value = false;

			ListContainer answerList = new ListContainer();
			answerList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			answerList.Alignment.Value = ListContainer.ListAlignment.Max;
			answerContainer.Children.Add(answerList);

			int selectedAnswer = 0;

			composeButton.Add(new CommandBinding(composeButton.MouseLeftUp, delegate()
			{
				answerContainer.Visible.Value = !answerContainer.Visible;
				if (answerContainer.Visible && main.GamePadConnected)
				{
					selectedAnswer = 0;
					foreach (UIComponent answer in answerList.Children)
						answer.Highlighted.Value = false;
					answerList.Children[0].Highlighted.Value = true;
				}
			}));

			tabs.Add(new Binding<bool>(tabs.EnableInput, () => !main.Paused && !answerContainer.Visible, answerContainer.Visible, main.Paused));
			msgList.Add(new Binding<bool>(msgList.EnableInput, () => !main.Paused && !answerContainer.Visible, answerContainer.Visible, main.Paused));
			answerContainer.Add(new Binding<bool>(answerContainer.EnableInput, x => !x, main.Paused));
			composeButton.Add(new Binding<bool>(composeButton.EnableInput, x => !x, main.Paused));

			Action scrollToBottom = delegate()
			{
				// HACK
				Animation scroll = new Animation
				(
					new Animation.Delay(0.01f),
					new Animation.Execute(delegate()
					{
						phoneScroll.ScrollToBottom();
					})
				);
				entity.Add(scroll);
			};

			// Note

			UIRenderer noteUi = entity.GetOrCreate<UIRenderer>("NoteUI");

			const float noteWidth = 400.0f;

			noteUi.RenderTargetBackground.Value = new Microsoft.Xna.Framework.Color(0.8f, 0.75f, 0.7f);
			noteUi.RenderTargetSize.Value = new Point((int)noteWidth, (int)(noteWidth * 1.29f)); // 8.5x11 aspect ratio
			noteUi.Serialize = false;
			noteUi.Enabled.Value = false;

			Model noteModel = entity.GetOrCreate<Model>("Note");
			noteModel.Filename.Value = "Models\\note";
			noteModel.Add(new Binding<Microsoft.Xna.Framework.Graphics.RenderTarget2D>(noteModel.GetRenderTarget2DParameter("Diffuse" + Model.SamplerPostfix), noteUi.RenderTarget));
			noteModel.Add(new Binding<Matrix>(noteModel.Transform, x => Matrix.CreateTranslation(-0.005f, 0.05f, 0.08f) * x, phoneModel.Transform));
			noteModel.Serialize = false;
			noteModel.Enabled.Value = false;

			Container togglePhoneMessage = null;

			entity.Add(new NotifyBinding(delegate()
			{
				bool hasSignalTower = (player.SignalTower.Value.Target != null && player.SignalTower.Value.Target.Active && !string.IsNullOrEmpty(player.SignalTower.Value.Target.Get<SignalTower>().Initial));
				if (hasSignalTower)
					phone.Enabled.Value = true;

				bool hasNoteOrSignalTower = (player.Note.Value.Target != null && player.Note.Value.Target.Active) || hasSignalTower;

				if (togglePhoneMessage == null && hasNoteOrSignalTower)
					togglePhoneMessage = main.Menu.ShowMessage(entity, hasSignalTower ? "\\signal tower prompt" : "\\note prompt");
				else if (togglePhoneMessage != null && !hasNoteOrSignalTower && !phoneActive && !noteActive)
				{
					main.Menu.HideMessage(entity, togglePhoneMessage);
					togglePhoneMessage = null;
				}
			}, player.Note, player.SignalTower));

			entity.Add(new CommandBinding(entity.Delete, delegate()
			{
				main.Menu.HideMessage(null, togglePhoneMessage);
			}));

			// Note UI

			const float notePadding = 40.0f;

			ListContainer noteLayout = new ListContainer();
			noteLayout.Spacing.Value = padding;
			noteLayout.Orientation.Value = ListContainer.ListOrientation.Vertical;
			noteLayout.Alignment.Value = ListContainer.ListAlignment.Min;
			noteLayout.Position.Value = new Vector2(notePadding, notePadding);
			noteLayout.Add(new Binding<Vector2, Point>(noteLayout.Size, x => new Vector2(x.X - notePadding * 2.0f, x.Y - notePadding * 2.0f), noteUi.RenderTargetSize));
			noteUi.Root.Children.Add(noteLayout);

			Sprite noteUiImage = new Sprite();
			noteLayout.Children.Add(noteUiImage);

			TextElement noteUiText = new TextElement();
			noteUiText.FontFile.Value = main.Font;
			noteUiText.Tint.Value = new Microsoft.Xna.Framework.Color(0.1f, 0.1f, 0.1f);
			noteUiText.Add(new Binding<float, Vector2>(noteUiText.WrapWidth, x => x.X, noteLayout.Size));
			noteLayout.Children.Add(noteUiText);

			// Toggle note
			Animation noteAnim = null;

			float startRotationY = 0;
			Action<bool> showNote = delegate(bool show)
			{
				model.Stop("Phone", "Note", "VRPhone", "VRNote");
				Entity noteEntity = player.Note.Value.Target;
				noteActive.Value = show && noteEntity != null;
				Note note = noteEntity != null ? noteEntity.Get<Note>() : null;
				if (noteActive)
				{
					input.EnableLook.Value = input.EnableMouse.Value = false;
					enableWalking.Value = false;
					noteModel.Enabled.Value = true;
					noteUi.Enabled.Value = true;
					noteLight.Enabled.Value = true;
					Session.Recorder.Event(main, "Note", note.Text);
					noteUiImage.Image.Value = note.Image;
					noteUiText.Text.Value = note.Text;
					string noteAnimation;
#if VR
					if (main.VR)
						noteAnimation = "VRNote";
					else
#endif
						noteAnimation = "Note";

					model.StartClip(noteAnimation, 6, true, AnimatedModel.DefaultBlendTime * 2.0f);
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_NOTE_PICKUP, entity);

					if (noteAnim != null && noteAnim.Active)
						noteAnim.Delete.Execute();
					else
						startRotationY = input.Mouse.Value.Y;
					// Level the player's view
					noteAnim = new Animation
					(
						new Animation.Ease
						(
							new Animation.Custom(delegate(float x)
							{
								input.Mouse.Value = new Vector2(input.Mouse.Value.X, startRotationY * (1.0f - x));
							}, 0.5f),
							Animation.Ease.EaseType.OutQuadratic
						)
					);
					entity.Add(noteAnim);
				}
				else
				{
					enableWalking.Value = true;
					if (note != null)
						Session.Recorder.Event(main, "NoteEnd");
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_NOTE_DROP, entity);
					if (note != null && !note.IsCollected)
						note.IsCollected.Value = true;

					// Return the player's view
					if (noteAnim != null && noteAnim.Active)
						noteAnim.Delete.Execute();
					noteAnim = new Animation
					(
						new Animation.Ease
						(
							new Animation.Custom(delegate(float x)
							{
								input.Mouse.Value = new Vector2(input.Mouse.Value.X, startRotationY * x);
							}, 0.5f),
							Animation.Ease.EaseType.OutQuadratic
						),
						new Animation.Execute(delegate()
						{
							noteModel.Enabled.Value = false;
							noteUi.Enabled.Value = false;
							noteLight.Enabled.Value = false;
							input.EnableLook.Value = input.EnableMouse.Value = true;
						})
					);
					entity.Add(noteAnim);
				}
			};

			// Toggle phone

			Animation phoneAnim = null;

			Action<bool> showPhone = delegate(bool show)
			{
				if (togglePhoneMessage != null)
				{
					main.Menu.HideMessage(entity, togglePhoneMessage);
					togglePhoneMessage = null;
				}

				if (show || (phone.Schedules.Length == 0 && !phone.WaitForAnswer))
				{
					phoneActive.Value = show;
					answerContainer.Visible.Value = false;

					model.Stop("Phone", "Note", "VRPhone", "VRNote");
					if (phoneActive)
					{
						phoneUi.IsMouseVisible.Value = true;
						enableWalking.Value = false;
						phoneModel.Enabled.Value = true;
						screen.Enabled.Value = true;
						phoneUi.Enabled.Value = true;
						phoneLight.Enabled.Value = true;
						input.EnableLook.Value = input.EnableMouse.Value = false;
						Session.Recorder.Event(main, "Phone");
						phoneScroll.CheckLayout();
						scrollToBottom();

						string phoneAnimation;
#if VR
						if (main.VR)
							phoneAnimation = "VRPhone";
						else
#endif
							phoneAnimation = "Phone";

						model.StartClip(phoneAnimation, 6, true, AnimatedModel.DefaultBlendTime * 2.0f);

						// Level the player's view
						if (phoneAnim != null && phoneAnim.Active)
							phoneAnim.Delete.Execute();
						else
							startRotationY = input.Mouse.Value.Y;
						phoneAnim = new Animation
						(
							new Animation.Ease
							(
								new Animation.Custom(delegate(float x)
								{
									input.Mouse.Value = new Vector2(input.Mouse.Value.X, startRotationY * (1.0f - x));
								}, 0.5f),
								Animation.Ease.EaseType.OutQuadratic
							)
						);
						entity.Add(phoneAnim);
					}
					else
					{
						Session.Recorder.Event(main, "PhoneEnd");
						enableWalking.Value = true;
						phoneUi.IsMouseVisible.Value = false;

						// Return the player's view
						if (phoneAnim != null && phoneAnim.Active)
							phoneAnim.Delete.Execute();
						phoneAnim = new Animation
						(
							new Animation.Ease
							(
								new Animation.Custom(delegate(float x)
								{
									input.Mouse.Value = new Vector2(input.Mouse.Value.X, startRotationY * x);
								}, 0.5f),
								Animation.Ease.EaseType.OutQuadratic
							),
							new Animation.Execute(delegate()
							{
								phoneModel.Enabled.Value = false;
								screen.Enabled.Value = false;
								phoneUi.Enabled.Value = false;
								phoneLight.Enabled.Value = false;
								input.EnableLook.Value = input.EnableMouse.Value = true;
							})
						);
						entity.Add(phoneAnim);
					}
				}
			};

			input.Bind(main.Settings.TogglePhone, PCInput.InputState.Down, delegate()
			{
				// Special hack to prevent phone toggling when you're trying to open the Steam overlay
				if (main.Settings.TogglePhone.Value.Key == Keys.Tab && input.GetKey(Keys.LeftShift))
					return;

				if (noteActive || phoneActive || phone.CanReceiveMessages)
				{
					if (!phoneActive && (noteActive || player.Note.Value.Target != null))
						showNote(!noteActive);
					else if (phone.Enabled)
						showPhone(!phoneActive);
				}
			});

			phone.Add(new CommandBinding(phone.Show, delegate()
			{
				phone.Enabled.Value = true;
				if (!phoneActive)
					showPhone(true);
			}));

			// Gamepad code for the phone

			input.Add(new CommandBinding(input.GetButtonUp(Buttons.A), () => phoneActive && composeButton.Visible && phone.CurrentMode.Value == Phone.Mode.Messages, delegate()
			{
				if (answerContainer.Visible)
					answerList.Children[selectedAnswer].MouseLeftUp.Execute();
				else
					answerContainer.Visible.Value = true;
			}));

			input.Add(new CommandBinding(input.GetButtonUp(Buttons.B), () => phoneActive && answerContainer.Visible, delegate()
			{
				answerContainer.Visible.Value = false;
			}));

			const float moveInterval = 0.1f;
			const float switchInterval = 0.2f;
			float lastScroll = 0;
			float lastModeSwitch = 0;
			Action<int> scrollPhone = delegate(int delta)
			{
				if (main.TotalTime - lastScroll > moveInterval
					&& main.TotalTime - lastModeSwitch > switchInterval)
				{
					if (answerContainer.Visible)
					{
						answerList.Children[selectedAnswer].Highlighted.Value = false;
						selectedAnswer += delta;
						while (selectedAnswer < 0)
							selectedAnswer += answerList.Children.Length;
						while (selectedAnswer > answerList.Children.Length - 1)
							selectedAnswer -= answerList.Children.Length;
						answerList.Children[selectedAnswer].Highlighted.Value = true;
					}
					else
						phoneScroll.MouseScrolled.Execute(delta * -4);
					lastScroll = main.TotalTime;
				}
			};

			Action switchMode = delegate()
			{
				if (main.TotalTime - lastScroll > switchInterval
					&& main.TotalTime - lastModeSwitch > moveInterval)
				{
					Phone.Mode current = phone.CurrentMode;
					Phone.Mode nextMode = current == Phone.Mode.Messages ? Phone.Mode.Photos : Phone.Mode.Messages;
					if (nextMode == Phone.Mode.Photos && string.IsNullOrEmpty(phone.Photo))
						nextMode = Phone.Mode.Messages;
					phone.CurrentMode.Value = nextMode;
					lastModeSwitch = main.TotalTime;
				}
			};
			input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickLeft), () => phoneActive && !answerContainer.Visible, switchMode));
			input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickRight), () => phoneActive && !answerContainer.Visible, switchMode));
			input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadLeft), () => phoneActive && !answerContainer.Visible, switchMode));
			input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadRight), () => phoneActive && !answerContainer.Visible, switchMode));

			input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickUp), () => phoneActive, delegate()
			{
				scrollPhone(-1);
			}));

			input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadUp), () => phoneActive, delegate()
			{
				scrollPhone(-1);
			}));

			input.Add(new CommandBinding(input.GetButtonDown(Buttons.LeftThumbstickDown), () => phoneActive, delegate()
			{
				scrollPhone(1);
			}));

			input.Add(new CommandBinding(input.GetButtonDown(Buttons.DPadDown), () => phoneActive, delegate()
			{
				scrollPhone(1);
			}));

			msgList.Add(new ListBinding<UIComponent, Phone.Message>
			(
				msgList.Children,
				phone.Messages,
				delegate(Phone.Message msg)
				{
					return makeAlign(makeButton(msg.Incoming ? incomingColor : outgoingColor, "\\" + msg.Name, messageWidth - padding * 2.0f), !msg.Incoming);
				}
			));

			Action<float, Container> animateMessage = delegate(float delay, Container msg)
			{
				msg.CheckLayout();
				Vector2 originalSize = msg.Size;
				msg.Size.Value = new Vector2(0, originalSize.Y);
				entity.Add(new Animation
				(
					new Animation.Delay(delay),
					new Animation.Ease(new Animation.Vector2MoveTo(msg.Size, originalSize, 0.5f), Animation.Ease.EaseType.OutExponential)
				));
			};

			Container typingIndicator = null;

			Action showTypingIndicator = delegate()
			{
				typingIndicator = makeAlign(makeButton(incomingColor, "\\...", messageWidth - padding * 2.0f), false);
				msgList.Children.Add(typingIndicator);
				animateMessage(0.2f, typingIndicator);
			};

			if (phone.Schedules.Length > 0)
				showTypingIndicator();

			answerList.Add(new ListBinding<UIComponent, Phone.Ans>
			(
				answerList.Children,
				phone.ActiveAnswers,
				delegate(Phone.Ans answer)
				{
					UIComponent button = makeButton(outgoingColor, "\\" + answer.Name, messageWidth - padding * 4.0f);
					button.Add(new CommandBinding(button.MouseLeftUp, delegate()
					{
						if (!phone.WaitForAnswer) // If we're not waiting for an answer, the player must be initiating a conversation
						{
							// This is the start of a conversation
							// Disable the signal tower if we're in range
							Entity s = player.SignalTower.Value.Target;
							if (s != null)
								s.Get<SignalTower>().Initial.Value = null;
						}

						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PHONE_SEND, entity);
						phone.Answer(answer);

						scrollToBottom();
						if (phone.Schedules.Length == 0) // No more messages incoming
						{
							if (togglePhoneMessage == null)
								togglePhoneMessage = main.Menu.ShowMessage(entity, "\\phone done prompt");
						}
						else
						{
							// More messages incoming
							showTypingIndicator();
						}
					}));
					return button;
				}
			));

			Action refreshComposeButtonVisibility = delegate()
			{
				bool show = phone.ActiveAnswers.Length > 0 && phone.Schedules.Length == 0;
				answerContainer.Visible.Value &= show;
				composeButton.Visible.Value = show;
				selectedAnswer = 0;
			};
			composeButton.Add(new ListNotifyBinding<Phone.Ans>(refreshComposeButtonVisibility, phone.ActiveAnswers));
			composeButton.Add(new ListNotifyBinding<Phone.Schedule>(refreshComposeButtonVisibility, phone.Schedules));
			refreshComposeButtonVisibility();

			entity.Add(new CommandBinding(phone.MessageReceived, delegate()
			{
				if (typingIndicator != null)
				{
					typingIndicator.Delete.Execute();
					typingIndicator = null;
				}
				
				if (phone.Schedules.Length > 0)
					showTypingIndicator();

				float delay;
				if (phoneActive)
				{
					scrollToBottom();
					delay = 0;
				}
				else
				{
					showPhone(true);
					delay = 0.5f;
				}

				// Animate the new message
				animateMessage(delay, (Container)msgList.Children[msgList.Children.Length - 1].Children[0]);

				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PHONE_VIBRATE, entity);
				if (togglePhoneMessage == null && phone.Schedules.Length == 0 && phone.ActiveAnswers.Length == 0) // No more messages incoming, and no more answers to give
					togglePhoneMessage = main.Menu.ShowMessage(entity, "[{{TogglePhone}}]");
			}));

			if (noteActive)
				showNote(true);
			else if (phoneActive)
				showPhone(true);
		}
	}
}