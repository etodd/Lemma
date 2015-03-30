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
			this.play();
		}

		private void play()
		{
			if (this.PlayCue != 0 && this.Enabled && !this.Suspended && !this.main.EditorEnabled)
				AkSoundEngine.PostEvent(this.PlayCue, this.Entity);
		}

		private void stop()
		{
			if (this.StopCue != 0)
				AkSoundEngine.PostEvent(this.StopCue, this.Entity);
		}

		public override void delete()
		{
			base.delete();
			this.stop();
		}
	}
}
