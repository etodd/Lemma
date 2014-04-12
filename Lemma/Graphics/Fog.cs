using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class Fog : FullscreenQuad
	{
		private Effect effect;
		public Property<Vector4> Color = new Property<Vector4> { Value = Vector4.One };
		public Property<float> StartDistance = new Property<float> { Editable = true, Value = 50.0f };
		public Property<float> VerticalCenter = new Property<float> { Editable = true, Value = 0.0f };
		public Property<float> VerticalSize = new Property<float> { Editable = true, Value = 20.0f };
		public Property<bool> VerticalLimit = new Property<bool> { Editable = true, Value = true };
		private Property<float> endDistance = new Property<float> { Editable = false };

		public override void InitializeProperties()
		{
			base.InitializeProperties();
			this.DrawOrder.Editable = true;
			this.VerticalSize.Set = delegate(float value)
			{
				this.VerticalSize.InternalValue = value;
				this.effect.Parameters["VerticalSize"].SetValue(value);
			};
			this.Color.Set = delegate(Vector4 value)
			{
				this.Color.InternalValue = value;
				this.effect.Parameters["Color"].SetValue(value);
			};
			this.StartDistance.Set = delegate(float value)
			{
				this.StartDistance.InternalValue = value;
				this.effect.Parameters["StartDistance"].SetValue(value);
			};
			this.endDistance.Set = delegate(float value)
			{
				this.endDistance.InternalValue = value;
				this.effect.Parameters["EndDistance"].SetValue(value);
			};
			this.VerticalLimit.Set = delegate(bool value)
			{
				this.VerticalLimit.InternalValue = value;
				this.effect.CurrentTechnique = this.effect.Techniques[value ? "FogVerticalLimit" : "Fog"];
			};
			this.VerticalLimit.Reset();
			this.DrawOrder.Value = 11; // In front of water
			this.Add(new Binding<float>(this.endDistance, this.main.Camera.FarPlaneDistance));
			this.Enabled.Editable = true;
		}

		public override void LoadContent(bool reload)
		{
			base.LoadContent(reload);
			this.effect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Fog").Clone();
			this.VerticalLimit.Reset();
			if (reload)
			{
				this.VerticalSize.Reset();
				this.Color.Reset();
				this.StartDistance.Reset();
				this.endDistance.Reset();
			}
		}

		public override void DrawAlpha(GameTime time, RenderParameters p)
		{
			if (p.IsMainRender)
			{
				Vector3 originalCameraPosition = p.Camera.Position;
				float verticalDiff = this.VerticalCenter - originalCameraPosition.Y;
				if (this.VerticalLimit)
				{
					if (Math.Abs(verticalDiff) - this.VerticalSize > p.Camera.FarPlaneDistance)
						return; // We're not visible anyway
				}

				Matrix originalViewMatrix = p.Camera.View;
				p.Camera.Position.Value = Vector3.Zero;
				p.Camera.SetParameters(this.effect);
				p.Camera.Position.Value = originalCameraPosition;
				p.Camera.View.Value = originalViewMatrix;

				this.effect.Parameters["Depth" + Model.SamplerPostfix].SetValue(p.DepthBuffer);
				if (this.VerticalLimit)
					this.effect.Parameters["VerticalCenter"].SetValue(verticalDiff);
				this.effect.CurrentTechnique.Passes[0].Apply();
				base.DrawAlpha(time, p);
			}
		}

		protected override void delete()
		{
			base.delete();
			this.effect.Dispose();
		}
	}
}
