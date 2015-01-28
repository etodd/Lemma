using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class AnimatedProp : Component<Main>
	{
		public Property<string> Clip = new Property<string>();
		public Property<bool> Loop = new Property<bool>();
		private AnimatedModel model;

		public override void Awake()
		{
			base.Awake();

			this.model = this.Entity.Get<AnimatedModel>();

			this.Add(new ChangeBinding<string>(this.Clip, delegate(string old, string value)
			{
				if (old != value)
				{
					if (!string.IsNullOrEmpty(old))
						this.model.Stop(old);
					this.play();
				}
			}));
			this.Add(new ChangeBinding<bool>(this.Loop, delegate(bool old, bool value)
			{
				if (!string.IsNullOrEmpty(this.Clip) && this.model.Clips.ContainsKey(this.Clip))
					this.model[this.Clip].Loop = value;
			}));
			this.Add(new CommandBinding(this.Enable, (Action)this.play));
			this.Add(new CommandBinding(this.Disable, (Action)this.stop));
		}

		public override void Start()
		{
			if (this.Enabled)
				this.play();
		}

		private void play()
		{
			if (!string.IsNullOrEmpty(this.Clip) && this.Enabled && !this.model.IsPlaying(this.Clip) && this.model.Clips.ContainsKey(this.Clip))
				this.model.StartClip(this.Clip, 0, this.Loop);
		}

		private void stop()
		{
			if (!string.IsNullOrEmpty(this.Clip))
				this.model.Stop(this.Clip);
		}
	}
}
