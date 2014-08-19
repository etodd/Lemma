#region File Description
//-----------------------------------------------------------------------------
// ParticleEmitter.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System; using ComponentBind;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.Collections.Generic;
#endregion

namespace Lemma.Components
{
	public class ParticleEmitter : Component<Main>, IUpdateableComponent
	{
		protected float timeBetweenParticles = 0.1f;

		private ParticleSystem particleSystem;
		public ParticleSystem ParticleSystem
		{
			get
			{
				return this.particleSystem;
			}
		}
		public Property<string> ParticleType = new Property<string>();
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<Vector3> Jitter = new Property<Vector3>();
		protected bool lastPositionSet = false;
		protected Vector3 lastPosition;
		public Property<int> ParticlesPerSecond = new Property<int> { Value = 10 };
		[XmlIgnore]
		public Action<Vector3, Vector3, float> AddParticle;

		private static Random random = new Random();

		public ParticleEmitter()
		{
			this.AddParticle = delegate(Vector3 position, Vector3 velocity, float prime)
			{
				this.particleSystem.AddParticle(position, velocity, -1.0f, -1.0f, prime);
			};
		}

		private static ListProperty<string> particleTypes;
		public void EditorProperties()
		{
			if (this.main.EditorEnabled && ParticleEmitter.particleTypes == null)
			{
				ParticleEmitter.particleTypes = new ListProperty<string>();
				ParticleEmitter.particleTypes.AddAll(ParticleSystem.Types);
			}
			this.Entity.Add("ParticleType", this.ParticleType, new PropertyEntry.EditorData
			{
				Options = ParticleEmitter.particleTypes,
			});
			this.Entity.Add("Jitter", this.Jitter);
			this.Entity.Add("ParticlesPerSecond", this.ParticlesPerSecond);
		}

		public override void Awake()
		{
			base.Awake();
			this.EnabledWhenPaused = false;

			this.ParticlesPerSecond.Set = delegate(int value)
			{
				this.ParticlesPerSecond.InternalValue = value;
				this.timeBetweenParticles = value == 0 ? 0.0f : 1.0f / value;
			};

			this.ParticleType.Set = delegate(string value)
			{
				this.ParticleType.InternalValue = value;
				this.particleSystem = value == null ? null : ParticleSystem.Get(this.main, value);
			};
		}

		public static void Emit(Main main, string type, Vector3 position, float jitter, int amount)
		{
			ParticleSystem particleSystem = ParticleSystem.Get(main, type);
			for (int i = 0; i < amount; i++)
				particleSystem.AddParticle(position + new Vector3(2.0f * ((float)random.NextDouble() - 0.5f) * jitter, 2.0f * ((float)random.NextDouble() - 0.5f) * jitter, 2.0f * ((float)random.NextDouble() - 0.5f) * jitter), Vector3.Zero);
		}

		public static void Emit(Main main, string type, IEnumerable<Vector3> positions)
		{
			ParticleSystem particleSystem = ParticleSystem.Get(main, type);
			foreach (Vector3 pos in positions)
				particleSystem.AddParticle(pos, Vector3.Zero);
		}

		public void Prime(Vector3 velocity)
		{
			this.ParticleSystem.Prime();
			float duration = (float)this.particleSystem.Settings.Value.Duration.TotalSeconds;
			int particles = Math.Min((int)(this.ParticlesPerSecond * duration), this.particleSystem.Settings.Value.MaxParticles);
			float interval = duration / particles;
			Vector3 jitter = this.Jitter;
			float prime = duration;
			for (int i = 0; i < particles; i++)
			{
				Vector3 position = this.Position + new Vector3(2.0f * ((float)random.NextDouble() - 0.5f) * jitter.X, 2.0f * ((float)random.NextDouble() - 0.5f) * jitter.Y, 2.0f * ((float)random.NextDouble() - 0.5f) * jitter.Z);

				// Create the particle.
				this.AddParticle(position, velocity, prime);
				prime -= interval;
			}
		}

		protected float timeLeftOver;

		/// <summary>
		/// Updates the emitter, creating the appropriate number of particles
		/// in the appropriate positions.
		/// </summary>
		public void Update(float elapsedTime)
		{
			if (elapsedTime == 0 || this.particleSystem == null || this.timeBetweenParticles == 0)
				return;

			// Set the initial "last position" so we don't add particles between (0, 0, 0) and our initial location
			if (!this.lastPositionSet)
			{
				this.lastPositionSet = true;
				this.lastPosition = this.Position;
			}

			// Work out how fast we are moving.
			Vector3 velocity = (this.Position.Value - this.lastPosition) / elapsedTime;

			// If we had any time left over that we didn't use during the
			// previous update, add that to the current elapsed time.
			float timeToSpend = this.timeLeftOver + elapsedTime;
				
			// Counter for looping over the time interval.
			float currentTime = -this.timeLeftOver;

			Vector3 jitter = this.Jitter;

			// Create particles as long as we have a big enough time interval.
			while (timeToSpend > this.timeBetweenParticles)
			{
				currentTime += this.timeBetweenParticles;
				timeToSpend -= this.timeBetweenParticles;

				// Work out the optimal position for this particle. This will produce
				// evenly spaced particles regardless of the object speed, particle
				// creation frequency, or game update rate.
				float mu = currentTime / elapsedTime;

				Vector3 position = Vector3.Lerp(this.lastPosition, this.Position, mu);
				position += new Vector3(2.0f * ((float)random.NextDouble() - 0.5f) * jitter.X, 2.0f * ((float)random.NextDouble() - 0.5f) * jitter.Y, 2.0f * ((float)random.NextDouble() - 0.5f) * jitter.Z);

				// Create the particle.
				this.AddParticle(position, velocity, 0);
			}

			// Store any time we didn't use, so it can be part of the next update.
			this.timeLeftOver = timeToSpend;
			this.lastPosition = this.Position;
		}
	}
}
