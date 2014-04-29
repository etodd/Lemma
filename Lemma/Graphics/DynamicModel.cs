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
		private VertexDeclaration declaration;
		private Vertex[] vertices = new Vertex[] {};
		private bool verticesChanged;
		private static IndexBuffer indexBuffer;
		private static object indicesLock = new object();
		private static uint[] indices = new uint[] { };
		private int vertexCount;
		private int indexCount;
		private int lockedVertexCount;

		public void UpdateVertices(Vertex[] v, int surfaces)
		{
			this.vertices = v;
			this.vertexCount = surfaces * 4;
			this.verticesChanged = true;
			this.indexCount = surfaces * 6;
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
			base.LoadContent(reload);
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

		public static uint[] GetIndices(int size)
		{
			lock (DynamicModel<Vertex>.indicesLock)
			{
				if (size <= DynamicModel<Vertex>.indices.Length)
					return DynamicModel<Vertex>.indices;

				int newBufferSize = (int)Math.Pow(2.0, Math.Ceiling(Math.Log(size, 2.0)));

				uint[] newIndices = new uint[newBufferSize];

				int startGeneratingIndices = (int)Math.Floor(DynamicModel<Vertex>.indices.Length / 6.0f) * 6;
				Array.Copy(DynamicModel<Vertex>.indices, newIndices, startGeneratingIndices);

				DynamicModel<Vertex>.indices = newIndices;

				uint vertexIndex = ((uint)startGeneratingIndices / 6) * 4;
				for (int index = startGeneratingIndices; index < (int)Math.Floor(newBufferSize / 6.0f) * 6; vertexIndex += 4)
				{
					newIndices[index++] = vertexIndex + 2;
					newIndices[index++] = vertexIndex + 1;
					newIndices[index++] = vertexIndex + 0;
					newIndices[index++] = vertexIndex + 3;
					newIndices[index++] = vertexIndex + 1;
					newIndices[index++] = vertexIndex + 2;
				}
				return newIndices;
			}
		}

		/// <summary>
		/// Draws a single mesh using the given world matrix.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="transform"></param>
		protected override void draw(RenderParameters parameters, Matrix transform)
		{
			bool needNewVertexBuffer = this.vertexBuffer == null || this.vertexBuffer.IsContentLost;

			if (this.vertexCount == 0)
			{
				this.verticesChanged = false;
				this.lockedVertexCount = 0;
			}
			else if (needNewVertexBuffer || this.verticesChanged)
			{
				bool locked;
				if (this.Lock == null)
					locked = true;
				else
					locked = Monitor.TryEnter(this.Lock);

				if (locked)
				{
					if (needNewVertexBuffer || this.vertexBuffer.VertexCount < this.vertexCount)
					{
						if (this.vertexBuffer != null && !this.vertexBuffer.IsDisposed)
							this.vertexBuffer.Dispose();
						this.vertexBuffer = new DynamicVertexBuffer
						(
							this.main.GraphicsDevice,
							this.declaration,
							(int)Math.Pow(2.0, Math.Ceiling(Math.Log(this.vertexCount, 2.0))),
							BufferUsage.WriteOnly
						);

						this.verticesChanged = true;
					}

					if (DynamicModel<Vertex>.indexBuffer == null || DynamicModel<Vertex>.indexBuffer.IndexCount < this.indexCount)
					{
						if (DynamicModel<Vertex>.indexBuffer != null)
							DynamicModel<Vertex>.indexBuffer.Dispose();
						
						lock (DynamicModel<Vertex>.indicesLock)
						{
							uint[] indices = DynamicModel<Vertex>.GetIndices(this.indexCount);
							DynamicModel<Vertex>.indexBuffer = new IndexBuffer(this.main.GraphicsDevice, typeof(uint), indices.Length, BufferUsage.WriteOnly);
							DynamicModel<Vertex>.indexBuffer.SetData(indices, 0, indices.Length);
						}
					}

					this.vertexBuffer.SetData(0, this.vertices, 0, this.vertexCount, this.declaration.VertexStride, SetDataOptions.Discard);

					this.verticesChanged = false;
					this.lockedVertexCount = this.vertexCount;

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
				this.main.GraphicsDevice.Indices = DynamicModel<Vertex>.indexBuffer;

				this.effect.CurrentTechnique.Passes[0].Apply();
				this.main.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.lockedVertexCount, 0, this.lockedVertexCount / 2);
				Model.DrawCallCounter++;
				Model.TriangleCounter += this.lockedVertexCount / 2;

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
		public Property<int> DrawOrder { get; set; }

		public DynamicModelAlpha(VertexDeclaration declaration)
			: base(declaration)
		{
			this.Alpha = this.GetFloatParameter("Alpha");
			this.Alpha.Value = 1.0f;
			this.DrawOrder = new Property<int> { Editable = true };
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