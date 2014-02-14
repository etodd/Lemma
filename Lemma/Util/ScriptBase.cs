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
			return ((GameMain)main).ShowMessage(text, properties);
		}

		protected static Container showMessage(string text, bool centered = false)
		{
			return ((GameMain)main).ShowMessage(text, centered);
		}

		protected static void hideMessage(Container container, float delay = 0.0f)
		{
			((GameMain)main).HideMessage(container, delay);
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
	}
}
