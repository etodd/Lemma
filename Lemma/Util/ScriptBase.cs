using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;

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
	}
}
