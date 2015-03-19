using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;

namespace Lemma.Components
{
	public class Note : Component<Main>
	{
		public Property<string> Text = new Property<string>();
		public Property<string> Image = new Property<string>();
		public Property<bool> IsCollected = new Property<bool>();

		const int totalNotes = 36;

		private static List<Note> notes = new List<Note>();

		[XmlIgnore]
		public Command Collected = new Command();

		public static int UncollectedCount
		{
			get
			{
				return Note.notes.Where(x => !x.IsCollected).Count();
			}
		}

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
					PlayerData playerData = PlayerDataFactory.Instance.Get<PlayerData>();
					playerData.Notes.Value++;
					if (playerData.Notes >= totalNotes)
						SteamWorker.SetAchievement("cheevo_notes");
					SteamWorker.IncrementStat("notes_read", 1);
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
