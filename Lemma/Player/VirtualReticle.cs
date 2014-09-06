using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Components;
using Microsoft.Xna.Framework;

namespace Lemma
{
	public class VirtualReticle : Component<Main>, IUpdateableComponent
	{
		private const float size = 0.01f;

		// Input properties
		public Property<float> Rotation = new Property<float>();

		// Output properties
		public Property<Matrix> Transform = new Property<Matrix>();

		public void Update(float dt)
		{
			Vector3 cameraPosition = this.main.Camera.Position;
			Vector3 forwardVector = -Matrix.CreateRotationY(this.Rotation).Forward;

			Matrix matrix = Matrix.CreateRotationY(this.Rotation + (float)Math.PI * 0.5f);

			Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(cameraPosition, forwardVector, this.main.Camera.FarPlaneDistance, null, true);
			if (hit.Voxel != null)
			{
				matrix *= Matrix.CreateScale(hit.Distance * VirtualReticle.size);
				matrix.Translation = hit.Position;
			}
			else
			{
				matrix *= Matrix.CreateScale(this.main.Camera.FarPlaneDistance * VirtualReticle.size);
				matrix.Translation = cameraPosition + forwardVector * this.main.Camera.FarPlaneDistance;
			}
			this.Transform.Value = matrix;
		}
	}
}
