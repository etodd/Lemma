using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Factories;
using ComponentBind;
using System.Xml.Serialization;
using Lemma.Console;

namespace Lemma.Components
{
	public class ConsoleCommand : Component<Main>
	{
		public Property<string> Name = new Property<string>();
		public Property<string> Description = new Property<string>();
		public Property<bool> EnabledInRelease = new Property<bool>();

		[XmlIgnore]
		public Command Execute = new Command();

		public override void Awake()
		{
			base.Awake();
#if !DEVELOPMENT
			if (!this.EnabledInRelease)
				this.Delete.Execute();
			else
			{
#endif
				Lemma.Console.Console.AddConCommand(new ConCommand(this.Name, this.Description, collection =>
				{
					if (this.Active)
						this.Execute.Execute();
				}));
#if !DEVELOPMENT
			}
#endif
		}

		public override void delete()
		{
			base.delete();
#if !DEVELOPMENT
			if (this.EnabledInRelease)
#endif
				Lemma.Console.Console.RemoveConCommand(this.Name);
		}
	}
}