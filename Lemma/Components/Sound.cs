using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using System.Xml.Serialization;
using Lemma.Factories;

namespace Lemma.Components
{
	public class Sound : Component, IUpdateableComponent
	{
		protected Cue cue;

		protected Dictionary<string, Property<float>> variables = new Dictionary<string, Property<float>>();

		public Property<string> Cue = new Property<string> { Editable = true };

		public Property<Vector3> Position = new Property<Vector3> { Editable = false };

		public Property<Vector3> Velocity = new Property<Vector3> { Editable = false };

		public Property<bool> Is3D = new Property<bool> { Editable = true };

		public Property<bool> IsPlaying = new Property<bool> { Editable = true };

		public Property<bool> DeleteWhenDone = new Property<bool> { Editable = true };

		public Property<AudioStopOptions> DeleteStopOption = new Property<AudioStopOptions> { Editable = true };

		protected AudioEmitter emitter;

		[XmlIgnore]
		public Command Play = new Command();

		[XmlIgnore]
		public Command<AudioStopOptions> Stop = new Command<AudioStopOptions>();

		public Sound()
		{
			this.Is3D.Value = true;
		}

		public static void ReverbSettings(Main main, float amount, float size)
		{
			main.AudioEngine.SetGlobalVariable("ReverbAmount", amount);
			main.AudioEngine.SetGlobalVariable("ReverbSize", size);
		}

		private static Dictionary<string, float> lastSoundPlayedTimes = new Dictionary<string, float>();

		public static Sound PlayCue(Main main, string cue, float volume = 1.0f, float minimumTimeBetweenSounds = 0.25f)
		{
			float lastSoundPlayedTime = minimumTimeBetweenSounds * -2.0f;
			Sound.lastSoundPlayedTimes.TryGetValue(cue, out lastSoundPlayedTime);
			float time = main.TotalTime;
			if (time > lastSoundPlayedTime + minimumTimeBetweenSounds)
			{
				Sound.lastSoundPlayedTimes[cue] = time;
				Sound sound = new Sound();
				sound.Cue.Value = cue;
				sound.Is3D.Value = false;
				if (volume != 1.0f)
					sound.GetProperty("Volume").Value = volume;
				sound.DeleteWhenDone.Value = true;
				sound.DeleteStopOption.Value = AudioStopOptions.AsAuthored;
				main.AddComponent(sound);
				sound.Play.Execute();
				return sound;
			}
			return null;
		}

		public static Sound PlayCue(Main main, string cue, Vector3 position, float volume = 1.0f, float minimumTimeBetweenSounds = 0.25f)
		{
			float lastSoundPlayedTime = minimumTimeBetweenSounds * -2.0f;
			Sound.lastSoundPlayedTimes.TryGetValue(cue, out lastSoundPlayedTime);
			float time = main.TotalTime;
			if (time > lastSoundPlayedTime + minimumTimeBetweenSounds)
			{
				Sound.lastSoundPlayedTimes[cue] = time;
				Sound sound = new Sound();
				sound.Cue.Value = cue;
				sound.Position.Value = position;
				if (volume != 1.0f)
					sound.GetProperty("Volume").Value = volume;
				sound.DeleteWhenDone.Value = true;
				sound.DeleteStopOption.Value = AudioStopOptions.AsAuthored;
				main.AddComponent(sound);
				sound.Play.Execute();
				return sound;
			}
			return null;
		}

		private bool playWhenResumed = false;

		public override void InitializeProperties()
		{
			this.Position.Set =
				delegate(Vector3 value)
				{
					this.Position.InternalValue = value;
					if (this.emitter != null)
						this.emitter.Position = value;
				};

			this.Velocity.Set =
				delegate(Vector3 value)
				{
					this.Velocity.InternalValue = value;
					if (this.emitter != null)
						this.emitter.Velocity = value;
				};

			this.Is3D.Set = delegate(bool value)
			{
				bool oldValue = this.Is3D.InternalValue;
				this.Is3D.InternalValue = value;
				if (value != oldValue && this.cue != null && this.cue.IsPlaying)
				{
					this.Stop.Execute(AudioStopOptions.Immediate);
					this.Play.Execute();
				}
			};

			this.Play.Action =
				delegate()
				{
					try
					{
						if (!this.Suspended && !this.main.EditorEnabled)
						{
							this.cue = this.main.SoundBank.GetCue(this.Cue);
							
							if (this.Is3D)
							{
								this.emitter = new AudioEmitter { Position = this.Position, Velocity = this.Velocity };
								AudioListener.Apply3D(this.cue, this.emitter);
							}
							this.cue.Play();
						}
					}
					catch (ArgumentException)
					{
						this.cue = null;
					}
				};

			this.Stop.Action =
				delegate(AudioStopOptions options)
				{
					if (this.cue != null)
						this.cue.Stop(options);
				};

			this.IsPlaying.Get =
				delegate()
				{
					if (this.main.EditorEnabled)
						return this.IsPlaying.InternalValue;
					else
						return this.cue != null ? this.cue.IsPlaying : false;
				};
			this.IsPlaying.Set =
				delegate(bool value)
				{
					if (this.Suspended)
						this.playWhenResumed = true;
					this.IsPlaying.InternalValue = value;
					if (value && !this.IsPlaying)
						this.Play.Execute();
					else if (!value && this.IsPlaying)
						this.Stop.Execute(AudioStopOptions.Immediate);
				};
			this.Add(new NotifyBinding(delegate()
			{
				if (this.cue != null)
				{
					if (this.Suspended)
					{
						this.playWhenResumed = this.IsPlaying;
						this.Stop.Execute(AudioStopOptions.AsAuthored);
					}
					else if (this.playWhenResumed)
						this.Play.Execute();
				}
			}, this.Suspended));
		}

		public Property<float> GetProperty(string name)
		{
			Property<float> result = null;
			if (!this.variables.TryGetValue(name, out result))
			{
				result = new Property<float>
				{
					Get = delegate()
					{
#if MONOGAME
						return this.cue != null ? this.cue.GetVariable(name, 0.0f) : 0.0f;
#else
						return this.cue != null ? this.cue.GetVariable(name) : 0.0f;
#endif
					},
					Set = delegate(float value)
					{
						result.InternalValue = value;
						if (this.cue != null)
							this.cue.SetVariable(name, value);
					}
				};
				this.variables.Add(name, result);
			}
			return result;
		}

		void IUpdateableComponent.Update(float dt)
		{
			if (this.cue != null)
			{
				if (this.cue.IsPlaying)
				{
					if (this.Is3D)
						AudioListener.Apply3D(this.cue, this.emitter);
				}
				else if (this.DeleteWhenDone)
					this.Delete.Execute();
			}
		}

		protected override void delete()
		{
			base.delete();
			if (this.cue != null && !this.cue.IsDisposed)
			{
				if (this.cue.IsPlaying)
					this.cue.Stop(this.DeleteStopOption);
				this.cue = null;
			}
		}
	}
}
