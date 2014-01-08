using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Lemma.Components
{
	public class AudioListener : Component
	{
		protected static List<AudioListener> listeners = new List<AudioListener>();

		public static void Apply3D(Cue cue, AudioEmitter emitter)
		{
			cue.Apply3D(AudioListener.GetClosestListener(emitter.Position), emitter);
		}

		public static void Apply3D(DynamicSoundEffectInstance instance, AudioEmitter emitter)
		{
			instance.Apply3D(AudioListener.GetClosestListener(emitter.Position), emitter);
		}

		public static Microsoft.Xna.Framework.Audio.AudioListener GetClosestListener(Vector3 pos)
		{
			AudioListener component;
			if (AudioListener.listeners.Count == 1)
				component = AudioListener.listeners[0];
			else
				component = AudioListener.listeners.OrderBy(x => (x.Position.Value - pos).LengthSquared()).FirstOrDefault();
			if (component != null)
			{
				component.Refresh();
				return component.listener;
			}
			else
				return new Microsoft.Xna.Framework.Audio.AudioListener { Position = Vector3.Zero, Forward = Vector3.Forward };
		}

		protected Microsoft.Xna.Framework.Audio.AudioListener listener = new Microsoft.Xna.Framework.Audio.AudioListener();
		public Property<Vector3> Position = new Property<Vector3> { Editable = false };
		public Property<Vector3> Forward = new Property<Vector3> { Editable = false };
		public Property<Vector3> Velocity = new Property<Vector3> { Editable = false };

		public void Refresh()
		{
			this.listener.Position = this.Position;
			this.listener.Forward = this.Forward;
			this.listener.Velocity = this.Velocity;
		}

		public AudioListener()
		{
			AudioListener.listeners.Add(this);
		}

		public override void InitializeProperties()
		{
			this.Position.Get =
				delegate()
				{
					return this.listener.Position;
				};
			this.Position.Set =
				delegate(Vector3 value)
				{
					this.listener.Position = value;
				};

			this.Forward.Get =
				delegate()
				{
					return this.listener.Forward;
				};
			this.Forward.Set =
				delegate(Vector3 value)
				{
					this.listener.Forward = value;
				};

			this.Velocity.Get =
				delegate()
				{
					return this.listener.Velocity;
				};
			this.Velocity.Set =
				delegate(Vector3 value)
				{
					this.listener.Velocity = value;
				};
		}

		protected override void delete()
		{
			base.delete();
			AudioListener.listeners.Remove(this);
		}
	}
}
