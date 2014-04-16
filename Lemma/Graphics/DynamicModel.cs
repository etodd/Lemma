using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using Lemma.Util;
using System.Threading;

namespace Lemma.Components
{
	public class DynamicModel<Vertex> : Model
		where Vertex : struct
	{
		private DynamicVertexBuffer vertexBuffer;
		private IndexBuffer indexBuffer;
		private VertexDeclaration declaration;
		public Vertex[] Vertices = new Vertex[] {};
		public uint[] Indices = new uint[] {};
		private bool verticesChanged;
		private bool indicesChanged;
		private int lockedVertexCount;

		public void UpdateVertices()
		{
			this.verticesChanged = true;
		}

		public void UpdateIndices()
		{
			this.indicesChanged = true;
		}

		[XmlIgnore]
		public object Lock;

		public DynamicModel(VertexDeclaration declaration)
		{
			this.Serialize = false;
			this.declaration = declaration;
		}

		public override void LoadContent(bool reload)
		{
			if (reload)
			{
				this.loadEffect(this.EffectFile);
				this.vertexBuffer = null;
			}
		}

		protected override void loadEffect(string file)
		{
			if (file == null)
				throw new Exception("Must define effect file for dynamic model.");
			base.loadEffect(file);
		}

		/// <summary>
		/// Draws a single mesh using the given world matrix.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="transform"></param>
		protected override void draw(RenderParameters parameters, Matrix transform)
		{
			if (parameters.IsMainRender)
			{
				bool locked;
				if (this.Lock == null)
					locked = true;
				else
					locked = Monitor.TryEnter(this.Lock);

				if (locked)
				{
					if (this.Vertices.Length > 0 && (this.vertexBuffer == null || this.vertexBuffer.IsContentLost || this.vertexBuffer.VertexCount < this.Vertices.Length))
					{
						if (this.vertexBuffer != null && !this.vertexBuffer.IsDisposed)
							this.vertexBuffer.Dispose();
						this.vertexBuffer = new DynamicVertexBuffer
						(
							this.main.GraphicsDevice,
							this.declaration,
							(int)Math.Pow(2.0, Math.Ceiling(Math.Log(this.Vertices.Length, 2.0))),
							BufferUsage.WriteOnly
						);

						this.verticesChanged = true;
					}

					if (this.Indices.Length > 0 && (this.indexBuffer == null || this.indexBuffer.IndexCount < this.Indices.Length))
					{
						if (this.indexBuffer != null)
							this.indexBuffer.Dispose();

						this.indexBuffer = new IndexBuffer(this.main.GraphicsDevice, typeof(uint), (int)Math.Pow(2.0, Math.Ceiling(Math.Log(this.Indices.Length, 2.0))), BufferUsage.WriteOnly);
						this.indicesChanged = true;
					}

					if (this.indicesChanged)
					{
						if (this.Indices.Length > 0)
							this.indexBuffer.SetData(this.Indices);
						this.indicesChanged = false;
					}

					if (this.verticesChanged)
					{
						if (this.Vertices.Length > 0)
							this.vertexBuffer.SetData(0, this.Vertices, 0, this.Vertices.Length, this.declaration.VertexStride, SetDataOptions.Discard);
						this.verticesChanged = false;
						this.lockedVertexCount = this.Vertices.Length;
					}

					if (this.Lock != null)
						Monitor.Exit(this.Lock);
				}
			}

			if (this.lockedVertexCount > 0 && this.vertexBuffer != null && !this.vertexBuffer.IsContentLost && this.setParameters(transform, parameters))
			{
				this.main.LightingManager.SetRenderParameters(this.effect, parameters);

				RasterizerState originalState = this.main.GraphicsDevice.RasterizerState;
				RasterizerState noCullState = null;
				if (parameters.IsMainRender && this.DisableCulling)
				{
					noCullState = new RasterizerState { CullMode = CullMode.None };
					this.main.GraphicsDevice.RasterizerState = noCullState;
				}

				this.main.GraphicsDevice.SetVertexBuffer(this.vertexBuffer);
				this.main.GraphicsDevice.Indices = this.indexBuffer;

				foreach (EffectPass pass in this.effect.CurrentTechnique.Passes)
				{
					pass.Apply();
					this.main.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.lockedVertexCount, 0, this.lockedVertexCount / 2);
				}

				if (noCullState != null)
					this.main.GraphicsDevice.RasterizerState = originalState;
			}

			if (parameters.IsMainRender)
			{
				this.lastTransform = transform;
				this.lastWorldViewProjection = transform * parameters.Camera.ViewProjection;
			}
		}

		/// <summary>
		/// Draws a collection of instances. Requires an HLSL effect designed for hardware instancing.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="camera"></param>
		/// <param name="instances"></param>
		protected override void drawInstances(RenderParameters parameters, Matrix transform)
		{
			throw new NotSupportedException("Instancing not supported for dynamic models.");
		}
	}

	public class DynamicModelAlpha<Vertex> : DynamicModel<Vertex>, IDrawableAlphaComponent
		where Vertex : struct
	{
		public Property<float> Alpha = null;

		public DynamicModelAlpha(VertexDeclaration declaration)
			: base(declaration)
		{
			this.Alpha = this.GetFloatParameter("Alpha");
			this.Alpha.Value = 1.0f;
		}

		public override void Draw(GameTime time, RenderParameters parameters)
		{
			
		}

		void IDrawableAlphaComponent.DrawAlpha(GameTime time, RenderParameters parameters)
		{
			if (this.Alpha > 0.0f)
				base.Draw(time, parameters);
		}

		protected override bool setParameters(Matrix transform, RenderParameters parameters)
		{
			bool result = base.setParameters(transform, parameters);
			if (result)
				this.effect.Parameters["DepthBuffer"].SetValue(parameters.DepthBuffer);
			return result;
		}
	}
}