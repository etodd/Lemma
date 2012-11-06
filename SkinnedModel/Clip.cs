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
		public Clip(TimeSpan duration, IEnumerable<Channel> channels)
		{
			this.Duration = duration;
			this.Channels = new List<Channel>(channels);
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
		public TimeSpan Duration { get; set; }

		[ContentSerializer]
		public string Name { get; set; }

		[ContentSerializerIgnore]
		public int Priority;

		[ContentSerializerIgnore]
		public float BlendTime;
		[ContentSerializerIgnore]
		public float BlendTotalTime;

		[ContentSerializerIgnore]
		public bool Loop;

		/// <summary>
		/// Gets a combined list containing all the keyframes for all bones,
		/// sorted by time.
		/// </summary>
		[ContentSerializer]
		public List<Channel> Channels { get; set; }

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
				if (this.Duration.TotalSeconds > 0)
				{
					while (time > this.Duration)
						time -= this.Duration;
				}

				foreach (Channel channel in this.Channels)
				{
					if (channel == null || channel.Count == 1)
						continue;
					int index = time > this.currentTime ? channel.CurrentKeyframeIndex : Math.Min(channel.Count - 1, 1);
					while (channel[index].Time < time)
					{
						index++;
						if (index >= channel.Count)
						{
							index = channel.Count - 1;
							break;
						}
					}
					if (index != channel.CurrentKeyframeIndex)
					{
						channel.LastKeyframeIndex = channel.CurrentKeyframeIndex;
						channel.CurrentKeyframeIndex = index;
					}
				}
				this.currentTime = time;

				foreach (Channel channel in this.Channels)
				{
					Keyframe lastKeyframe = channel.LastKeyframeIndex < channel.Count ? channel[channel.LastKeyframeIndex] : null;
					Keyframe currentKeyframe = channel[channel.CurrentKeyframeIndex];
					if (lastKeyframe == null)
						channel.CurrentMatrix = currentKeyframe.Transform;
					else
					{
						double lerp = (this.CurrentTime.TotalSeconds - lastKeyframe.Time.TotalSeconds) / (currentKeyframe.Time.TotalSeconds - lastKeyframe.Time.TotalSeconds);
						channel.CurrentMatrix = Matrix.Lerp(lastKeyframe.Transform, currentKeyframe.Transform, (float)Math.Min(lerp, 1.0));
					}
				}
			}
		}
	}
}
