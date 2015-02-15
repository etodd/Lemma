using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class VoxelTrigger : Component<Main>
	{
		// Output properties
		public Property<bool> Triggered = new Property<bool>();

		// Input properties
		public Property<Voxel.t> State = new Property<Voxel.t>();
		[XmlIgnore]
		public Property<Entity.Handle> AttachedVoxel = new Property<Entity.Handle>();
		[XmlIgnore]
		public Property<Voxel.Coord> Coord = new Property<Voxel.Coord>();

		[XmlIgnore]
		public Command OnTriggerOn = new Command();

		[XmlIgnore]
		public Command OnTriggerOff = new Command();

		private CommandBinding<IEnumerable<Voxel.Coord>, Voxel> voxelBinding;

		public override void Awake()
		{
			base.Awake();
			this.Add(new CommandBinding(this.OnTriggerOn, () => !this.Triggered, delegate()
			{
				this.Triggered.Value = true;
			}));

			this.Add(new CommandBinding(this.OnTriggerOff, () => this.Triggered, delegate()
			{
				this.Triggered.Value = false;
			}));

			this.Add(new ChangeBinding<bool>(this.Triggered, delegate(bool old, bool value)
			{
				if (!old && value)
					this.OnTriggerOn.Execute();
				else if (old && !value)
					this.OnTriggerOff.Execute();
			}));

			if (!this.main.EditorEnabled)
				this.Add(new NotifyBinding(this.bindVoxel, this.AttachedVoxel));
		}

		public override void Start()
		{
			if (!this.main.EditorEnabled)
				this.bindVoxel();
		}

		private void bindVoxel()
		{
			if (this.voxelBinding != null)
			{
				this.Remove(this.voxelBinding);
				this.voxelBinding = null;
			}
			Entity attachedVoxel = this.AttachedVoxel.Value.Target;
			if (attachedVoxel != null && attachedVoxel.Active)
			{
				Voxel m = this.AttachedVoxel.Value.Target.Get<Voxel>();
				this.Triggered.Value = m[this.Coord].ID == this.State.Value;
				this.voxelBinding = new CommandBinding<IEnumerable<Voxel.Coord>, Voxel>(m.CellsFilled, delegate(IEnumerable<Voxel.Coord> coords, Voxel v)
				{
					foreach (Voxel.Coord c in coords)
					{
						if (c.Equivalent(this.Coord))
						{
							if (m[c].ID == this.State.Value)
								this.OnTriggerOn.Execute();
							else
								this.OnTriggerOff.Execute();
							break;
						}
					}
				});
				this.Add(this.voxelBinding);
			}
		}
		
		public void EditorProperties()
		{
			this.Entity.Add("State", this.State);
			this.Entity.Add("OnTriggerOn", this.OnTriggerOn);
			this.Entity.Add("OnTriggerOff", this.OnTriggerOff);
			this.Entity.Add("Triggered", this.Triggered, new PropertyEntry.EditorData { Readonly = true });
		}
	}
}
