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
				Log.d("Entity " + id + " not found!");
			return result;
		}

		protected static Container showMessage(Func<string> text, params IProperty[] properties)
		{
			return ((GameMain)main).ShowMessage(WorldFactory.Get(), text, properties);
		}

		protected static Container showMessage(string text)
		{
			return ((GameMain)main).ShowMessage(WorldFactory.Get(), text);
		}

		protected static void hideMessage(Container container, float delay = 0.0f)
		{
			((GameMain)main).HideMessage(WorldFactory.Get(), container, delay);
		}

		protected static void bindTrigger(string id, Action callback, bool oneTimeOnly = true)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				return;

			Trigger trigger = triggerEntity.Get<Trigger>();
			Action[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate() { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding(trigger.Entered, callbacks));
		}

		protected static void bindTriggerLeave(string id, Action callback, bool oneTimeOnly = false)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				return;
			Trigger trigger = triggerEntity.Get<Trigger>();
			Action[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate() { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding(trigger.Exited, callbacks));
		}

		protected static void bindTrigger(string id, Action<Entity> callback, bool oneTimeOnly = true)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				return;
			PlayerTrigger trigger = triggerEntity.Get<PlayerTrigger>();
			Action<Entity>[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate(Entity e) { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding<Entity>(trigger.PlayerEntered, callbacks));
		}

		protected static void bindTriggerLeave(string id, Action<Entity> callback, bool oneTimeOnly = false)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				return;
			PlayerTrigger trigger = triggerEntity.Get<PlayerTrigger>();
			Action<Entity>[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate(Entity p) { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding<Entity>(trigger.PlayerExited, callbacks));
		}
	}
}
