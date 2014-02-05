using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Factories;

namespace Lemma.Scripts
{
	public class ScriptBase
	{
		public static Main main;
		public static Renderer renderer;

		protected static Entity get(string id)
		{
			Entity result = ScriptBase.main.GetByID(id);
			if (result == null)
				throw new Exception("Entity " + id + " not found!");
			return result;
		}

		protected static void bindTrigger(string id, Action callback, bool oneTimeOnly = true)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				throw new Exception("Entity " + id + " not found!");
			Trigger trigger = triggerEntity.Get<Trigger>();
			Action[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate() { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding(trigger.Entered, callbacks));
		}

		protected static void bindTrigger(string id, Action<Entity> callback, bool oneTimeOnly = true)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				throw new Exception("Entity " + id + " not found!");
			PlayerTrigger trigger = triggerEntity.Get<PlayerTrigger>();
			Action<Entity>[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate(Entity e) { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding<Entity>(trigger.PlayerEntered, callbacks));
		}

		private const float messageFadeTime = 1.0f;
		private const float messageBackgroundOpacity = 0.75f;

		private static Container buildMessage(bool centered = false)
		{
			Container msgBackground = new Container();

			if (centered)
			{
				((GameMain)main).UI.Root.Children.Add(msgBackground);
				msgBackground.Add(new Binding<Vector2, Point>(msgBackground.Position, x => new Vector2(x.X * 0.5f, x.Y * 0.5f), main.ScreenSize));
				msgBackground.AnchorPoint.Value = new Vector2(0.5f);
			}
			else
				((GameMain)main).UI.Root.GetChildByName("Messages").Children.Add(msgBackground);

			msgBackground.Tint.Value = Color.Black;
			msgBackground.Opacity.Value = 0.0f;
			TextElement msg = new TextElement();
			msg.FontFile.Value = "Font";
			msg.Opacity.Value = 0.0f;
			msg.WrapWidth.Value = 250.0f;
			msgBackground.Children.Add(msg);
			return msgBackground;
		}

		protected static Container showMessage(Func<string> text, params IProperty[] properties)
		{
			Container container = buildMessage();
			TextElement textElement = (TextElement)container.Children[0];
			textElement.Add(new Binding<string>(textElement.Text, text, properties));
			WorldFactory.Get().Add(new Animation
			(
				new Animation.Parallel
				(
					new Animation.FloatMoveTo(container.Opacity, messageBackgroundOpacity, messageFadeTime),
					new Animation.FloatMoveTo(textElement.Opacity, 1.0f, messageFadeTime)
				)
			));
			return container;
		}

		protected static Container showMessage(string text, bool centered = false)
		{
			Container container = buildMessage(centered);
			TextElement textElement = (TextElement)container.Children[0];
			textElement.Text.Value = text;
			WorldFactory.Get().Add(new Animation
			(
				new Animation.Parallel
				(
					new Animation.FloatMoveTo(container.Opacity, messageBackgroundOpacity, messageFadeTime),
					new Animation.FloatMoveTo(textElement.Opacity, 1.0f, messageFadeTime)
				)
			));
			return container;
		}

		protected static void hideMessage(Container container, float delay = 0.0f)
		{
			if (container != null && container.Active)
			{
				WorldFactory.Get().Add(new Animation
				(
					new Animation.Delay(messageFadeTime + delay),
					new Animation.Parallel
					(
						new Animation.FloatMoveTo(container.Opacity, 0.0f, messageFadeTime),
						new Animation.FloatMoveTo(((TextElement)container.Children[0]).Opacity, 0.0f, messageFadeTime)
					),
					new Animation.Execute(container.Delete)
				));
			}
		}
	}
}
