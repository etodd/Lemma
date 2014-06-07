using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class AmbientSound : Component<Main>
	{
		public EditorProperty<string> Play = new EditorProperty<string>();
		public EditorProperty<string> Stop = new EditorProperty<string>();
		public EditorProperty<bool> SuspendByDistance = new EditorProperty<bool>();
		public EditorProperty<bool> Looping = new EditorProperty<bool> { Value = true };
		public Property<Vector3> Position = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();

			AkGameObjectTracker.Attach(this.Entity, this.Position);

			this.Entity.CannotSuspendByDistance = !this.SuspendByDistance;
			this.Entity.Add(new NotifyBinding(delegate()
			{
				this.Entity.CannotSuspendByDistance = !this.SuspendByDistance;
			}, this.SuspendByDistance));

			Action play = delegate()
			{
				if (!string.IsNullOrEmpty(this.Play) && this.Enabled && !this.Suspended && !this.main.EditorEnabled)
					AkSoundEngine.PostEvent(this.Play, this.Entity);
			};

			Action stop = delegate()
			{
				if (!string.IsNullOrEmpty(this.Stop))
					AkSoundEngine.PostEvent(this.Stop, this.Entity);
			};

			this.Add(new CommandBinding(this.OnEnabled, play));
			this.Add(new CommandBinding(this.OnResumed, () => this.Looping, play));
			this.Add(new CommandBinding(this.OnDisabled, stop));
			this.Add(new CommandBinding(this.OnSuspended, stop));

			if (this.Looping)
				play();
		}

		public override void delete()
		{
			base.delete();
			if (!string.IsNullOrEmpty(this.Stop))
				AkSoundEngine.PostEvent(this.Stop, this.Entity);
		}
	}
}
