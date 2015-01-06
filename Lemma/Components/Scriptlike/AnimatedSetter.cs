using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class AnimatedSetter : Setter
	{
		public Property<Animation.Ease.EaseType> Blend = new Property<Animation.Ease.EaseType>();
		public Property<float> Duration = new Property<float>();
		[XmlIgnore]
		public Command OnDone = new Command();
		private Animation animation;

		public override void Awake()
		{
			base.Awake();
			this.Set.Action = this.set;
			this.OnDone.Action = this.onDone;
		}

		private void onDone()
		{
			this.animation = null;
		}

		private void set()
		{
			this.FindProperties();
			if (this.targetProperty != null)
			{
				if (this.animation != null && this.animation.Active)
					this.animation.Delete.Execute();

				Animation.Interval interval = null;
				switch (this.PropertyType.Value)
				{
					case PropType.Bool:
						interval = new Animation.Set<bool>((Property<bool>)this.targetProperty, this.Bool);
						break;
					case PropType.Int:
						interval = new Animation.IntMoveTo((Property<int>)this.targetProperty, this.Int, this.Duration);
						break;
					case PropType.Float:
						interval = new Animation.FloatMoveTo((Property<float>)this.targetProperty, this.Float, this.Duration);
						break;
					case PropType.Vector2:
						interval = new Animation.Vector2MoveTo((Property<Vector2>)this.targetProperty, this.Vector2, this.Duration);
						break;
					case PropType.Vector3:
						interval = new Animation.Vector3MoveTo((Property<Vector3>)this.targetProperty, this.Vector3, this.Duration);
						break;
					case PropType.Vector4:
						interval = new Animation.Vector4MoveTo((Property<Vector4>)this.targetProperty, this.Vector4, this.Duration);
						break;
					case PropType.Coord:
						interval = new Animation.Set<Voxel.Coord>((Property<Voxel.Coord>)this.targetProperty, this.Coord);
						break;
					case PropType.Direction:
						interval = new Animation.Set<Direction>((Property<Direction>)this.targetProperty, this.Direction);
						break;
					case PropType.String:
						interval = new Animation.Set<string>((Property<string>)this.targetProperty, this.String);
						break;
				}
				this.animation = new Animation
				(
					new Animation.Ease(interval, this.Blend),
					new Animation.Execute(this.OnDone)
				);
				this.Entity.Add(this.animation);
			}
		}
	}
}