using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public override void Awake()
		{
			base.Awake();

			AkGameObjectTracker.Attach(this.Entity, this.Position);

			this.Entity.CannotSuspendByDistance = !this.Is3D;
			this.Entity.Add(new NotifyBinding(delegate()
			{
				this.Entity.CannotSuspendByDistance = !this.Is3D;
			}, this.Is3D));

			this.Add(new CommandBinding(this.Disable, (Action)this.Stop));
			this.Add(new CommandBinding(this.OnSuspended, (Action)this.Stop));
		}

		public void Play()
		{
			if (!string.IsNullOrEmpty(this.PlayCue) && this.Enabled && !this.Suspended && !this.main.EditorEnabled)
				AkSoundEngine.PostEvent(this.PlayCue, this.Entity);
		}

		public void Stop()
		{
			if (!string.IsNullOrEmpty(this.StopCue))
				AkSoundEngine.PostEvent(this.StopCue, this.Entity);
		}

		public override void delete()
		{
			base.delete();
			this.Stop();
		}
	}
}
