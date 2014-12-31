using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.Components
{
	public class ParticleWind : Component<Main>, IUpdateableComponent
	{
		public Property<float> KernelSpacing = new Property<float> { Value = 8.0f };
		public const int KernelSize = 10;
		public const float RaycastHeight = 30.0f;
		public const float RaycastInterval = 0.25f;
		public const float StartHeightMultiplier = 2.0f;

		[XmlIgnore]
		public float[,] RaycastDistances = new float[KernelSize, KernelSize];

		private float raycastTimer = RaycastInterval;

		private static Random random = new Random();

		// Input properties
		public Property<Quaternion> Orientation = new Property<Quaternion>();
		public Property<float> Speed = new Property<float>();

		// Output properties
		public Property<Vector3> Jitter = new Property<Vector3>();
		public Property<Vector3> KernelOffset = new Property<Vector3>();

		public override void Awake()
		{
			base.Awake();
			this.EnabledInEditMode = true;
			this.EnabledWhenPaused = false;

			this.Add(new Binding<Vector3, float>(this.Jitter, x => new Vector3(x * KernelSize * 0.5f, x * KernelSize * 0.1f, x * KernelSize * 0.5f), this.KernelSpacing));
		}

		public void Update(float dt)
		{
			this.raycastTimer += dt;
			if (this.raycastTimer > RaycastInterval)
				this.Update();
		}

		public void Update()
		{
			this.raycastTimer = 0.0f;
			this.KernelOffset.Value = main.Camera.Position + Vector3.Transform(new Vector3(KernelSize * KernelSpacing * -0.5f, RaycastHeight + this.Speed * StartHeightMultiplier, KernelSize * KernelSpacing * -0.5f), this.Orientation);
			Vector3 dir = Vector3.Transform(Vector3.Down, this.Orientation);
			for (int x = 0; x < KernelSize; x++)
			{
				for (int y = 0; y < KernelSize; y++)
				{
					Vector3 pos = this.KernelOffset + Vector3.Transform(new Vector3(x * KernelSpacing, 0, y * KernelSpacing), this.Orientation);
					Voxel.GlobalRaycastResult raycast = Voxel.GlobalRaycast(pos, dir, (this.Speed * StartHeightMultiplier * 2.0f) + RaycastHeight, (index, type) => type != Voxel.t.Invisible);
					this.RaycastDistances[x, y] = raycast.Voxel == null ? float.MaxValue : raycast.Distance - RaycastHeight;
				}
			}
		}
	}
}