#region File Description
//-----------------------------------------------------------------------------
// AnimationClip.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
#endregion

namespace SkinnedModel
{
	/// <summary>
	/// An animation clip is the runtime equivalent of the
	/// Microsoft.Xna.Framework.Content.Pipeline.Graphics.AnimationContent type.
	/// It holds all the keyframes needed to describe a single animation.
	/// </summary>
	public class Clip
	{
		/// <summary>
		/// Constructs a new animation clip object.
		/// </summary>
		public Clip(IEnumerable<Channel> channels)
		{
			this.Channels = new List<Channel>(channels);
			foreach (Channel channel in this.Channels)
			{
				TimeSpan offset = channel[0].Time;
				if (offset.TotalSeconds < 0)
				{
					offset = offset.Negate();
					foreach (Keyframe frame in channel)
					{
						frame.Time += offset;
						if (this.Duration < frame.Time)
							this.Duration = frame.Time;
					}
				}
				else
				{
					foreach (Keyframe frame in channel)
					{
						if (this.Duration < frame.Time)
							this.Duration = frame.Time;
					}
				}
			}
		}

		/// <summary>
		/// Private constructor for use by the XNB deserializer.
		/// </summary>
		private Clip()
		{
		}

		/// <summary>
		/// Gets the total length of the animation.
		/// </summary>
		[ContentSerializer]
		public TimeSpan Duration;

		[ContentSerializer]
		public string Name;

		[ContentSerializerIgnore]
		public int Priority;

		[ContentSerializerIgnore]
		public float BlendTime;
		[ContentSerializerIgnore]
		public float BlendTotalTime;

		[ContentSerializerIgnore]
		public float Speed = 1.0f;

		[ContentSerializerIgnore]
		public bool Loop;

		[ContentSerializerIgnore]
		public bool StopOnEnd = true;

		[ContentSerializerIgnore]
		public float Strength = 1.0f;

		public float TotalStrength
		{
			get
			{
				float blend = this.BlendTotalTime > 0.0f ? this.BlendTime / this.BlendTotalTime : 1.0f;
				if (this.Stopping)
					blend = 1.0f - blend;
				blend = MathHelper.Clamp(blend, 0.0f, 1.0f);
				return MathHelper.Clamp(this.Strength * blend, 0.0f, 1.0f);
			}
		}

		private List<Channel> channels;
		/// <summary>
		/// Gets a combined list containing all the keyframes for all bones,
		/// sorted by time.
		/// </summary>
		[ContentSerializer]
		public List<Channel> Channels
		{
			get
			{
				return this.channels;
			}
			set
			{
				this.channels = value;
				foreach (Channel channel in this.channels)
				{
					if (channel.Count > 1)
						channel.CurrentKeyframeIndex = 1;
					else
						channel.CurrentMatrix = channel[0].Transform;
				}
			}
		}

		[ContentSerializerIgnore]
		public bool Stopping;

		protected TimeSpan currentTime;
		[ContentSerializerIgnore]
		public TimeSpan CurrentTime
		{
			get { return this.currentTime; }
			set
			{
				if (this.Channels == null)
					return;

				TimeSpan time = value;
				if (this.Loop)
				{
					if (this.Duration.TotalSeconds > 0)
					{
						if (time > this.Duration)
						{
							foreach (Channel channel in this.Channels)
							{
								if (channel.Count > 1)
								{
									channel.LastKeyframeIndex = 0;
									channel.CurrentKeyframeIndex = 1;
								}
							}
						}
						while (time > this.Duration)
							time -= this.Duration;
					}
				}
				else
					time = time > this.Duration ? this.Duration : time;

				foreach (Channel channel in this.Channels)
				{
					if (channel.Count > 1)
					{
						int index = time > this.currentTime ? channel.CurrentKeyframeIndex : 0;

						while (index < channel.Count - 1 && channel[index].Time <= time)
							index++;

						channel.LastKeyframeIndex = Math.Max(0, index - 1);
						channel.CurrentKeyframeIndex = index;
					}
				}
				this.currentTime = time;

				foreach (Channel channel in this.Channels)
				{
					if (channel.Count > 1)
					{
						Keyframe lastKeyframe = channel[channel.LastKeyframeIndex];
						Keyframe currentKeyframe = channel[channel.CurrentKeyframeIndex];
						double lerp = (this.currentTime.TotalSeconds - lastKeyframe.Time.TotalSeconds) / (currentKeyframe.Time.TotalSeconds - lastKeyframe.Time.TotalSeconds);
						channel.CurrentMatrix = Matrix.Lerp(lastKeyframe.Transform, currentKeyframe.Transform, (float)Math.Min(lerp, 1.0));
					}
				}
			}
		}
	}
}
