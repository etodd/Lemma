using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class RollKickSlide : Component<Main>, IUpdateableComponent
	{
		public void StopKick()
		{
			if (this.Kicking)
			{
				this.Kicking.Value = false;
				AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_SLIDE_LOOP, this.Entity);
				this.Model.Stop("Kick", "Slide");
				this.EnableWalking.Value = true;
				if (!this.RollKickButton)
					this.AllowUncrouch.Value = true;
			}
		}

		// Input
		public Property<bool> RollKickButton = new Property<bool>();
		public Property<bool> EnableRoll = new Property<bool>();
		public Property<bool> EnableKick = new Property<bool>();
		public Property<bool> EnableCrouch = new Property<bool>();
		public Property<float> Rotation = new Property<float>();
		public Property<bool> IsSwimming = new Property<bool>();
		public Property<bool> IsSupported = new Property<bool>();
		public Property<Vector3> FloorPosition = new Property<Vector3>();
		public Property<Vector3> SupportVelocity = new Property<Vector3>();
		public Property<float> Height = new Property<float>();
		public Property<float> MaxSpeed = new Property<float>();
		public Property<float> JumpSpeed = new Property<float>();
		public Property<bool> EnableEnhancedRollSlide = new Property<bool>();

		// Input/output
		public Property<bool> AllowUncrouch = new Property<bool>();
		public Property<bool> Crouched = new Property<bool>();
		public Property<bool> EnableWalking = new Property<bool>();
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public BlockPredictor Predictor;

		// Output
		public Property<bool> Rolling = new Property<bool>();
		public Property<bool> Kicking = new Property<bool>();
		public Property<float> LastRollEnded = new Property<float>();
		public Command DeactivateWallRun = new Command();
		public Command Footstep = new Command();
		public Command LockRotation = new Command();
		public AnimatedModel Model;
		public VoxelTools VoxelTools;

		public Property<bool> CanKick = new Property<bool>();
		private float rollKickTime = 0.0f;
		private bool firstTimeBreak = false;
		private Vector3 forward;
		private Vector3 right;
		private Direction forwardDir;
		private Direction rightDir;
		private Map floorMap;
		private Map.Coordinate floorCoordinate;
		private bool shouldBuildFloor;
		private bool shouldBreakFloor;
		private Vector3 velocity;

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.Add(new NotifyBinding(delegate()
			{
				if (this.IsSupported)
					this.CanKick.Value = true;
			}, this.IsSupported));
		}

		public void Go()
		{
			if (this.Rolling || this.Kicking)
				return;

			Matrix rotationMatrix = Matrix.CreateRotationY(this.Rotation);
			this.forward = -rotationMatrix.Forward;
			this.right = rotationMatrix.Right;

			if (this.EnableCrouch && this.EnableRoll && !this.IsSwimming && (!this.EnableKick || !this.IsSupported || this.LinearVelocity.Value.Length() < 2.0f))
			{
				// Try to roll
				Vector3 playerPos = this.FloorPosition + new Vector3(0, 0.5f, 0);

				Map.GlobalRaycastResult floorRaycast = Map.GlobalRaycast(playerPos, Vector3.Down, this.Height + MathHelper.Clamp(this.LinearVelocity.Value.Y * -0.2f, 0.0f, 4.0f));

				bool nearGround = this.LinearVelocity.Value.Y < this.SupportVelocity.Value.Y + 0.1f && (this.IsSupported || floorRaycast.Map != null);

				bool instantiatedBlockPossibility = false;

				this.floorCoordinate = new Map.Coordinate();
				this.floorMap = null;

				if (nearGround)
				{
					this.floorMap = floorRaycast.Map;
					this.floorCoordinate = floorRaycast.Coordinate.Value;
				}
				else
				{
					// Check for block possibilities
					foreach (BlockPredictor.Possibility block in this.Predictor.AllPossibilities)
					{
						bool first = true;
						foreach (Map.Coordinate coord in block.Map.Rasterize(playerPos + Vector3.Up * 2.0f, playerPos + (Vector3.Down * (this.Height + 3.0f))))
						{
							if (coord.Between(block.StartCoord, block.EndCoord))
							{
								if (first)
									break; // If the top coord is intersecting the possible block, we're too far down into the block. Need to be at the top.
								this.Predictor.InstantiatePossibility(block);
								instantiatedBlockPossibility = true;
								floorMap = block.Map;
								floorCoordinate = coord;
								nearGround = true;
								break;
							}
							first = false;
						}
						if (nearGround)
							break;
					}
				}

				if (nearGround)
				{
					// We're rolling.
					this.Rolling.Value = true;

					Session.Recorder.Event(main, "Roll");

					this.DeactivateWallRun.Execute();

					this.EnableWalking.Value = false;
					this.LockRotation.Execute();

					this.Footstep.Execute(); // We just landed; play a footstep sound
					AkSoundEngine.PostEvent("Skill_Roll_Play", this.Entity);

					this.Model.StartClip("Roll", 5, false, AnimatedModel.DefaultBlendTime);

					Map.CellState floorState = floorRaycast.Map == null ? Map.EmptyState : floorRaycast.Coordinate.Value.Data;
					this.shouldBuildFloor = false;
					if (this.EnableEnhancedRollSlide && (instantiatedBlockPossibility || (floorState.ID != 0 && floorState.ID != Map.t.Temporary && floorState.ID != Map.t.Powered)))
						this.shouldBuildFloor = true;
					
					// If the player is not yet supported, that means they're just about to land.
					// So give them a little speed boost for having such good timing.
					this.velocity = this.forward * this.MaxSpeed * (this.IsSupported ? 0.75f : 1.25f);
					this.LinearVelocity.Value = new Vector3(this.velocity.X, instantiatedBlockPossibility ? 0.0f : this.LinearVelocity.Value.Y, this.velocity.Z);

					// Crouch
					this.Crouched.Value = true;
					this.AllowUncrouch.Value = false;

					this.rightDir = this.floorMap.GetRelativeDirection(this.right);
					this.forwardDir = this.floorMap.GetRelativeDirection(this.forward);

					this.rollKickTime = 0.0f;
					this.firstTimeBreak = true;
				}
			}

			if (!this.Rolling && this.EnableKick && this.CanKick && !this.Kicking)
			{
				// Kick
				this.Kicking.Value = true;
				this.CanKick.Value = false;

				Session.Recorder.Event(main, "Kick");

				this.DeactivateWallRun.Execute();

				this.EnableWalking.Value = false;
				this.LockRotation.Execute();

				this.Crouched.Value = true;
				this.AllowUncrouch.Value = false;

				this.LinearVelocity.Value += this.forward * Math.Max(4.0f, Vector3.Dot(this.forward, this.LinearVelocity) * 0.5f) + new Vector3(0, this.JumpSpeed * 0.25f, 0);

				this.velocity = this.LinearVelocity;

				this.Entity.Add(new Animation
				(
					new Animation.Delay(0.25f),
					new Animation.Execute(delegate() { AkSoundEngine.PostEvent("Kick_Play", this.Entity); })
				));

				Vector3 playerPos = this.FloorPosition + new Vector3(0, 0.5f, 0);

				this.shouldBuildFloor = false;
				this.shouldBreakFloor = false;

				Map.GlobalRaycastResult floorRaycast = Map.GlobalRaycast(playerPos, Vector3.Down, this.Height);
				this.floorMap = floorRaycast.Map;
				if (floorRaycast.Map == null)
				{
					this.shouldBreakFloor = true;
					this.floorCoordinate = new Map.Coordinate();
				}
				else
				{
					this.floorCoordinate = floorRaycast.Coordinate.Value;
					if (this.EnableEnhancedRollSlide)
					{
						Map.t floorType = floorRaycast.Coordinate.Value.Data.ID;
						if (floorType != Map.t.Temporary && floorType != Map.t.Powered)
							this.shouldBuildFloor = true;
					}
				}

				this.Model.StartClip(this.shouldBreakFloor ? "Kick" : "Slide", 5, false, AnimatedModel.DefaultBlendTime);
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE, this.Entity);
				if (!this.shouldBreakFloor) // We're sliding on the floor
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE_LOOP, this.Entity);

				this.forwardDir = Direction.None;
				this.rightDir = Direction.None;

				if (this.shouldBuildFloor)
				{
					this.forwardDir = floorRaycast.Map.GetRelativeDirection(this.forward);
					this.rightDir = floorRaycast.Map.GetRelativeDirection(this.right);
				}

				this.rollKickTime = 0.0f;
				this.firstTimeBreak = true;
			}
		}

		public void Update(float dt)
		{
			if (this.Rolling)
			{
				this.rollKickTime += dt;

				if (this.rollKickTime > 0.1f && (this.rollKickTime > 1.0f || Vector3.Dot(this.LinearVelocity, this.forward) < 0.1f))
				{
					this.Rolling.Value = false;
					this.EnableWalking.Value = true;
					if (!this.RollKickButton)
						this.AllowUncrouch.Value = true;
					this.LastRollEnded.Value = main.TotalTime;
				}
				else
				{
					this.LinearVelocity.Value = new Vector3(this.velocity.X, this.LinearVelocity.Value.Y, this.velocity.Z);
					if (this.VoxelTools.BreakWalls(this.forward, this.right, false))
					{
						if (this.firstTimeBreak)
							AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WALL_BREAK_01, this.Entity);
						this.firstTimeBreak = false;
					}

					if (this.shouldBuildFloor)
						this.VoxelTools.BuildFloor(this.floorMap, this.floorCoordinate, this.forwardDir, this.rightDir);
				}
			}
			else if (this.Kicking)
			{
				this.rollKickTime += dt;

				if (this.shouldBreakFloor && !this.IsSupported) // We weren't supported when we started kicking. We're flying.
				{
					// Roll if we hit the ground while kicking mid-air
					Vector3 playerPos = this.FloorPosition + new Vector3(0, 0.5f, 0);
					Map.GlobalRaycastResult r = Map.GlobalRaycast(playerPos, Vector3.Down, this.Height);
					if (r.Map != null)
					{
						this.StopKick();
						this.Go();
						return;
					}
				}

				if (this.rollKickTime > 0.75f || this.LinearVelocity.Value.Length() < 0.1f)
				{
					this.StopKick();
					return;
				}

				this.LinearVelocity.Value = new Vector3(this.velocity.X, this.LinearVelocity.Value.Y, this.velocity.Z);
				if (this.VoxelTools.BreakWalls(this.forward, this.right, this.shouldBreakFloor))
				{
					if (this.firstTimeBreak)
						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WALL_BREAK_01, this.Entity);
					this.firstTimeBreak = false;
				}
				if (this.shouldBuildFloor)
					this.VoxelTools.BuildFloor(this.floorMap, this.floorCoordinate, this.forwardDir, this.rightDir);
			}
		}
	}
}
