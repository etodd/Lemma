using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma
{
	public class Oculus
	{
		public static Matrix MatrixOvrToXna(OVR.ovrMatrix4f m)
		{
			return new Matrix
			(
				m.m[0, 0],
				m.m[1, 0],
				m.m[2, 0],
				m.m[3, 0],
				m.m[0, 1],
				m.m[1, 1],
				m.m[2, 1],
				m.m[3, 1],
				m.m[0, 2],
				m.m[1, 2],
				m.m[2, 2],
				m.m[3, 2],
				m.m[0, 3],
				m.m[1, 3],
				m.m[2, 3],
				m.m[3, 3]
			);
		}

		public class DistortionMesh
		{
			private VertexBuffer vb;
			private IndexBuffer ib;

			private Vector2 uvScale;
			private Vector2 uvOffset;

			private OVR.ovrEyeType eye;
			private OVR.Hmd hmd;
			private GraphicsDevice graphicsDevice;

			public void Load(GraphicsDevice graphicsDevice, OVR.Hmd hmd, OVR.ovrEyeType eyeType, OVR.ovrFovPort fov, Point textureSize)
			{
				this.hmd = hmd;
				this.graphicsDevice = graphicsDevice;
				this.eye = eyeType;
				OVR.ovrDistortionMesh meshData = hmd.CreateDistortionMesh(eyeType, fov, hmd.GetDesc().DistortionCaps).Value;
				OVR.ovrVector2f[] scaleAndOffset = hmd.GetRenderScaleAndOffset(fov, new OVR.ovrSizei(textureSize.X, textureSize.Y), new OVR.ovrRecti { Size = { w = textureSize.X, h = textureSize.Y } });
				this.uvScale = new Vector2(scaleAndOffset[0].x, scaleAndOffset[0].y);
				this.uvOffset = new Vector2(scaleAndOffset[1].x, scaleAndOffset[1].y);
				Vertex[] vertices = new Vertex[meshData.VertexCount];
				for (int i = 0; i < meshData.VertexCount; i++)
				{
					OVR.ovrDistortionVertex v = meshData.pVertexData[i];
					vertices[i].ScreenPosNDC = new Vector2(v.ScreenPosNDC.x, v.ScreenPosNDC.y);
					vertices[i].TimeWarpFactor = v.TimeWarpFactor;
					vertices[i].VignetteFactor = v.VignetteFactor;
					vertices[i].TanEyeAnglesR = new Vector2(v.TanEyeAnglesR.x, v.TanEyeAnglesR.y);
					vertices[i].TanEyeAnglesG = new Vector2(v.TanEyeAnglesG.x, v.TanEyeAnglesG.y);
					vertices[i].TanEyeAnglesB = new Vector2(v.TanEyeAnglesB.x, v.TanEyeAnglesB.y);
				}

				this.vb = new VertexBuffer(graphicsDevice, typeof(Vertex), (int)meshData.VertexCount, BufferUsage.WriteOnly);
				this.vb.SetData<Vertex>(vertices);

				this.ib = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, (int)meshData.IndexCount, BufferUsage.WriteOnly);
				this.ib.SetData<short>(meshData.pIndexData);
			}

			public void Render(RenderTarget2D frameBuffer, OVR.ovrPosef eyePose, Effect effect)
			{
				effect.Parameters["EyeToSourceUVScale"].SetValue(this.uvScale);
				effect.Parameters["EyeToSourceUVOffset"].SetValue(this.uvOffset);
				OVR.ovrMatrix4f[] timeWarpMatrices = this.hmd.ovrHmd_GetEyeTimewarpMatrices(this.eye, eyePose);
				Matrix timeWarp1 = Oculus.MatrixOvrToXna(timeWarpMatrices[0]);
				Matrix timeWarp2 = Oculus.MatrixOvrToXna(timeWarpMatrices[1]);
				effect.Parameters["EyeRotationStart"].SetValue(timeWarp1);
				effect.Parameters["EyeRotationEnd"].SetValue(timeWarp2);
				effect.Parameters["FrameBuffer" + Lemma.Components.Model.SamplerPostfix].SetValue(frameBuffer);
				effect.CurrentTechnique.Passes[0].Apply();

				this.graphicsDevice.SetVertexBuffer(this.vb);
				this.graphicsDevice.Indices = this.ib;

				// Draw primitives
				this.graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.vb.VertexCount, 0, this.ib.IndexCount / 3);
				Lemma.Components.Model.DrawCallCounter++;
				Lemma.Components.Model.TriangleCounter += this.ib.IndexCount / 3;
			}

			public struct Vertex : IVertexType
			{
				public Vector2 ScreenPosNDC; // [-1,+1],[-1,+1] over the entire framebuffer.
				public float TimeWarpFactor; // Lerp factor between time-warp matrices. Can be encoded in Pos.z.
				public float VignetteFactor; // Vignette fade factor. Can be encoded in Pos.w.
				public Vector2 TanEyeAnglesR;
				public Vector2 TanEyeAnglesG;
				public Vector2 TanEyeAnglesB;
				public readonly static VertexDeclaration Declaration = new VertexDeclaration
				(
					new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
					new VertexElement(8, VertexElementFormat.Single, VertexElementUsage.Position, 1),
					new VertexElement(12, VertexElementFormat.Single, VertexElementUsage.Position, 2),
					new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
					new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
					new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2)
				);

				public VertexDeclaration VertexDeclaration
				{
					get
					{
						return Vertex.Declaration;
					}
				}
			}
		}
	}
}
