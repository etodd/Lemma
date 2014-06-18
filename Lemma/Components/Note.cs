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

		private static List<Note> notes = new List<Note>();

		[XmlIgnore]
		public Command Collected = new Command();

		public override void Awake()
		{
			base.Awake();
			Note.notes.Add(this);
			this.Add(new NotifyBinding(delegate()
			{
				if (this.IsCollected)
				{
					int notesCollected = Note.notes.Where(x => x.IsCollected).Count();
					int total = Note.notes.Count;

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

		public override void delete()
		{
			base.delete();
			Note.notes.Remove(this);
		}
	}
}
