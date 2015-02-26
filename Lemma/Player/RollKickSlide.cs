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
				this.LastRollKickEnded.Value = main.TotalTime;
				this.Kicking.Value = false;
				AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_SLIDE_LOOP, this.Entity);
				this.model.Stop("Kick", "Slide");
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
		public Property<Vector3> Position = new Property<Vector3>();

		// Output
		public Property<bool> Rolling = new Property<bool>();
		public Property<bool> Kicking = new Property<bool>();
		public Property<float> LastRollKickEnded = new Property<float> { Value = -1.0f };
		public Property<float> LastRollStarted = new Property<float> { Value = -1.0f };
		public Command DeactivateWallRun = new Command();
		public Command Footstep = new Command();
		public Command LockRotation = new Command();
		public Command<float> Rumble = new Command<float>();
		private AnimatedModel model;
		public VoxelTools VoxelTools;

		public Property<bool> CanKick = new Property<bool>();
		private float rollKickTime = 0.0f;
		private bool firstTimeBreak = false;
		private Vector3 forward;
		private Vector3 right;
		private Direction forwardDir;
		private Direction rightDir;
		private Voxel floorMap;
		private Voxel.Coord floorCoordinate;
		private bool shouldBuildFloor;
		private bool sliding;
		private Vector3 velocity;

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.EnabledWhenPaused = false;
			this.Add(new NotifyBinding(delegate()
			{
				if (this.IsSupported)
					this.CanKick.Value = true;
			}, this.IsSupported));
		}

		public void Bind(AnimatedModel model)
		{
			this.model = model;
			model["Slide"].Speed = 1.4f;
			model["Roll"].Speed = 1.75f;
			this.model["Kick"].GetChannel(this.model.GetBoneIndex("ORG-hips")).Filter = delegate(Matrix m)
			{
				m.Translation += new Vector3(0.0f, 0.0f, -1.25f);
				return m;
			};
		}

		private const float coolDown = 0.35f;

		private bool determineShouldBuildFloor(Voxel.State floorState)
		{
			bool result = false;
			if (floorState == Voxel.States.Blue || floorState == Voxel.States.Powered)
			{
				// If we're standing on blue or powered, we need to check if we're close to a non-blue block before we can build a floor
				// This prevents the player from building a floor infinitely
				if (VoxelAStar.BroadphaseSearch(this.floorMap, this.floorCoordinate, 6, x => x.Type != Voxel.States.Blue && x.Type != Voxel.States.Powered) != null)
					result = true;
			}
			else
				result = true;
			return result;
		}

		public void Go(bool overrideCooldown = false)
		{
			if (this.Rolling || this.Kicking || (main.TotalTime - this.LastRollKickEnded < coolDown && !overrideCooldown))
				return;

			Matrix rotationMatrix = Matrix.CreateRotationY(this.Rotation);
			this.forward = -rotationMatrix.Forward;
			this.right = rotationMatrix.Right;
			this.shouldBuildFloor = false;

			bool instantiatedBlockPossibility = false;

			if (this.EnableCrouch && this.EnableRoll && !this.IsSwimming
				&& (!this.EnableKick || !this.IsSupported || (this.LinearVelocity.Value - this.SupportVelocity.Value).Length() < 4.0f))
			{
				// Try to roll
				Vector3 playerPos = this.FloorPosition + new Vector3(0, 0.5f, 0);

				Voxel.GlobalRaycastResult floorRaycast = this.raycastFloor();

				bool nearGround = this.LinearVelocity.Value.Y < this.SupportVelocity.Value.Y + 0.1f && floorRaycast.Voxel != null;

				this.floorCoordinate = new Voxel.Coord();
				this.floorMap = null;

				if (nearGround)
				{
					this.floorMap = floorRaycast.Voxel;
					this.floorCoordinate = floorRaycast.Coordinate.Value;
				}
				else
				{
					// Check for block possibilities
					foreach (BlockPredictor.Possibility block in this.Predictor.AllPossibilities)
					{
						bool first = true;
						foreach (Voxel.Coord coord in block.Map.Rasterize(playerPos + Vector3.Up * 2.0f, playerPos + (Vector3.Down * (this.Height + 3.0f))))
						{
							if (coord.Between(block.StartCoord, block.EndCoord))
							{
								if (first)
									break; // If the top coord is intersecting the possible block, we're too far down into the block. Need to be at the top.
								this.Predictor.InstantiatePossibility(block);
								instantiatedBlockPossibility = true;
								this.floorMap = block.Map;
								this.floorCoordinate = coord;
								this.Position.Value += new Vector3(0, this.floorMap.GetAbsolutePosition(coord).Y + 2 - this.FloorPosition.Value.Y, 0);
								this.shouldBuildFloor = true;
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
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_ROLL, this.Entity);

					this.model.StartClip("Roll", 5, false, AnimatedModel.DefaultBlendTime);

					Voxel.State floorState = floorRaycast.Voxel == null ? Voxel.States.Empty : floorRaycast.Coordinate.Value.Data;
					if (this.EnableEnhancedRollSlide && (instantiatedBlockPossibility || (this.IsSupported && floorState != Voxel.States.Slider && floorState != Voxel.States.SliderPowered)))
						this.shouldBuildFloor |= this.determineShouldBuildFloor(floorState);
					
					// If the player is not yet supported, that means they're just about to land.
					// So give them a little speed boost for having such good timing.
					this.velocity = this.SupportVelocity + this.forward * this.MaxSpeed * (this.IsSupported ? 0.75f : 1.25f);
					this.LinearVelocity.Value = new Vector3(this.velocity.X, instantiatedBlockPossibility ? 0.0f : this.LinearVelocity.Value.Y, this.velocity.Z);

					// Crouch
					this.Crouched.Value = true;
					this.AllowUncrouch.Value = false;

					this.rightDir = this.floorMap.GetRelativeDirection(this.right);
					this.forwardDir = this.floorMap.GetRelativeDirection(this.forward);

					this.rollKickTime = 0.0f;
					this.firstTimeBreak = true;
					this.LastRollStarted.Value = this.main.TotalTime;
				}
			}

			if (!this.Rolling && this.EnableCrouch && this.EnableKick && this.CanKick && !this.Kicking && !this.IsSwimming && Vector3.Dot(this.LinearVelocity.Value - this.SupportVelocity, this.forward) > 0.05f)
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

				Vector3 playerPos = this.FloorPosition + new Vector3(0, 0.5f, 0);

				this.sliding = false;

				Voxel.GlobalRaycastResult floorRaycast = this.raycastFloor();
				this.floorMap = floorRaycast.Voxel;

				if (instantiatedBlockPossibility)
				{
					this.sliding = true;
					this.shouldBuildFloor = true;
				}
				else if (this.floorMap == null)
				{
					this.shouldBuildFloor = false;
					this.sliding = false;
					this.floorCoordinate = new Voxel.Coord();
				}
				else if (this.LinearVelocity.Value.Y - this.SupportVelocity.Value.Y < 1.0f)
				{
					this.floorCoordinate = floorRaycast.Coordinate.Value;
					if (this.EnableEnhancedRollSlide)
						this.shouldBuildFloor |= this.determineShouldBuildFloor(floorRaycast.Coordinate.Value.Data);
					this.sliding = true;
				}

				float forwardSpeed = Vector3.Dot(this.forward, this.LinearVelocity.Value - this.SupportVelocity);
				if (forwardSpeed < this.MaxSpeed * 1.1f)
				{
					if (this.sliding)
						this.velocity = this.LinearVelocity.Value + this.forward * (this.MaxSpeed - forwardSpeed);
					else
						this.velocity = this.LinearVelocity.Value + this.forward * Math.Max(4.0f, forwardSpeed * 0.4f) + new Vector3(0, this.JumpSpeed * 0.2f, 0);
				}
				else
					this.velocity = this.LinearVelocity;

				if (this.sliding)
					this.velocity.Y = 0.0f;

				this.LinearVelocity.Value = this.velocity;

				this.model.StartClip(this.sliding ? "Slide" : "Kick", 5, false, AnimatedModel.DefaultBlendTime);
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE, this.Entity);
				if (this.sliding) // We're sliding on the floor
					AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE_LOOP, this.Entity);

				this.forwardDir = Direction.None;
				this.rightDir = Direction.None;

				if (this.floorMap != null)
				{
					this.forwardDir = this.floorMap.GetRelativeDirection(this.forward);
					this.rightDir = this.floorMap.GetRelativeDirection(this.right);
				}

				this.rollKickTime = 0.0f;
				this.firstTimeBreak = true;
			}
		}

		private Voxel.GlobalRaycastResult raycastFloor()
		{
			Vector3 playerPos = this.FloorPosition + new Vector3(0, 0.5f, 0);

			return Voxel.GlobalRaycast(playerPos, Vector3.Down, this.Height + MathHelper.Clamp((this.LinearVelocity.Value.Y - this.SupportVelocity.Value.Y) * -0.3f, 0.0f, 5.0f));
		}

		private void checkShouldBuildFloor()
		{
			if (this.EnableEnhancedRollSlide)
			{
				Voxel.GlobalRaycastResult floorRaycast = this.raycastFloor();
				if (floorRaycast.Voxel != null)
				{
					Voxel.t t = floorRaycast.Voxel[floorRaycast.Coordinate.Value].ID;
					if (t != Voxel.t.Blue && t != Voxel.t.Powered && t != Voxel.t.Slider && t != Voxel.t.SliderPowered)
					{
						this.floorCoordinate = floorRaycast.Coordinate.Value;
						this.shouldBuildFloor = true;
						if (this.Kicking)
						{
							this.sliding = true;
							AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE_LOOP, this.Entity);
						}
					}
				}
			}
		}

		public void Update(float dt)
		{
			if (this.Rolling)
			{
				this.rollKickTime += dt;

				Vector3 originalVelocity = this.LinearVelocity;
				this.LinearVelocity.Value = new Vector3(this.velocity.X, originalVelocity.Y, this.velocity.Z);

				if (this.rollKickTime > 0.1f && (this.rollKickTime > 1.0f || Vector3.Dot(originalVelocity - this.SupportVelocity, this.forward) < 0.1f))
				{
					this.Rolling.Value = false;
					this.EnableWalking.Value = true;
					if (!this.RollKickButton)
						this.AllowUncrouch.Value = true;
					this.LastRollKickEnded.Value = main.TotalTime;
				}
				else
				{
					if (this.VoxelTools.BreakWalls(this.forward, this.right))
					{
						if (this.firstTimeBreak)
						{
							this.Rumble.Execute(0.5f);
							AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WALL_BREAK_01, this.Entity);
						}
						this.firstTimeBreak = false;
					}

					if (this.shouldBuildFloor)
						this.VoxelTools.BuildFloor(this.floorMap, this.floorCoordinate, this.forwardDir, this.rightDir);
					else
						this.checkShouldBuildFloor();
				}
			}
			else if (this.Kicking)
			{
				this.rollKickTime += dt;

				Vector3 originalVelocity = this.LinearVelocity;
				this.LinearVelocity.Value = new Vector3(this.velocity.X, originalVelocity.Y, this.velocity.Z);

				if (!this.IsSupported) 
				{
					if (this.sliding)
					{
						// We started out on the ground, but we kicked off an edge.
						AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_SLIDE_LOOP, this.Entity);
					}
					else if (originalVelocity.Y - this.SupportVelocity.Value.Y < FallDamage.DamageVelocity)
					{
						// We weren't supported when we started kicking. We're flying.
						// Roll if we hit the ground while kicking mid-air
						Vector3 playerPos = this.FloorPosition + new Vector3(0, 0.5f, 0);
						Voxel.GlobalRaycastResult r = this.raycastFloor();
						if (r.Voxel != null)
						{
							this.StopKick();
							this.Go(true);
							return;
						}
					}
				}

				if (this.rollKickTime > 1.0f
					|| (this.rollKickTime > 0.7f && (!this.RollKickButton || Vector3.Dot(originalVelocity - this.SupportVelocity, this.forward) < 0.1f)))
				{
					this.StopKick();
					return;
				}

				if (this.VoxelTools.BreakWalls(this.forward, this.right))
				{
					if (this.firstTimeBreak)
					{
						this.Rumble.Execute(0.5f);
						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WALL_BREAK_01, this.Entity);
					}
					this.firstTimeBreak = false;
				}
				if (this.shouldBuildFloor)
					this.VoxelTools.BuildFloor(this.floorMap, this.floorCoordinate, this.forwardDir, this.rightDir);
				else if (this.sliding)
					this.checkShouldBuildFloor();
			}
		}
	}
}