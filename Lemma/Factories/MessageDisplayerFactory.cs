using System;
using System.Security.Cryptography;
using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class MessageDisplayerFactory : Factory<Main>
	{
		public MessageDisplayerFactory()
		{
			this.Color = new Vector3(0.57f, 0.87f, 0.75f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "MessageDisplayer");
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			Scriptlike.AttachEditorComponents(entity, main, this.Color);
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			entity.CannotSuspend = true;
			Transform transform = entity.GetOrCreate<Transform>("Transform");
			MessageDisplayer message = entity.GetOrCreate<MessageDisplayer>("MessageDisplayer");

			base.Bind(entity, main, creating);

			entity.Add("Message", message.Message, "The message to display");
			entity.Add("Display Time", message.DisplayLength, "The time to display the message. If 0, the message will stay until Hide is called.");
			entity.Add("Display", message.Display);
			entity.Add("One Time Only", message.OneTimeOnly, "If true, the message will only display once (resets on map reload)");
			entity.Add("Hide", message.Hide);
		}
	}
}
