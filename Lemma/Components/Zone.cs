using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using ComponentBind;

namespace Lemma.Components
{
	public class Zone : Component<Main>
	{
		public enum BuildMode { CanBuild, NoBuild, ExclusiveBuild };

		public static List<Zone> Zones = new List<Zone>();

		public ListProperty<Entity.Handle> ConnectedEntities = new ListProperty<Entity.Handle>();

		public Property<Entity.Handle> Parent = new Property<Entity.Handle> { Editable = false, Serialize = true };

		public Property<Matrix> Transform = new Property<Matrix> { Editable = false };

		public Property<BoundingBox> BoundingBox = new Property<BoundingBox> { Editable = false, Value = new BoundingBox(new Vector3(-5.0f), new Vector3(5.0f)) };

		public Property<float> ReverbAmount = new Property<float> { Value = 0.0f, Editable = true };

		public Property<float> ReverbSize = new Property<float> { Value = 0.0f, Editable = true };

		public Property<bool> Exclusive = new Property<bool> { Value = true, Editable = true };

		public Property<int> Priority = new Property<int> { Value = 0, Editable = true };

		public Property<BuildMode> Build = new Property<BuildMode> { Value = BuildMode.CanBuild, Editable = true };

		public Property<bool> DetailedShadows = new Property<bool> { Value = true, Editable = true };

		public static IEnumerable<Zone> GetConnectedZones(Zone zone)
		{
			IEnumerable<Zone> result = new Zone[] { zone };
			foreach (Entity e in zone.ConnectedEntities)
			{
				Zone z;
				if (e != null && (z = e.Get<Zone>()) != null)
					result = result.Concat(Zone.GetConnectedZones(z));
			}
			return result;
		}

		public override void InitializeProperties()
		{
			base.InitializeProperties();
			Zone.Zones.Add(this);
			this.Add(new CommandBinding(this.Delete, delegate() { Zone.Zones.Remove(this); }));

			IBinding[] parentBindings = null;

			this.Add(new NotifyBinding(delegate()
			{
				if (parentBindings != null)
				{
					foreach (IBinding binding in parentBindings)
						this.Remove(binding);
				}

				Entity parent = this.Parent.Value.Target;
				if (parent != null)
				{
					Zone z = parent.Get<Zone>();
					if (z != null)
					{
						parentBindings = new IBinding[]
						{
							new TwoWayBinding<float>(z.ReverbAmount, this.ReverbAmount),
							new TwoWayBinding<float>(z.ReverbSize, this.ReverbSize),
							new TwoWayBinding<bool>(z.Exclusive, this.Exclusive),
						};
						foreach (IBinding binding in parentBindings)
							this.Add(binding);
					}
				}
			}, this.Parent));

			this.Add(new NotifyBinding(delegate()
			{
				Lemma.Factories.WorldFactory.Get().GetCommand("UpdateZones").Execute();
			}, this.Parent, this.Exclusive, this.BoundingBox, this.Transform));

			this.main.AddComponent(new PostInitialization
			{
				delegate()
				{
					this.Parent.Reset();
				}
			});
		}

		public bool Contains(Vector3 x)
		{
			return this.BoundingBox.Value.Contains(Vector3.Transform(x, Matrix.Invert(this.Transform))) == ContainmentType.Contains;
		}

		public static Zone Get(Vector3 x)
		{
			Zone result = null;
			int minPriority = int.MaxValue;
			foreach (Zone z in Zone.Zones)
			{
				if (z.Contains(x))
				{
					Zone zone = z;
					while (zone.Parent.Value.Target != null)
						zone = zone.Parent.Value.Target.Get<Zone>();
					if (zone.Priority < minPriority)
					{
						result = zone;
						minPriority = zone.Priority;
					}
				}
			}
			return result;
		}

		public static bool CanBuild(Vector3 pos)
		{
			bool defaultValue = true;
			foreach (Zone z in Zone.Zones)
			{
				bool inZone = z.Contains(pos);
				if (inZone)
				{
					if (z.Build == Zone.BuildMode.NoBuild)
						defaultValue = false;
					else if (z.Build == Zone.BuildMode.CanBuild)
						return true;
				}
				else if (z.Build == Zone.BuildMode.ExclusiveBuild)
					defaultValue = false;
			}
			return defaultValue;
		}
	}
}
