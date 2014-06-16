using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Note : Component<Main>
	{
		public Property<string> Text = new Property<string>();
		public Property<string> Image = new Property<string>();
		public Property<bool> IsCollected = new Property<bool>();

		[XmlIgnore]
		public Command Collected = new Command();

		public override void Awake()
		{
			base.Awake();
			List<Entity> notes = this.main.Get("Note").ToList();
			int notesCollected = notes.Where(x => x.Get<Note>().IsCollected).Count();
			int total = notes.Count;

			this.Add(new NotifyBinding(delegate()
			{
				if (this.IsCollected)
				{
					Container msg = this.main.Menu.ShowMessageFormat
					(
						this.Entity,
						"\\notes read",
						notesCollected, total
					);
					this.main.Menu.HideMessage(this.Entity, msg, 4.0f);
					this.Collected.Execute();
				}
			}, this.IsCollected));
		}
	}
}
