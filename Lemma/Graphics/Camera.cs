using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Lemma.Util;

namespace Lemma.Components
{
	public class Camera : Component<Main>, IUpdateableComponent
	{
		/// <summary>
		/// Gets camera view matrix.
		/// </summary>
		public Property<Matrix> View = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets the inverse camera view matrix.
		/// </summary>
		public Property<Matrix> InverseView = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets or sets camera projection matrix.
		/// </summary>
		public Property<Matrix> Projection = new Property<Matrix> { Value = Matrix.Identity };
		public Property<Matrix> LastProjection = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets the inverse of the camera projection matrix.
		/// </summary>
		public Property<Matrix> InverseProjection = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets the view matrix from the last frame.
		/// </summary>
		public Property<Matrix> LastView = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets the view/projection matrix from the last frame.
		/// </summary>
		public Property<Matrix> LastViewProjection = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets camera view matrix multiplied by projection matrix.
		/// </summary>
		public Property<Matrix> ViewProjection = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets inverse of the view/projection matrix.
		/// </summary>
		public Property<Matrix> InverseViewProjection = new Property<Matrix> { Value = Matrix.Identity };
		/// <summary>
		/// Gets or sets camera position.
		/// </summary>
		public Property<Vector3> Position = new Property<Vector3>();
		/// <summary>
		/// Gets or sets camera angles.
		/// </summary>
		public Property<Vector3> Angles = new Property<Vector3>();
		/// <summary>
		/// Gets or sets camera field of view.
		/// </summary>
		public Property<float> FieldOfView = new Property<float> { Value = MathHelper.ToRadians(80.0f) };

		public Property<Point> ViewportSize = new Property<Point> { Value = new Point(1, 1) };
		/// <summary>
		/// Gets or sets camera aspect ratio.
		/// </summary>
		public Property<float> AspectRatio = new Property<float> { Value = 1.0f };
		/// <summary>
		/// Gets or sets camera near plane distance.
		/// </summary>
		public Property<float> NearPlaneDistance = new Property<float> { Value = 0.01f };
		/// <summary>
		/// Gets or sets camera far plane distance.
		/// </summary>
		public Property<float> FarPlaneDistance = new Property<float> { Value = 100.0f };

		public Property<Vector3> Forward = new Property<Vector3> { Value = Vector3.Forward };

		public Property<Vector3> Right = new Property<Vector3> { Value = Vector3.Right };

		public Property<Vector3> Up = new Property<Vector3> { Value = Vector3.Up };

		public Property<Matrix> RotationMatrix = new Property<Matrix> { Value = Matrix.Identity };

		public Property<BoundingFrustum> BoundingFrustum = new Property<BoundingFrustum> { Value = new BoundingFrustum(Matrix.Identity) };

		public Property<Vector2> OrthographicSize = new Property<Vector2> { Value = new Vector2(70.0f, 70.0f) };

		public Property<bool> OrthographicProjection = new Property<bool>();

		/// <summary>
		/// Gets or sets camera's target.
		/// </summary>
		public Property<Vector3> Target = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();
			this.Add(new Binding<float, Point>(this.AspectRatio, x => (float)x.X / (float)x.Y, this.ViewportSize));
			
			this.Add(new Binding<Matrix>(this.Projection,
				() => this.OrthographicProjection
					? Matrix.CreateOrthographic(this.OrthographicSize.Value.X, this.OrthographicSize.Value.Y, this.NearPlaneDistance, this.FarPlaneDistance)
					: Matrix.CreatePerspectiveFieldOfView(this.FieldOfView, this.AspectRatio, this.NearPlaneDistance, this.FarPlaneDistance),
					this.FieldOfView, this.AspectRatio, this.NearPlaneDistance, this.FarPlaneDistance, this.OrthographicSize, this.OrthographicProjection));

			this.Add(new Binding<Matrix, Matrix>(this.InverseProjection, x => Matrix.Invert(x), this.Projection));
			this.Add(new Binding<Matrix>(this.ViewProjection, () => this.View.Value * this.Projection, this.View, this.Projection));
			this.Add(new Binding<Matrix, Matrix>(this.InverseViewProjection, x => Matrix.Invert(x), this.ViewProjection));
			this.Add(new Binding<BoundingFrustum, Matrix>(this.BoundingFrustum, x => new BoundingFrustum(x), this.ViewProjection));
			this.Add(new TwoWayBinding<Vector3, Matrix>(
				this.Angles,
				delegate(Matrix matrix)
				{
					Vector3 scale, translation;
					Quaternion rotation;
					matrix.Decompose(out scale, out rotation, out translation);
					Vector3 euler = rotation.ToEuler();
					return new Vector3(euler.X, -euler.Y + (float)Math.PI * 0.5f, euler.Z);
				},
				this.RotationMatrix,
				delegate(Vector3 angles)
				{
					return Matrix.CreateRotationX(angles.X) * Matrix.CreateRotationY(angles.Y) * Matrix.CreateRotationZ(angles.Z);
				}));

