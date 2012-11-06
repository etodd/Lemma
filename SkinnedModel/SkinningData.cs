#region File Description
//-----------------------------------------------------------------------------
// SkinningData.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
#endregion

namespace SkinnedModel
{
	/// <summary>
	/// Combines all the data needed to render and animate a skinned object.
	/// This is typically stored in the Tag property of the Model being animated.
	/// </summary>
	public class SkinningData
	{
		/// <summary>
		/// Constructs a new skinning data object.
		/// </summary>
		public SkinningData(Dictionary<string, int> boneMap, Dictionary<string, Clip> clips,
							List<Matrix> bindPose, List<Matrix> inverseBindPose,
							List<int> skeletonHierarchy)
		{
			this.BoneMap = boneMap;
			this.Clips = clips;
			this.BindPose = bindPose;
			this.InverseBindPose = inverseBindPose;
			this.SkeletonHierarchy = skeletonHierarchy;
		}


		/// <summary>
		/// Private constructor for use by the XNB deserializer.
		/// </summary>
		private SkinningData()
		{
		}

		/// <summary>
		/// Map of bone names to indices.
		/// </summary>
		[ContentSerializer]
		public Dictionary<string, int> BoneMap { get; set; }

		/// <summary>
		/// Gets a collection of animation clips. These are stored by name in a
		/// dictionary, so there could for instance be clips for "Walk", "Run",
		/// "JumpReallyHigh", etc.
		/// </summary>
		[ContentSerializer]
		public Dictionary<string, Clip> Clips { get; set; }


		/// <summary>
		/// Bindpose matrices for each bone in the skeleton,
		/// relative to the parent bone.
		/// </summary>
		[ContentSerializer]
		public List<Matrix> BindPose { get; set; }


		/// <summary>
		/// Vertex to bonespace transforms for each bone in the skeleton.
		/// </summary>
		[ContentSerializer]
		public List<Matrix> InverseBindPose { get; set; }


		/// <summary>
		/// For each bone in the skeleton, stores the index of the parent bone.
		/// </summary>
		[ContentSerializer]
		public List<int> SkeletonHierarchy { get; set; }
	}
}
