using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class AmbientSound : Component<Main>
	{
		public Property<uint> PlayCue = new Property<uint>();
		public Property<uint> StopCue = new Property<uint>();

		public Property<bool> Is3D = new Property<bool>();
		public Property<Vector3> Position = new Property<Vector3>();

		private bool playing;

		public override void Awake()
		{
			base.Awake();

			Sound.AttachTracker(this.Entity, this.Position);

			this.Entity.CannotSuspendByDistance = !this.Is3D;
			this.Entity.Add(new NotifyBinding(delegate()
			{
				this.Entity.CannotSuspendByDistance = !this.Is3D;
			}, this.Is3D));

			this.Add(new CommandBinding(this.Enable, (Action)this.play));
			this.Add(new CommandBinding(this.OnResumed, (Action)this.play));
			this.Add(new CommandBinding(this.Disable, (Action)this.stop));
			this.Add(new CommandBinding(this.OnSuspended, (Action)this.stop));
		}

		public override void Start()
		{
			if (this.Enabled)
				this.play();
		}

		private void play()
		{
			if (this.PlayCue != 0 && !this.Suspended && !this.main.EditorEnabled && !this.playing)
			{
				AkSoundEngine.PostEvent(this.PlayCue, this.Entity);
				this.playing = true;
			}
		}

		private void stop()
		{
			if (this.StopCue != 0)
				AkSoundEngine.PostEvent(this.StopCue, this.Entity);
			this.playing = false;
		}

		public override void delete()
		{
			base.delete();
			this.stop();
		}
	}
}