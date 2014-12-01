using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class AnimatedModel : Model, IUpdateableComponent
	{
		// Information about the currently playing animation clips.
		[XmlIgnore]
		private List<SkinnedModel.Clip> CurrentClips = new List<SkinnedModel.Clip>();

		[XmlIgnore]
		public Dictionary<string, SkinnedModel.Clip> Clips
		{
			get
			{
				return this.skinningData.Clips;
			}
		}

		// Animation blending data

		// Current animation transform matrices.
		protected Matrix[] boneTransforms;
		protected Matrix[] worldTransforms;
		protected Matrix[] skinTransforms;

		public bool bound = false;

		public const float DefaultBlendTime = 0.25f;

		// Backlink to the bind pose and skeleton hierarchy data.
		private SkinnedModel.SkinningData skinningData;

		public bool IsPlaying(params string[] clipNames)
		{
			if (clipNames.Length == 0)
				return this.CurrentClips.Count > 0;
			foreach (string clipName in clipNames)
			{
				SkinnedModel.Clip clip;
				if (this.skinningData.Clips.TryGetValue(clipName, out clip))
				{
					if (clip.Active && !clip.Stopping)
						return true;
				}
			}
			return false;
		}

		private class Callback
		{
			public float Offset;
			public Command Command;
		}

		private Dictionary<string, List<Callback>> callbacks = new Dictionary<string,List<Callback>>();

		public void StartClip(string clipName, int priority = 0, bool loop = false, float blendTime = AnimatedModel.DefaultBlendTime)
		{
			SkinnedModel.Clip clip = this.skinningData.Clips[clipName];

			if (clip.Stopping)
				clip.BlendTime = Math.Max(clip.BlendTotalTime - clip.BlendTime, 0);
			else
				clip.BlendTime = 0.0f;

			clip.BlendTotalTime = blendTime;

			clip.Priority = priority;
			clip.CurrentTime = TimeSpan.Zero;
			clip.Loop = loop;
			clip.Stopping = false;
			clip.Active = true;

			if (!this.CurrentClips.Contains(clip))
				this.CurrentClips.Add(clip);

			this.CurrentClips.Sort(new Util.LambdaComparer<SkinnedModel.Clip>((x, y) => x.Priority - y.Priority));
		}

		public void Stop(params string[] clips)
		{
			if (clips.Length == 0)
			{
				foreach (SkinnedModel.Clip clip in this.CurrentClips)
				{
					if (!clip.Stopping)
					{
						clip.Stopping = true;
						clip.BlendTime = Math.Max(clip.BlendTotalTime - clip.BlendTime, 0.0f);
					}
				}
			}
			else
			{
				foreach (string clipName in clips)
				{
					SkinnedModel.Clip clip;
					if (this.skinningData.Clips.TryGetValue(clipName, out clip))
					{
						if (clip.Active && !clip.Stopping)
						{
							clip.Stopping = true;
							clip.BlendTime = Math.Max(clip.BlendTotalTime - clip.BlendTime, 0.0f);
						}
					}
				}
			}
		}

		public void Trigger(string clip, float offset, Command callback)
		{
			List<Callback> list;
			if (!this.callbacks.TryGetValue(clip, out list))
				list = this.callbacks[clip] = new List<Callback>();
			list.Add(new Callback { Offset = offset, Command = callback });
		}

		public void Bind(AnimatedModel target)
		{
			this.boneTransforms = target.boneTransforms;
			this.worldTransforms = target.worldTransforms;
			this.skinTransforms = target.skinTransforms;
			this.bound = true;
		}

		public SkinnedModel.Clip this[string name]
		{
			get
			{
				return this.skinningData.Clips[name];
			}
		}

		/// <summary>
		/// Helper used by the Update method to refresh the BoneTransforms data.
		/// </summary>
		public void UpdateBoneTransforms(TimeSpan elapsedTime)
		{
			int i = 0;
			foreach (Matrix bone in this.skinningData.BindPose)
			{
				this.boneTransforms[i] = bone;
				i++;
			}

			for (i = 0; i < this.CurrentClips.Count; i++)
			{
				SkinnedModel.Clip clip = this.CurrentClips[i];
				TimeSpan newTime = clip.CurrentTime + new TimeSpan((long)((float)elapsedTime.Ticks * clip.Speed));

				List<Callback> callbacks;
				if (this.callbacks.TryGetValue(clip.Name, out callbacks))
				{
					float currentSeconds = (float)clip.CurrentTime.TotalSeconds;
					float newSeconds = (float)newTime.TotalSeconds;
					foreach (Callback c in callbacks)
					{
						if (currentSeconds < c.Offset && newSeconds > c.Offset)
							c.Command.Execute();
					}
				}

				float targetStrength = MathHelper.Clamp(clip.TargetStrength, 0.0f, 1.0f);
				float strengthBlendSpeed = (1.0f / AnimatedModel.DefaultBlendTime) * (float)elapsedTime.TotalSeconds;
				clip.Strength += MathHelper.Clamp(targetStrength - clip.Strength, -strengthBlendSpeed, strengthBlendSpeed);

				if (!clip.Stopping && clip.Duration.TotalSeconds > 0)
				{
					if (!clip.Loop && newTime >= clip.Duration)
					{
						clip.Stopping = true;
						clip.BlendTime = 0.0f;
					}
					else
						clip.CurrentTime = newTime;
				}

				clip.BlendTime += (float)elapsedTime.TotalSeconds;
				float blend = clip.Strength;
				if (clip.BlendTime < clip.BlendTotalTime)
				{
					float b = clip.BlendTime / clip.BlendTotalTime;

					b = -b * (b - 2); // Quadratic easing

					if (clip.Stopping)
						blend *= 1.0f - b;
					else
						blend *= b;
				}
				else if (clip.Stopping)
				{
					clip.Active = false;
					this.CurrentClips.RemoveAt(i);
					i--;
					continue;
				}

				if (blend > 0.0f)
				{
					if (blend < 1.0f)
					{
						foreach (SkinnedModel.Channel channel in clip.Channels)
						{
							Matrix bone1 = this.boneTransforms[channel.BoneIndex];
							Vector3 scale1;
							Quaternion quat1;
							Vector3 translation1;
							bone1.Decompose(out scale1, out quat1, out translation1);

							Matrix bone2 = channel.CurrentMatrix;
							Vector3 scale2;
							Quaternion quat2;
							Vector3 translation2;
							bone2.Decompose(out scale2, out quat2, out translation2);

							this.boneTransforms[channel.BoneIndex] = Matrix.CreateScale(Vector3.Lerp(scale1, scale2, blend)) * Matrix.CreateFromQuaternion(Quaternion.Lerp(quat1, quat2, blend)) * Matrix.CreateTranslation(Vector3.Lerp(translation1, translation2, blend));
						}
					}
					else
					{
						foreach (SkinnedModel.Channel channel in clip.Channels)
							this.boneTransforms[channel.BoneIndex] = channel.CurrentMatrix;
					}
				}
			}

			foreach (KeyValuePair<int, Property<Matrix>> pair in this.relativeBoneTransformProperties)
				pair.Value.Value = this.boneTransforms[pair.Key];
		}

		/// <summary>
		/// Helper used by the Update method to refresh the WorldTransforms data.
		/// </summary>
		public void UpdateWorldTransforms()
		{
			// Child bones.
			for (int bone = 0; bone < this.worldTransforms.Length; bone++)
			{
				int parentBone = this.skinningData.SkeletonHierarchy[bone];
				if (parentBone == -1)
					this.worldTransforms[bone] = this.boneTransforms[bone];
				else
					this.worldTransforms[bone] = this.boneTransforms[bone] * this.worldTransforms[parentBone];
				this.skinTransforms[bone] = this.skinningData.InverseBindPose[bone] * this.worldTransforms[bone];
			}

			foreach (KeyValuePair<int, Property<Matrix>> pair in this.boneTransformProperties)
				pair.Value.Value = this.worldTransforms[pair.Key];
		}

		private Dictionary<int, Property<Matrix>> boneTransformProperties = new Dictionary<int, Property<Matrix>>();
		private Dictionary<int, Property<Matrix>> absoluteBoneTransformProperties = new Dictionary<int, Property<Matrix>>();
		private Dictionary<int, Property<Matrix>> relativeBoneTransformProperties = new Dictionary<int, Property<Matrix>>();

		public AnimatedModel()
		{
			this.EnabledWhenPaused = true;
		}

		public Property<Matrix> GetRelativeBoneTransform(string bone)
		{
			Property<Matrix> property = null;
			int index = this.skinningData.BoneMap[bone];
			if (this.relativeBoneTransformProperties.TryGetValue(index, out property))
				return property;
			else
			{
				property = new Property<Matrix>();
				this.relativeBoneTransformProperties[index] = property;
				this.Add(new SetBinding<Matrix>(property, delegate(Matrix value)
				{
					this.boneTransforms[index] = value;
				}));
				return property;
			}
		}

		public Property<Matrix> GetBoneTransform(string bone)
		{
			Property<Matrix> property = null;
			int index = this.skinningData.BoneMap[bone];
			if (this.boneTransformProperties.TryGetValue(index, out property))
				return property;
			else
			{
				property = new Property<Matrix>();
				this.boneTransformProperties[index] = property;
				this.Add(new SetBinding<Matrix>(property, delegate(Matrix value)
				{
					this.worldTransforms[index] = value;
				}));
				return property;
			}
		}

		public int GetBoneIndex(string bone)
		{
			return this.skinningData.BoneMap[bone];
		}

		public Property<Matrix> GetWorldBoneTransform(string bone)
		{
			Property<Matrix> property = null;
			int index = this.skinningData.BoneMap[bone];
			if (this.absoluteBoneTransformProperties.TryGetValue(index, out property))
				return property;
			else
			{
				property = new Property<Matrix>();
				this.absoluteBoneTransformProperties[index] = property;

				Property<Matrix> relativeTransformProperty = this.GetBoneTransform(bone);

				this.Add(new Binding<Matrix>(property, () => relativeTransformProperty.Value * this.Transform.Value, this.Transform, relativeTransformProperty));
				return property;
			}
		}

		protected override void loadModel(string file, bool reload)
		{
			base.loadModel(file, reload);
			if (this.model != null && (file != this.Filename.Value || this.skinningData == null))
			{
				// Look up our custom skinning information.
				this.skinningData = this.model.Tag as SkinnedModel.SkinningData;

				if (this.skinningData == null)
					throw new InvalidOperationException("This model does not contain a SkinningData tag.");

				this.boneTransforms = new Matrix[this.skinningData.BindPose.Count];
				this.skinningData.BindPose.CopyTo(this.boneTransforms, 0);
				this.worldTransforms = new Matrix[this.skinningData.BindPose.Count];
				this.skinTransforms = new Matrix[this.skinningData.BindPose.Count];
			}
		}

		public void Update(float elapsedTime)
		{
			if (this.skinningData != null && !this.bound && !this.main.Paused)
			{
				this.UpdateBoneTransforms(new TimeSpan((long)(elapsedTime * TimeSpan.TicksPerSecond)));
				this.UpdateWorldTransforms();
			}
		}

		protected override void drawInstances(RenderParameters parameters, Matrix transform)
		{
			// Animated instancing not supported
		}

		protected override bool setParameters(Matrix transform, RenderParameters parameters)
		{
			bool result = base.setParameters(transform, parameters);
			if (result)
				this.effect.Parameters["Bones"].SetValue(this.skinTransforms);
			return result;
		}
	}
}
