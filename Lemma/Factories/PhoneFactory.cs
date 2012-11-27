using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Input;

namespace Lemma.Factories
{
	public class PhoneFactory : Factory
	{
		public PhoneFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Phone");
			result.Add("Phone", new Phone());

			result.Add("Active", new Property<bool> { Value = false });
			result.Add("Attached", new Property<bool> { Value = false });

			PhysicsBlock physics = new PhysicsBlock();
			physics.Size.Value = new Vector3(0.4f, 0.25f, 0.25f);
			physics.Mass.Value = 0.025f;
			result.Add("Physics", physics);

			Model model = new Model();
			result.Add("Model", model);
			model.Filename.Value = "Models\\phone";
			model.Color.Value = new Vector3(0.2f);
			model.Editable = false;

			PointLight light = new PointLight();
			light.Color.Value = Vector3.One;
			light.Attenuation.Value = 4.0f;
			light.Shadowed.Value = false;
			result.Add("Light", light);

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 3.0f;
			result.Add("Trigger", trigger);

			Transform transform = new Transform();
			result.Add("Transform", transform);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			Model model = result.Get<Model>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			PhysicsBlock physics = result.Get<PhysicsBlock>();
			Phone phone = result.Get<Phone>();
			PCInput input = new PCInput();
			result.Add("Input", input);

			PointLight light = result.Get<PointLight>();

			UIRenderer ui = new UIRenderer();
			result.Add("UIRenderer", ui);

			Sprite phoneSprite = new Sprite();
			phoneSprite.Image.Value = "Images\\phone";
			phoneSprite.AnchorPoint.Value = new Vector2(0.5f, 0);
			phoneSprite.Name.Value = "phone";
			phoneSprite.Visible.Value = false;
			ui.Root.Children.Add(phoneSprite);

			Scroller phoneScroller = new Scroller();
			phoneScroller.AnchorPoint.Value = new Vector2(0.5f, 0);
			phoneScroller.Name.Value = "scroller";
			phoneSprite.Children.Add(phoneScroller);

			ListContainer messageList = new ListContainer();
			messageList.Name.Value = "messages";
			messageList.AnchorPoint.Value = new Vector2(0, 0);
			messageList.Spacing.Value = 20.0f;
			phoneScroller.Children.Add(messageList);

			Sprite phoneLight = new Sprite();
			phoneLight.Image.Value = "Images\\phone-light";
			phoneLight.AnchorPoint.Value = new Vector2(1, 1);
			phoneLight.Name.Value = "phone-light";
			phoneLight.Visible.Value = false;
			ui.Root.Children.Add(phoneLight);

			Container composeBackground = new Container();
			composeBackground.Tint.Value = new Color(0.1f, 0.1f, 0.1f);
			composeBackground.Name.Value = "compose-button";
			composeBackground.AnchorPoint.Value = new Vector2(1, 0);
			phoneSprite.Children.Add(composeBackground);

			TextElement composeMessage = new TextElement();
			composeMessage.FontFile.Value = "Font";
			composeMessage.Text.Value = "Compose";
			composeBackground.Children.Add(composeMessage);

			Container responseBackground = new Container();
			responseBackground.Tint.Value = new Color(0.1f, 0.1f, 0.1f);
			responseBackground.Opacity.Value = 1.0f;
			responseBackground.Name.Value = "response-menu";
			responseBackground.AnchorPoint.Value = new Vector2(1, 1);
			responseBackground.Visible.Value = false;
			phoneSprite.Children.Add(responseBackground);

			ListContainer responseList = new ListContainer();
			responseList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			responseList.Name.Value = "responses";
			responseBackground.Children.Add(responseList);

			Property<bool> active = result.GetProperty<bool>("Active");

