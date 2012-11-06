using System;
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
		// Information about the currently playing animation clip.
		[XmlIgnore]
		public List<SkinnedModel.Clip> CurrentClips = new List<SkinnedModel.Clip>();

		// Animation blending data

		// Current animation transform matrices.
		protected Matrix[] boneTransforms;
		protected Matrix[] worldTransforms;
		protected Matrix[] skinTransforms;

		public bool bound = false;

		public const float DefaultBlendTime = 0.25f;

		private static Matrix rotation = Matrix.CreateRotationX((float)Math.PI * -0.5f);

		// Backlink to the bind pose and skeleton hierarchy data.
		private SkinnedModel.SkinningData skinningData;

		public bool IsPlaying(string clipName = null)
		{
			if (clipName == null)
				return this.CurrentClips.Count > 0;
			return this.CurrentClips.FirstOrDefault(x => x.Name == clipName) != null;
		}

		public void Bind(AnimatedModel target)
		{
			this.boneTransforms = target.boneTransforms;
			this.worldTransforms = target.worldTransforms;
			this.skinTransforms = target.skinTransforms;
			this.bound = true;
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
						clip.BlendTime = 0.0f;
					}
				}
			}
			else
			{
				foreach (SkinnedModel.Clip clip in this.CurrentClips)
				{
					if (clips.Contains(clip.Name) && !clip.Stopping)
					{
						clip.Stopping = true;
						clip.BlendTime = Math.Max(clip.BlendTotalTime - clip.BlendTime, 0.0f);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Starts decoding the specified animation clip.
		/// </summary>
		public void StartClip(string clipName, int priority = 0, bool loop = false, float blendTime = AnimatedModel.DefaultBlendTime)
		{
			SkinnedModel.Clip clip = this.skinningData.Clips[clipName];

			clip.Priority = priority;
			clip.CurrentTime = TimeSpan.Zero;
			clip.BlendTime = 0.0f;
			clip.BlendTotalTime = blendTime;
			clip.Loop = loop;
			clip.Stopping = false;

			if (!this.CurrentClips.Contains(clip))
				this.CurrentClips.Add(clip);

			this.CurrentClips.Sort(new Util.LambdaComparer<SkinnedModel.Clip>((x, y) => x.Priority - y.Priority));
		}

		/// <summary>
		/// Helper used by the Update method to refresh the BoneTransforms data.
		/// </summary>
		public void UpdateBoneTransforms(TimeSpan elapsedTime)
		{
			TimeSpan deltaTime = new TimeSpan((long)((float)elapsedTime.Ticks * this.Speed));

			List<SkinnedModel.Clip> removals = new List<SkinnedModel.Clip>();

			int i = 0;
			foreach (Matrix bone in this.skinningData.BindPose)
			{
				this.boneTransforms[i] = bone;
				i++;
			}

			foreach (SkinnedModel.Clip clip in this.CurrentClips)
			{
				TimeSpan newTime = clip.CurrentTime + deltaTime;

				if (!clip.Stopping && clip.Duration.TotalSeconds > 0)
				{
					if (newTime >= clip.Duration && !clip.Loop)
					{
						clip.Stopping = true;
						clip.BlendTime = 0.0f;
					}
					else
						clip.CurrentTime = newTime;
				}

				if (clip.BlendTime < clip.BlendTotalTime)
				{
					float blend = clip.BlendTime / clip.BlendTotalTime;
					if (clip.Stopping)
						blend = 1.0f - blend;

					foreach (SkinnedModel.Channel channel in clip.Channels)
						this.boneTransforms[channel.BoneIndex] = Matrix.Lerp(this.boneTransforms[channel.BoneIndex], channel.CurrentMatrix, blend);
					
					clip.BlendTime += (float)elapsedTime.TotalSeconds;
				}
				else
				{
					if (clip.Stopping)
					{
						clip.Stopping = false;
						removals.Add(clip);
					}
					else
					{
						foreach (SkinnedModel.Channel channel in clip.Channels)
							this.boneTransforms[channel.BoneIndex] = channel.CurrentMatrix;
					}
				}
			}

			foreach (SkinnedModel.Clip clip in removals)
				this.CurrentClips.Remove(clip);

			foreach (KeyValuePair<int, Property<Matrix>> pair in this.relativeBoneTransformProperties)
				pair.Value.Changed();
		}

		/// <summary>
		/// Helper used by the Update method to refresh the WorldTransforms data.
		/// </summary>
		public void UpdateWorldTransforms()
		{
			// Root bone.
			this.worldTransforms[0] = AnimatedModel.rotation * this.boneTransforms[0];

			// Child bones.
			for (int bone = 1; bone < this.worldTransforms.Length; bone++)
			{
				int parentBone = this.skinningData.SkeletonHierarchy[bone];
				this.worldTransforms[bone] = this.boneTransforms[bone] * this.worldTransforms[parentBone];
				this.skinTransforms[bone] = this.skinningData.InverseBindPose[bone] * this.worldTransforms[bone];
			}

			foreach (KeyValuePair<int, Property<Matrix>> pair in this.boneTransformProperties)
				pair.Value.Changed();
		}

		private Dictionary<int, Property<Matrix>> boneTransformProperties = new Dictionary<int, Property<Matrix>>();
		private Dictionary<int, Property<Matrix>> absoluteBoneTransformProperties = new Dictionary<int, Property<Matrix>>();
		private Dictionary<int, Property<Matrix>> relativeBoneTransformProperties = new Dictionary<int, Property<Matrix>>();

		public Property<float> Speed = new Property<float> { Editable = true, Value = 1.0f };

		public Property<float> RemainingBlendTime = new Property<float> { Editable = false };
		
		public AnimatedModel()
		{
			this.EnabledWhenPaused.Value = false;
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
				property.Get = delegate()
				{
					return this.boneTransforms[index];
				};
				property.Set = delegate(Matrix value)
				{
					this.boneTransforms[index] = value;
				};
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
				property.Get = delegate()
				{
					return this.worldTransforms[index];
				};
				property.Set = delegate(Matrix value)
				{
					this.worldTransforms[index] = value;
				};
				return property;
			}
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
			if (this.skinningData != null && !this.bound)
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
