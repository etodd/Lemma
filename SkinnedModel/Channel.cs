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
		public int CurrentKeyframeIndex { get; set; }

		[ContentSerializerIgnore]
		public int LastKeyframeIndex { get; set; }

		[ContentSerializerIgnore]
		public Matrix CurrentMatrix;

		[ContentSerializer]
		public int BoneIndex { get; set; }
	}
}
