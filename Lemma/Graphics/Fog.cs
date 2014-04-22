using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Fog : Component<Main>, IDrawableAlphaComponent
	{
		/// <summary>
		/// A struct that represents a single vertex in the
		/// vertex buffer.
		/// </summary>
		private struct QuadVertex : IVertexType
		{
			public Vector3 Position;
			public Vector2 TexCoord;
			public Vector3 Normal;
			public VertexDeclaration VertexDeclaration
			{
				get
				{
					return Fog.VertexDeclaration;
				}
			}
		}

		public Property<int> DrawOrder { get; set; }
		public Property<float> VerticalCenter = new Property<float> { Editable = true, Value = 0.0f };
		public Property<float> VerticalSize = new Property<float> { Editable = true, Value = 20.0f };
		public Property<float> StartDistance = new Property<float> { Editable = true, Value = 50.0f };
		public Property<bool> Vertical = new Property<bool> { Editable = true };
		public Property<Vector4> Color = new Property<Vector4> { Value = Vector4.One };
		private Property<float> endDistance = new Property<float> { Editable = false };

		private VertexBuffer surfaceVertexBuffer;
		private VertexBuffer underSurfaceVertexBuffer;

		private static VertexDeclaration vertexDeclaration;
		public static VertexDeclaration VertexDeclaration
		{
			get
			{
				if (Fog.vertexDeclaration == null)
				{
					Microsoft.Xna.Framework.Graphics.VertexElement[] declElements = new VertexElement[3];
					declElements[0].Offset = 0;
					declElements[0].UsageIndex = 0;
					declElements[0].VertexElementFormat = VertexElementFormat.Vector3;
					declElements[0].VertexElementUsage = VertexElementUsage.Position;
					declElements[1].Offset = sizeof(float) * 3;
					declElements[1].UsageIndex = 0;
					declElements[1].VertexElementFormat = VertexElementFormat.Vector2;
					declElements[1].VertexElementUsage = VertexElementUsage.TextureCoordinate;
					declElements[2].Offset = sizeof(float) * 5;
					declElements[2].UsageIndex = 0;
					declElements[2].VertexElementFormat = VertexElementFormat.Vector3;
					declElements[2].VertexElementUsage = VertexElementUsage.Normal;
					Fog.vertexDeclaration = new VertexDeclaration(declElements);
				}
				return Fog.vertexDeclaration;
			}
		}

		private Effect effect;

		public Fog()
		{
			this.DrawOrder = new Property<int> { Editable = true, Value = 10 };
		}

		public override void LoadContent(bool reload)
		{
			this.effect = this.main.Content.Load<Effect>("Effects\\PostProcess\\Fog").Clone();

			// Surface
			this.surfaceVertexBuffer = new VertexBuffer(this.main.GraphicsDevice, typeof(QuadVertex), Fog.VertexDeclaration.VertexStride * 4, BufferUsage.None);
			QuadVertex[] surfaceData = new QuadVertex[4];

			// Upper right
			const float scale = 0.5f;
			surfaceData[0].Position = new Vector3(scale, 0, scale);
			surfaceData[0].TexCoord = new Vector2(1, 0);

			// Upper left
			surfaceData[1].Position = new Vector3(-scale, 0, scale);
			surfaceData[1].TexCoord = new Vector2(0, 0);

			// Lower right
			surfaceData[2].Position = new Vector3(scale, 0, -scale);
			surfaceData[2].TexCoord = new Vector2(1, 1);

			// Lower left
			surfaceData[3].Position = new Vector3(-scale, 0, -scale);
			surfaceData[3].TexCoord = new Vector2(0, 1);

			surfaceData[0].Normal = surfaceData[1].Normal = surfaceData[2].Normal = surfaceData[3].Normal = new Vector3(0, 1, 0);

			this.surfaceVertexBuffer.SetData(surfaceData);

			// Undersurface
			this.underSurfaceVertexBuffer = new VertexBuffer(this.main.GraphicsDevice, typeof(QuadVertex), Fog.VertexDeclaration.VertexStride * 4, BufferUsage.None);

			QuadVertex[] underSurfaceData = new QuadVertex[4];

			// Upper right
			underSurfaceData[0].Position = new Vector3(1, 1, 1);
			underSurfaceData[0].TexCoord = new Vector2(1, 0);

			// Lower right
			underSurfaceData[1].Position = new Vector3(1, -1, 1);
			underSurfaceData[1].TexCoord = new Vector2(1, 1);

			// Upper left
			underSurfaceData[2].Position = new Vector3(-1, 1, 1);
			underSurfaceData[2].TexCoord = new Vector2(0, 0);

			// Lower left
			underSurfaceData[3].Position = new Vector3(-1, -1, 1);
			underSurfaceData[3].TexCoord = new Vector2(0, 1);

			underSurfaceData[0].Normal = underSurfaceData[1].Normal = underSurfaceData[2].Normal = underSurfaceData[3].Normal = new Vector3(0, 0, -1);

			this.underSurfaceVertexBuffer.SetData(underSurfaceData);

			this.VerticalSize.Reset();
			this.Vertical.Reset();
			this.Color.Reset();
			this.StartDistance.Reset();
			this.endDistance.Reset();
		}

		public override void InitializeProperties()
		{
			this.EnabledWhenPaused.Value = true;

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
			this.Add(new Binding<float>(this.endDistance, this.main.Camera.FarPlaneDistance));
		}

		void IDrawableAlphaComponent.DrawAlpha(Microsoft.Xna.Framework.GameTime time, RenderParameters p)
		{
			if (!p.IsMainRender)
				return;

			Vector3 originalCameraPosition = p.Camera.Position;
			p.Camera.Position.Value = Vector3.Zero;
			float verticalDiff = this.VerticalCenter + this.VerticalSize - originalCameraPosition.Y;

			if (this.Vertical && verticalDiff < -p.Camera.FarPlaneDistance)
				return; // We're not visible anyway

			this.effect.Parameters["Depth" + Model.SamplerPostfix].SetValue(p.DepthBuffer);
			p.Camera.SetParameters(this.effect);
			p.Camera.Position.Value = originalCameraPosition;

			bool underSurface = !this.Vertical || verticalDiff > 0;

			if (this.Vertical)
				this.effect.Parameters["VerticalCenter"].SetValue(verticalDiff);

			if (underSurface)
			{
				// Draw under surface stuff
				this.effect.CurrentTechnique = this.effect.Techniques[this.Vertical ? "FogVertical" : "Fog"];
				this.main.GraphicsDevice.SetVertexBuffer(this.underSurfaceVertexBuffer);
			}
			else
			{
				this.effect.CurrentTechnique = this.effect.Techniques["FogVerticalSurface"];
				this.main.GraphicsDevice.SetVertexBuffer(this.surfaceVertexBuffer);
			}
			this.effect.CurrentTechnique.Passes[0].Apply();
			this.main.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
			Model.DrawCallCounter++;
			Model.TriangleCounter += 2;
		}

		public override void delete()
		{
			this.effect.Dispose();
			this.surfaceVertexBuffer.Dispose();
			this.underSurfaceVertexBuffer.Dispose();
			base.delete();
		}
	}
}