			Property<bool> attached = result.GetProperty<bool>("Attached");

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));
			physics.Add(new Binding<bool>(physics.Enabled, x => !x, attached));
			model.Add(new Binding<bool>(model.Enabled, x => !x, attached));
			trigger.Add(new Binding<bool>(trigger.Enabled, x => !x, attached));
			input.Add(new Binding<bool>(input.Enabled, active));
			ui.Add(new Binding<bool>(ui.Enabled, attached));
			light.Add(new Binding<bool>(light.Enabled, x => !x, attached));
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			Command hidePhone = null;
			Command showPhone = null;

			Binding<Vector3> attachBinding = null;
			result.Add("Attach", new Command<Entity>
			{
				Action = delegate(Entity e)
				{
					attached.Value = true;
					attachBinding = new Binding<Vector3>(transform.Position, e.Get<Transform>().Position);
					result.Add(attachBinding);
				}
			});

			result.Add("Detach", new Command
			{
				Action = delegate()
				{
					attached.Value = false;
					result.Remove(attachBinding);
					if (active)
						hidePhone.Execute();
				}
			});

			result.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				player.GetProperty<Entity.Handle>("Phone").Value = result;
				showPhone.Execute();
			}));

			phoneSprite.Add(new Binding<Vector2, Point>(phoneSprite.Position, x => new Vector2(x.X * 0.5f, x.Y), main.ScreenSize));
			phoneLight.Add(new Binding<Vector2, Point>(phoneLight.Position, x => new Vector2(x.X - 20, x.Y - 20), main.ScreenSize));

			const float padding = 20.0f;
			phoneScroller.Add(new Binding<Vector2>(phoneScroller.Position, x => new Vector2(x.X * 0.5f, 61 + padding), phoneSprite.Size));
			phoneScroller.Add(new Binding<Vector2>(phoneScroller.Size, x => new Vector2(x.X - 64 - (padding * 2.0f), x.Y - (61 + 145 + (padding * 2.0f))), phoneSprite.Size));

			Container composeButton = (Container)phoneSprite.GetChildByName("compose-button");
			composeButton.Add(new Binding<Vector2>(composeButton.Position, () => phoneScroller.Position + (phoneScroller.InverseAnchorPoint.Value * phoneScroller.ScaledSize), phoneScroller.Position, phoneScroller.AnchorPoint, phoneScroller.ScaledSize));
			composeButton.Add(new Binding<Color>(composeButton.Tint, () => composeButton.Highlighted ? new Color(0.3f, 0.3f, 0.3f) : (phone.Responses.Size > 0 ? new Color(0.4f, 0.1f, 0.1f) : new Color(0.1f, 0.1f, 0.1f)), composeButton.Highlighted, phone.Responses.Size));

			Container responseMenu = (Container)phoneSprite.GetChildByName("response-menu");
			responseMenu.Add(new Binding<Vector2>(responseMenu.Position, () => composeButton.Position - new Vector2(0, composeButton.AnchorPoint.Value.Y * composeButton.ScaledSize.Value.Y), composeButton.Position, composeButton.AnchorPoint, composeButton.ScaledSize));

			ListContainer responses = (ListContainer)responseMenu.GetChildByName("responses");
			responses.Add(new ListBinding<UIComponent, Phone.Response>
			(
				responses.Children,
				phone.Responses,
				delegate(Phone.Response response)
				{
					Container field = new Container();
					field.Tint.Value = new Color(1.0f, 1.0f, 1.0f);
					field.Opacity.Value = 0.3f;
					field.Add(new Binding<float, bool>(field.Opacity, x => x ? 0.4f : 0.2f, field.Highlighted));
					field.Add(new CommandBinding<Point>(field.MouseLeftDown, delegate(Point mouse)
					{
						phone.Respond(response);
						responseMenu.Visible.Value = false;
						phoneScroller.CheckLayout();
						phoneScroller.ScrollToBottom();
						result.Add(new Animation(new Animation.Delay(0.1f), new Animation.Execute(hidePhone.Execute)));
					}));

					TextElement textField = new TextElement();
					textField.FontFile.Value = "Font";
					textField.Add(new Binding<float, Vector2>(textField.WrapWidth, x => x.X - 16.0f, phoneScroller.Size));
					textField.Text.Value = response.Text;
					field.Children.Add(textField);
					return new[] { field };
				}
			));

			composeButton.Add(new CommandBinding<Point>(composeButton.MouseLeftDown, delegate(Point mouse)
			{
				if (responses.Children.Count > 0 || responseMenu.Visible)
					responseMenu.Visible.Value = !responseMenu.Visible;
			}));

			messageList.Add(new ListBinding<UIComponent, Phone.Message>
			(
				messageList.Children,
				phone.Messages,
				delegate(Phone.Message msg)
				{
					Container field = new Container();
					field.Tint.Value = new Color(0.0f, 0.6f, 1.0f);
					field.Opacity.Value = msg.Incoming ? 0.3f : 0.15f;

					TextElement textField = new TextElement();
					textField.FontFile.Value = "Font";
					textField.Opacity.Value = msg.Incoming ? 1.0f : 0.75f;
					textField.Text.Value = msg.Text;
					textField.Add(new Binding<float, Vector2>(textField.WrapWidth, x => x.X - 32.0f, phoneScroller.Size));
					field.Children.Add(textField);
					return new[] { field };
				}
			));

			phoneScroller.ScrollToBottom();

			result.Add(new Binding<bool>(main.IsMouseVisible, active, () => attached));

			// Vibrate the phone and start the light blinking when we receive a message
			Animation lightBlinker = new Animation
			(
				new Animation.RepeatForever
				(
					new Animation.Sequence
					(
						new Animation.Set<bool>(phoneLight.Visible, true),
						new Animation.Delay(0.25f),
						new Animation.Set<bool>(phoneLight.Visible, false),
						new Animation.Delay(2.0f)
					)
				)
			);
			lightBlinker.Add(new Binding<bool>(lightBlinker.Enabled, phone.HasUnreadMessages));
			result.Add(lightBlinker);

			phone.Messages.ItemAdded += delegate(int index, Phone.Message msg)
			{
				if (!msg.Incoming) // Only for incoming messages
					return;

				Sound.PlayCue(main, "Phone Vibrate");

				if (!active)
					phone.HasUnreadMessages.Value = true;
			};

			const float phoneTransitionTime = 0.25f;
			Animation phoneAnimation = null;

			MouseState mouseState = new MouseState();

			showPhone = new Command
			{
				Action = delegate()
				{
					if (phoneAnimation == null || !phoneAnimation.Active)
					{
						// Show the phone
						mouseState = main.MouseState;

						active.Value = true;
						phoneSprite.Visible.Value = true;

						if (phone.HasUnreadMessages)
							phoneScroller.ScrollToBottom();
						phone.HasUnreadMessages.Value = false;

						phoneLight.Visible.Value = false;

						phoneAnimation = new Animation
						(
							new Animation.Parallel
							(
								new Animation.FloatMoveTo(main.Renderer.BlurAmount, 1.0f, phoneTransitionTime),
								new Animation.Vector2MoveTo(phoneSprite.AnchorPoint, new Vector2(0.5f, 1), phoneTransitionTime)
							)
						);
						result.Add(phoneAnimation);
					}
				}
			};

			result.Add("Show", showPhone);

			hidePhone = new Command
			{
				Action = delegate()
				{
					if (phoneAnimation == null || !phoneAnimation.Active)
					{
						// Hide the phone
						main.MouseState.Value = main.LastMouseState.Value = mouseState;

						active.Value = false;
						phoneAnimation = new Animation
						(
							new Animation.Parallel
							(
								new Animation.FloatMoveTo(main.Renderer.BlurAmount, 0.0f, phoneTransitionTime),
								new Animation.Vector2MoveTo(phoneSprite.AnchorPoint, new Vector2(0.5f, 0), phoneTransitionTime)
							),
							new Animation.Execute(delegate() { phoneSprite.Visible.Value = false; })
						);
						result.Add(phoneAnimation);
					}
				}
			};

			result.Add("Hide", hidePhone);

			input.Add(new CommandBinding(input.GetKeyDown(Keys.Tab), hidePhone));

			this.SetMain(result, main);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			// Don't attach the default editor components.
			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}
