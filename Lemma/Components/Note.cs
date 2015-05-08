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

		public static List<Note> Notes = new List<Note>();

		[XmlIgnore]
		public Command Collected = new Command();

		public static int UncollectedCount
		{
			get
			{
				return Note.Notes.Where(x => !x.IsCollected).Count();
			}
		}

		public override void Awake()
		{
			base.Awake();
			Note.Notes.Add(this);
			this.Add(new NotifyBinding(delegate()
			{
				if (this.IsCollected)
				{
					int notesCollected = Note.Notes.Where(x => x.IsCollected).Count();
					int total = Note.Notes.Count;

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
					SteamWorker.SetStat("stat_notes_read", playerData.Notes);
					if (SteamWorker.GetStat("stat_notes_read") == playerData.Notes)
						SteamWorker.IndicateAchievementProgress("cheevo_notes", (uint)playerData.Notes.Value, (uint)totalNotes);
				}
			}, this.IsCollected));
		}

		public override void delete()
		{
			base.delete();
			Note.Notes.Remove(this);
		}
	}
}