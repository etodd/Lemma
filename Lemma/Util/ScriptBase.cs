using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma.Scripts
{
	public class ScriptBase
	{
		public static Main main;
		public static Renderer renderer;
		public static Entity script;

		protected static Entity get(string id)
		{
			return ScriptBase.main.GetByID(id);
		}

		protected static void bindTrigger(string id, Action<Entity> callback, bool oneTimeOnly = true)
		{
			Entity triggerEntity = ScriptBase.get(id);
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

		private static Container buildMessage()
		{
			Container msgBackground = new Container();
			((GameMain)main).UI.Root.GetChildByName("Messages").Children.Add(msgBackground);
			msgBackground.Tint.Value = Color.Black;
			msgBackground.Opacity.Value = 0.0f;
			TextElement msg = new TextElement();
			msg.FontFile.Value = "Font";
			msg.Opacity.Value = 0.0f;
			msg.WrapWidth.Value = 200.0f;
			msgBackground.Children.Add(msg);
			script.Add(msgBackground);
			return msgBackground;
		}

		protected static Container showMessage(Func<string> text, params IProperty[] properties)
		{
			Container container = buildMessage();
			TextElement textElement = (TextElement)container.Children[0];
			textElement.Add(new Binding<string>(textElement.Text, text, properties));
			script.Add(new Animation
			(
				new Animation.Parallel
				(
					new Animation.FloatMoveTo(container.Opacity, messageBackgroundOpacity, messageFadeTime),
					new Animation.FloatMoveTo(textElement.Opacity, 1.0f, messageFadeTime)
				)
			));
			return container;
		}

		protected static Container showMessage(string text)
		{
			Container container = buildMessage();
			TextElement textElement = (TextElement)container.Children[0];
			textElement.Text.Value = text;
			script.Add(new Animation
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
			script.Add(new Animation
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
