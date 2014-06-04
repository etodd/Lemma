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
		public Property<bool> EnableStamina = new Property<bool> { Value = enabled };
		public Property<bool> EnableMoves = new Property<bool> { Value = true };
		public Property<bool> EnablePhone = new Property<bool> { Value = enabled };
		public Property<float> MaxSpeed = new Property<float> { Value = Character.DefaultMaxSpeed };
		public Property<float> GameTime = new Property<float>();
		public ListProperty<RespawnLocation> RespawnLocations = new ListProperty<RespawnLocation>();
		public Property<bool> PhoneActive = new Property<bool>();
		public Property<bool> NoteActive = new Property<bool>();
		public Property<float> CameraShakeAmount = new Property<float>();

		public override void Awake()
		{
			this.EnabledWhenPaused = false;
			base.Awake();
		}

		public void Update(float dt)
		{
			this.GameTime.Value += dt;
		}
	}
}
