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
	public class ConsoleCommandFactory : Factory<Main>
	{
		public ConsoleCommandFactory()
		{
			this.Color = new Vector3(0.0f, 1f, 0.0f);
			this.AvailableInRelease = false;
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "ConsoleCommand");
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
			ConsoleCommand cmd = entity.GetOrCreate<ConsoleCommand>("ConsoleCommand");

			base.Bind(entity, main, creating);

			entity.Add("Execute", cmd.Execute, description: "Execute console command");
			entity.Add("Name", cmd.Name, description: "Name of the console command");
			entity.Add("Description", cmd.Description, description: "Description to display in console help");
#if DEVELOPMENT
			entity.Add("EnabledInRelease", cmd.EnabledInRelease);
#endif
		}
	}
}
