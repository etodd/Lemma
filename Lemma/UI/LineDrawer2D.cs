using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class LineDrawer2D : UIComponent
	{
		public struct Line
		{
			public VertexPositionColor A;
			public VertexPositionColor B;
		}

		[XmlIgnore]
		public ListProperty<Line> Lines = new ListProperty<Line>();

		public Property<Vector4> Color = new Property<Vector4> { Value = Vector4.One };

		private Effect effect;

		private bool changed;

		private DynamicVertexBuffer vertexBuffer;

		public override void LoadContent(bool reload)
		{
			this.effect = this.main.Content.Load<Effect>("Effects\\Lines2D").Clone();
		}

		public override void Awake()
		{
			base.Awake();
			this.requiresNewBatch = true;
			this.Add(new ListNotifyBinding<Line>(delegate() { this.changed = true; }, this.Lines));
		}

		protected override void draw(GameTime time, Matrix parent, Matrix transform)
		{
			if (this.Lines.Length == 0)
				return;

			if (this.vertexBuffer == null || this.vertexBuffer.IsContentLost || this.Lines.Length * 2 > this.vertexBuffer.VertexCount || this.changed)
			{
				this.changed = false;
				if (this.vertexBuffer != null)
					this.vertexBuffer.Dispose();

				this.vertexBuffer = new DynamicVertexBuffer(this.main.GraphicsDevice, VertexPositionColor.VertexDeclaration, (this.Lines.Length * 2) + 8, BufferUsage.WriteOnly);

				VertexPositionColor[] data = new VertexPositionColor[this.vertexBuffer.VertexCount];
				int i = 0;
				foreach (Line line in this.Lines)
				{
					data[i] = line.A;
					data[i + 1] = line.B;
					i += 2;
				}
				this.vertexBuffer.SetData<VertexPositionColor>(data, 0, this.Lines.Length * 2, SetDataOptions.Discard);
			}

			Viewport viewport = this.main.GraphicsDevice.Viewport;
			float inverseWidth = (viewport.Width > 0) ? (1f / (float)viewport.Width) : 0f;
			float inverseHeight = (viewport.Height > 0) ? (-1f / (float)viewport.Height) : 0f;
			Matrix projection = default(Matrix);
			projection.M11 = inverseWidth * 2f;
			projection.M22 = inverseHeight * 2f;
			projection.M33 = 1f;
			projection.M44 = 1f;
			projection.M41 = -1f;
			projection.M42 = 1f;
			projection.M41 -= inverseWidth;
			projection.M42 -= inverseHeight;

			this.effect.Parameters["Color"].SetValue(this.Color);
			this.effect.Parameters["Transform"].SetValue(transform * projection);

			this.effect.CurrentTechnique.Passes[0].Apply();
			this.main.GraphicsDevice.SetVertexBuffer(this.vertexBuffer);
			this.main.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, this.Lines.Length);
			Model.DrawCallCounter++;
			Model.TriangleCounter += this.Lines.Length;
		}

		public override void delete()
		{
			this.effect.Dispose();
			if (this.vertexBuffer != null)
				this.vertexBuffer.Dispose();
			base.delete();
		}
	}
}
