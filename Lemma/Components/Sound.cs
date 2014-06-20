using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Sound : Component<Main>
	{
		public Property<string> PlayCue = new Property<string>();
		public Property<string> StopCue = new Property<string>();
		public Property<bool> Is3D = new Property<bool>();
		public Property<Vector3> Position = new Property<Vector3>();

		[XmlIgnore]
		public Command Play = new Command();

		public override void Awake()
		{
			base.Awake();

			AkGameObjectTracker.Attach(this.Entity, this.Position);

			this.Entity.CannotSuspendByDistance = !this.Is3D;
			this.Entity.Add(new NotifyBinding(delegate()
			{
				this.Entity.CannotSuspendByDistance = !this.Is3D;
			}, this.Is3D));

			this.Play.Action = this.play;
			this.Add(new CommandBinding(this.Disable, (Action)this.stop));
			this.Add(new CommandBinding(this.OnSuspended, (Action)this.stop));
		}

		public void EditorProperties()
		{
			this.Entity.Add("Play", this.Play);
			this.Entity.Add("PlayCue", this.PlayCue);
			this.Entity.Add("StopCue", this.StopCue);
			this.Entity.Add("Is3D", this.Is3D);
		}

		private void play()
		{
			if (!string.IsNullOrEmpty(this.PlayCue) && this.Enabled && !this.Suspended && !this.main.EditorEnabled)
				AkSoundEngine.PostEvent(this.PlayCue, this.Entity);
		}

		private void stop()
		{
			if (!string.IsNullOrEmpty(this.StopCue))
				AkSoundEngine.PostEvent(this.StopCue, this.Entity);
		}

		public override void delete()
		{
			base.delete();
			this.stop();
		}
	}
}
