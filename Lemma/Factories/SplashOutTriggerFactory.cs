using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Lemma.Util;

namespace Lemma.Factories
{
	public class SplashOutTriggerFactory : Factory<Main>
	{
		public SplashOutTriggerFactory()
		{
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
			this.AvailableInRelease = false;
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "SplashOutTrigger");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			this.SetMain(entity, main);

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main, true, false);
			attachable.Enabled.Value = true;

			if (!main.EditorEnabled)
			{
				// -1 means we're currently submerged, anything above 0 is the timestamp of the last time we were submerged
				float submerged = -1.0f;
				float lastEmit = 0.0f;
				Water submergedWater = null;
				Property<Vector3> coordinatePos = new Property<Vector3>();
				VoxelAttachable.BindTarget(entity, coordinatePos);
				Action check = delegate()
				{
					Water nowSubmerged = Water.Get(coordinatePos);
					if (nowSubmerged == null && main.TotalTime - submerged < 1.0f)
					{
						Entity ve = attachable.AttachedVoxel.Value.Target;
						if (ve != null)
						{
							Voxel v = ve.Get<Voxel>();
							Voxel.Box b = v.GetBox(attachable.Coord);
							if (b != null)
							{
								BoundingBox box = new BoundingBox(v.GetRelativePosition(b.X, b.Y, b.Z), v.GetRelativePosition(b.X + b.Width, b.Y + b.Height, b.Z + b.Depth));
								if (submergedWater != null && main.TotalTime - lastEmit > 0.1f)
								{
									Water.SplashParticles(main, box.Transform(v.Transform), v, submergedWater.Position.Value.Y);
									lastEmit = main.TotalTime;
								}
							}
						}
					}

					if (nowSubmerged != null)
					{
						submerged = -1.0f;
						submergedWater = nowSubmerged;
					}
					else if (submerged == -1.0f && nowSubmerged == null)
					{
						Sound.PostEvent(AK.EVENTS.PLAY_WATER_SPLASH_OUT_BIG, transform.Position);
						submerged = main.TotalTime;
					}
				};
				transform.Add(new NotifyBinding(check, coordinatePos));
				entity.Add(new PostInitialization(delegate()
				{
					submerged = Water.Get(coordinatePos) != null ? -1.0f : -2.0f;
				}));
			}

			entity.Add("AttachOffset", attachable.Offset);
			entity.Add("AttachVector", attachable.Vector);
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);

			VoxelAttachable.AttachEditorComponents(entity, main, entity.Get<Model>().Color);
		}
	}
}
