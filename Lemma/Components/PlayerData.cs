using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;

namespace Lemma.Components
{
	public class PlayerData : Component<Main>, IUpdateableComponent
	{
		private const bool enabled = true;
		public Property<bool> EnableRoll = new Property<bool> { Value = enabled };
		public Property<bool> EnableCrouch = new Property<bool> { Value = enabled };
		public Property<bool> EnableKick = new Property<bool> { Value = enabled };
		public Property<bool> EnableWallRun = new Property<bool> { Value = enabled };
		public Property<bool> EnableWallRunHorizontal = new Property<bool> { Value = enabled };
		public Property<bool> EnableEnhancedWallRun = new Property<bool> { Value = enabled };
		public Property<bool> EnableSlowMotion = new Property<bool> { Value = enabled };
		public Property<bool> EnableMoves = new Property<bool> { Value = true };
		public Property<float> MaxSpeed = new Property<float> { Value = Character.DefaultMaxSpeed };
		public Property<float> GameTime = new Property<float>();
		public ListProperty<RespawnLocation> RespawnLocations = new ListProperty<RespawnLocation>();
		public Property<bool> PhoneActive = new Property<bool>();
		public Property<bool> NoteActive = new Property<bool>();
		public Property<int> Collectibles = new Property<int>();
		public Property<int> Notes = new Property<int>();
		public Property<Voxel.t> CloudType = new Property<Voxel.t>();
		public Property<int> CollapseLevel = new Property<int>();
		public Property<int> VictimsHandled = new Property<int>();
		public Property<bool> ThirdPerson = new Property<bool>();
		public Property<bool> DebugVelocity = new Property<bool>();

		public override void Awake()
		{
			this.EnabledWhenPaused = false;
			base.Awake();
			foreach (RespawnLocation l in this.RespawnLocations)
				l.Coordinate.Data = null;
		}

		public void Update(float dt)
		{
			this.GameTime.Value += dt;
		}
	}
}