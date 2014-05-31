using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace SkinnedModel
{
	public class Channel : List<Keyframe>
	{
		public Channel()
		{
			this.CurrentKeyframeIndex = 0;
		}

		[ContentSerializerIgnore]
		public int CurrentKeyframeIndex;

		[ContentSerializerIgnore]
		public int LastKeyframeIndex;

		[ContentSerializerIgnore]
		public Matrix CurrentMatrix;

		[ContentSerializerIgnore]
		public Func<Matrix, Matrix> Filter;

		[ContentSerializer]
		public int BoneIndex;
	}
}