			this.Add(new TwoWayBinding<Matrix, Vector3>(
				this.RotationMatrix,
				x => Matrix.CreateLookAt(Vector3.Zero, x, Vector3.Up),
				this.Forward,
				x => x.Forward));

			this.Add(new Binding<Vector3, Matrix>(this.Right, x => x.Right, this.RotationMatrix));

			this.Add(new Binding<Vector3, Matrix>(this.Up, x => x.Up, this.RotationMatrix));

			this.Add(new Binding<Matrix>(
				this.View,
				() => Matrix.CreateTranslation(-this.Position.Value) * Matrix.Invert(this.RotationMatrix),
				this.Position, this.RotationMatrix));

			this.Add(new Binding<Matrix, Matrix>(this.InverseView, x => Matrix.Invert(x), this.View));

			this.Add(new TwoWayBinding<Vector3, Vector3>(
				this.Angles,
				delegate(Vector3 target)
				{
					Vector3 dir = Vector3.Normalize(this.Position.Value - target);
					return new Vector3(-(float)Math.Asin(dir.Y), (float)Math.Atan2(dir.X, dir.Z), 0.0f);
				},
				new IProperty[] { this.Position },
				this.Target,
				delegate(Vector3 angles)
				{
					return this.Position.Value + Vector3.TransformNormal(Vector3.Forward, Matrix.CreateRotationX(angles.X) * Matrix.CreateRotationY(angles.Y));
				},
				new IProperty[] { this.Position }));
		}

		public void SetPerspectiveProjection(float fov, Point viewportSize, float near, float far)
		{
			this.FieldOfView.SetStealthy(fov);
			this.ViewportSize.SetStealthy(viewportSize);
			this.AspectRatio.SetStealthy((float)viewportSize.X / (float)viewportSize.Y);
			this.NearPlaneDistance.SetStealthy(near);
			this.FarPlaneDistance.SetStealthy(far);
			this.OrthographicProjection.SetStealthy(false);
			this.Projection.Value = Matrix.CreatePerspectiveFieldOfView(fov, this.AspectRatio, near, far);
		}

		public void SetOrthographicProjection(float width, float height, float near, float far)
		{
			this.OrthographicSize.SetStealthy(new Vector2(width, height));
			this.NearPlaneDistance.SetStealthy(near);
			this.FarPlaneDistance.SetStealthy(far);
			this.OrthographicProjection.SetStealthy(true);
			this.Projection.Value = Matrix.CreateOrthographic(width, height, near, far);
		}

		void IUpdateableComponent.Update(float dt)
		{
			this.LastView.Value = this.View;
			this.LastViewProjection.Value = this.ViewProjection;
			this.LastProjection.Value = this.Projection;
		}

		public void SetParameters(Effect effect)
		{
			EffectParameter param = effect.Parameters["ViewMatrix"];
			if (param != null)
				param.SetValue(this.View);
			param = effect.Parameters["ViewMatrixRotationOnly"];
			if (param != null)
			{
				Matrix m = this.View;
				m.Translation = Vector3.Zero;
				param.SetValue(m);
			}
			param = effect.Parameters["InverseViewMatrixRotationOnly"];
			if (param != null)
			{
				Matrix m = this.InverseView;
				m.Translation = Vector3.Zero;
				param.SetValue(m);
			}
			param = effect.Parameters["InverseViewMatrix"];
			if (param != null)
				param.SetValue(this.InverseView);
			param = effect.Parameters["ProjectionMatrix"];
			if (param != null)
				param.SetValue(this.Projection);
			param = effect.Parameters["InverseProjectionMatrix"];
			if (param != null)
				param.SetValue(this.InverseProjection);
			param = effect.Parameters["ViewProjectionMatrix"];
			if (param != null)
				param.SetValue(this.ViewProjection);
			param = effect.Parameters["FarPlaneDistance"];
			if (param != null)
				param.SetValue(this.FarPlaneDistance);
			param = effect.Parameters["NearPlaneDistance"];
			if (param != null)
				param.SetValue(this.NearPlaneDistance);
			param = effect.Parameters["InverseViewProjectionMatrix"];
			if (param != null)
				param.SetValue(this.InverseViewProjection);
			param = effect.Parameters["LastFrameViewProjectionMatrix"];
			if (param != null)
				param.SetValue(this.LastViewProjection);
			param = effect.Parameters["LastFrameViewProjectionMatrixRotationOnly"];
			if (param != null)
			{
				Matrix m = this.LastView;
				m.Translation = Vector3.Zero;
				param.SetValue(m * this.LastProjection);
			}
			param = effect.Parameters["LastFrameViewMatrix"];
			if (param != null)
				param.SetValue(this.LastView);
			param = effect.Parameters["DestinationDimensions"];
			if (param != null)
			{
				Point size = this.ViewportSize;
				param.SetValue(new Vector2(size.X, size.Y));
			}
		}

		public Vector2 GetWorldSpaceControllerCoordinates(Vector2 thumbStick)
		{
			Vector3 temp = Vector3.Transform(new Vector3(thumbStick.X, 0.0f, -thumbStick.Y), Matrix.CreateRotationY(this.Angles.Value.Y));
			return new Vector2(temp.X, temp.Z);
		}
	}
}
