using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Lemma.Components
{
	public class Rumble : Component<Main>, IUpdateableComponent
	{
		// Input properties
		public Property<float> BaseAmount = new Property<float>();
		public Property<float> CameraShake = new Property<float>();

		private Property<float> internalAmount = new Property<float>();

		private const float fadeTime = 0.5f;

		public void Go(float amount)
		{
			this.internalAmount.Value = Math.Max(this.internalAmount, amount);
		}

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.Add(new NotifyBinding(delegate()
			{
				float amount = MathHelper.Clamp(this.BaseAmount + this.CameraShake + this.internalAmount, 0.0f, 1.0f);
				GamePad.SetVibration(PlayerIndex.One, amount, amount);
			}, this.BaseAmount, this.CameraShake, this.internalAmount));
		}

		public override void delete()
		{
			base.delete();
			GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
		}

		public void Update(float dt)
		{
			float a = this.internalAmount;
			if (a > 0.0f)
				this.internalAmount.Value = MathHelper.Clamp(a - dt / fadeTime, 0.0f, 1.0f);
		}
	}
}
