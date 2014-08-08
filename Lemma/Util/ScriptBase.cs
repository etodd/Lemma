using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Lemma.Factories;
using Lemma.Console;

namespace Lemma.GameScripts
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
			return main.Menu.ShowMessage(WorldFactory.Instance, text, properties);
		}

		protected static Container showMessage(string text)
		{
			return main.Menu.ShowMessage(WorldFactory.Instance, text);
		}

		protected static void hideMessage(Container container, float delay = 0.0f)
		{
			main.Menu.HideMessage(WorldFactory.Instance, container, delay);
		}

		protected static void bindEntityTrigger(string id, Action callback, bool oneTimeOnly = true)
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

		protected static void bindEntityTriggerLeave(string id, Action callback, bool oneTimeOnly = false)
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

		protected static Property<T> property<T>(Entity script, string name, T defaultValue = default(T))
		{
			Data data = script.GetOrCreate<Data>("Data");
			return data.Property<T>(name, defaultValue);
		}

		protected static ListProperty<T> listProperty<T>(Entity script, string name)
		{
			Data data = script.GetOrCreate<Data>("Data");
			return data.ListProperty<T>(name);
		}

		protected static Command command(Entity script, string name)
		{
			Data data = script.GetOrCreate<Data>("Data");
			return data.Command(name);
		}
		
		protected static void bindPlayerTrigger(string id, Action callback, bool oneTimeOnly = true)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				return;
			PlayerTrigger trigger = triggerEntity.Get<PlayerTrigger>();
			Action[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate() { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding(trigger.PlayerEntered, callbacks));
		}

		protected static void bindTriggerLeave(string id, Action callback, bool oneTimeOnly = false)
		{
			Entity triggerEntity = ScriptBase.get(id);
			if (triggerEntity == null)
				return;
			PlayerTrigger trigger = triggerEntity.Get<PlayerTrigger>();
			Action[] callbacks;
			if (oneTimeOnly)
				callbacks = new[] { callback, delegate() { trigger.Enabled.Value = false; } };
			else
				callbacks = new[] { callback };
			triggerEntity.Add(new CommandBinding(trigger.PlayerExited, callbacks));
		}
		
		protected static void consoleCommand(ConCommand conCommand)
		{
			Lemma.Console.Console.AddConCommand(conCommand);
			WorldFactory.Instance.Add(new CommandBinding(WorldFactory.Instance.Delete, delegate()
			{
				Lemma.Console.Console.RemoveConCommand(conCommand.Name);
			}));
		}
	}
}
