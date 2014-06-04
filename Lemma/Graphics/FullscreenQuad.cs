using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Lemma.Components
{
	public class FullscreenQuad : Component<Main>, IDrawableAlphaComponent
	{
		/// <summary>
		/// A struct that represents a single vertex in the
		/// vertex buffer.
		/// </summary>
		private struct QuadVertex : IVertexType
		{
			public Vector3 Position;
			public Vector3 TexCoordAndCornerIndex;
			public VertexDeclaration VertexDeclaration
			{
				get
				{
					return FullscreenQuad.VertexDeclaration;
				}
			}
		}

		public EditorProperty<int> DrawOrder { get; set; }

		private VertexBuffer vertexBuffer;

		private static VertexDeclaration vertexDeclaration;
		public static VertexDeclaration VertexDeclaration
		{
			get
			{
				if (FullscreenQuad.vertexDeclaration == null)
				{
					VertexElement[] declElements = new VertexElement[2];
					declElements[0].Offset = 0;
					declElements[0].UsageIndex = 0;
					declElements[0].VertexElementFormat = VertexElementFormat.Vector3;
					declElements[0].VertexElementUsage = VertexElementUsage.Position;
					declElements[1].Offset = sizeof(float) * 3;
					declElements[1].UsageIndex = 0;
					declElements[1].VertexElementFormat = VertexElementFormat.Vector3;
					declElements[1].VertexElementUsage = VertexElementUsage.TextureCoordinate;
					FullscreenQuad.vertexDeclaration = new VertexDeclaration(declElements);
				}
				return FullscreenQuad.vertexDeclaration;
			}
		}

		public FullscreenQuad()
		{
			this.DrawOrder = new EditorProperty<int>();
		}

		public virtual void LoadContent(bool reload)
		{
			// Create a vertex buffer for the quad, and fill it in
			this.vertexBuffer = new VertexBuffer(this.main.GraphicsDevice, typeof(QuadVertex), FullscreenQuad.VertexDeclaration.VertexStride * 4, BufferUsage.None);
			QuadVertex[] vbData = new QuadVertex[4];

			// Upper right
			vbData[0].Position = new Vector3(1, 1, 1);
			vbData[0].TexCoordAndCornerIndex = new Vector3(1, 0, 1);

			// Lower right
			vbData[1].Position = new Vector3(1, -1, 1);
			vbData[1].TexCoordAndCornerIndex = new Vector3(1, 1, 2);

			// Upper left
			vbData[2].Position = new Vector3(-1, 1, 1);
			vbData[2].TexCoordAndCornerIndex = new Vector3(0, 0, 0);

			// Lower left
			vbData[3].Position = new Vector3(-1, -1, 1);
			vbData[3].TexCoordAndCornerIndex = new Vector3(0, 1, 3);

			this.vertexBuffer.SetData(vbData);
		}

		/// <summary>
		/// Draws the full screen quad
		/// </summary>
		/// <param name="graphicsDevice">The GraphicsDevice to use for rendering</param>
		public virtual void DrawAlpha(GameTime time, RenderParameters p)
		{
			// Set the vertex buffer and declaration
			this.main.GraphicsDevice.SetVertexBuffer(this.vertexBuffer);

			// Draw primitives
			this.main.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
			Model.DrawCallCounter++;
			Model.TriangleCounter += 2;
		}
	}
}
