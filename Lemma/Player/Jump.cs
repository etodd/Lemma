using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Jump : Component<Main>
	{
		// Input/output properties
		public Property<bool> IsSupported = new Property<bool>();
		public Property<bool> HasTraction = new Property<bool>();
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<BEPUphysics.Entities.Entity> SupportEntity = new Property<BEPUphysics.Entities.Entity>();

		// Input properties
		public Property<Vector2> AbsoluteMovementDirection = new Property<Vector2>();
		public Property<WallRun.State> WallRunState = new Property<WallRun.State>();
		public Property<float> Rotation = new Property<float>();
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<Vector3> FloorPosition = new Property<Vector3>();
		public Property<float> MaxSpeed = new Property<float>();
		public Property<float> JumpSpeed = new Property<float>();
		public Property<float> Mass = new Property<float>();
		public Property<float> LastRollEnded = new Property<float>();
		public Property<Voxel> WallRunMap = new Property<Voxel>();
		public Property<Direction> WallDirection = new Property<Direction>();

		// Output
		public Command<Voxel, Voxel.Coord, Direction> WalkedOn = new Command<Voxel, Voxel.Coord, Direction>();
		public Command DeactivateWallRun = new Command();
		public Command<float> FallDamage = new Command<float>();
		public BlockPredictor Predictor;
		public AnimatedModel Model;
		public Property<Voxel> LastWallRunMap = new Property<Voxel>();
		public Property<Direction> LastWallDirection = new Property<Direction>();
		public Property<bool> CanKick = new Property<bool>();
		public Property<float> LastWallJump = new Property<float> { Value = -1.0f };
		public Property<float> LastJump = new Property<float> { Value = -1.0f };
		public Property<float> LastSupportedSpeed = new Property<float>();

		private Voxel.State temporary;

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.EnabledWhenPaused = false;
			this.temporary = Voxel.States[Voxel.t.Blue];
			this.Add(new NotifyBinding(delegate()
			{
				if (this.IsSupported)
					this.wallJumpCount = 0;
			}, this.IsSupported));
		}

		private int wallJumpCount;
		private Vector3 wallJumpChainStart;

		private const float jumpCoolDown = 0.3f;

		public bool Go()
		{
			if (this.main.TotalTime - this.LastJump < jumpCoolDown)
				return false;

			bool supported = this.IsSupported;

			WallRun.State wallRunState = this.WallRunState;

			// Check if we're vaulting
			Matrix rotationMatrix = Matrix.CreateRotationY(this.Rotation);

			Vector2 jumpDirection = this.AbsoluteMovementDirection;

			Vector3 baseVelocity = Vector3.Zero;

			bool wallJumping = false;

			const float wallJumpHorizontalVelocityAmount = 0.75f;
			const float wallJumpDistance = 2.0f;

			Action<Voxel, Direction, Voxel.Coord> wallJump = delegate(Voxel wallJumpMap, Direction wallNormalDirection, Voxel.Coord wallCoordinate)
			{
				this.LastWallRunMap.Value = wallJumpMap;
				this.LastWallDirection.Value = wallNormalDirection.GetReverse();
				this.LastWallJump.Value = main.TotalTime;

				Voxel.State wallType = wallJumpMap[wallCoordinate];
				if (wallType == Voxel.EmptyState) // Empty. Must be a block possibility that hasn't been instantiated yet
					wallType = this.temporary;

				this.WalkedOn.Execute(wallJumpMap, wallCoordinate, wallNormalDirection.GetReverse());

				AkSoundEngine.PostEvent(AK.EVENTS.FOOTSTEP_PLAY, this.Entity);

				wallJumping = true;
				// Set up wall jump velocity
				Vector3 absoluteWallNormal = wallJumpMap.GetAbsoluteVector(wallNormalDirection.GetVector());
				Vector2 wallNormal2 = new Vector2(absoluteWallNormal.X, absoluteWallNormal.Z);
				wallNormal2.Normalize();

				bool wallRunningStraight = wallRunState == WallRun.State.Straight || wallRunState == WallRun.State.Down;
				if (wallRunningStraight)
					jumpDirection = new Vector2(main.Camera.Forward.Value.X, main.Camera.Forward.Value.Z);
				else
					jumpDirection = new Vector2(-rotationMatrix.Forward.X, -rotationMatrix.Forward.Z);

				jumpDirection.Normalize();

				float dot = Vector2.Dot(wallNormal2, jumpDirection);
				if (dot < 0)
					jumpDirection = jumpDirection - (2.0f * dot * wallNormal2);
				jumpDirection *= wallJumpHorizontalVelocityAmount;

				if (!wallRunningStraight && Math.Abs(dot) < 0.5f)
				{
					// If we're jumping perpendicular to the wall, add some velocity so we jump away from the wall a bit
					jumpDirection += wallJumpHorizontalVelocityAmount * 0.75f * wallNormal2;
				}

				DynamicVoxel dynamicMap = wallJumpMap as DynamicVoxel;
				if (dynamicMap != null)
				{
					BEPUphysics.Entities.Entity supportEntity = dynamicMap.PhysicsEntity;
					Vector3 supportLocation = this.FloorPosition;
					baseVelocity += supportEntity.LinearVelocity + Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
				}
			};

			if (!supported && wallRunState == WallRun.State.None
				&& this.LinearVelocity.Value.Y > Lemma.Components.FallDamage.DamageVelocity * 1.5f)
			{
				// We're not doing our normal jump, and not wall-runnign
				// See if we can wall-jump
				Vector3 playerPos = this.Position;
				Voxel.GlobalRaycastResult? wallRaycastHit = null;
				Vector3 wallRaycastDirection = Vector3.Zero;

				foreach (Vector3 dir in new[] { rotationMatrix.Left, rotationMatrix.Right, rotationMatrix.Backward, rotationMatrix.Forward })
				{
					Voxel.GlobalRaycastResult hit = Voxel.GlobalRaycast(playerPos, dir, wallJumpDistance);
					if (hit.Voxel != null)
					{
						wallRaycastDirection = dir;
						wallRaycastHit = hit;
						break;
					}
				}

				if (wallRaycastHit != null)
				{
					Voxel m = wallRaycastHit.Value.Voxel;
					wallJump(m, wallRaycastHit.Value.Normal, wallRaycastHit.Value.Coordinate.Value);
				}
			}

			// If we're wall-running, we can wall-jump
			// Add some velocity so we jump away from the wall a bit
			if (wallRunState != WallRun.State.None)
			{
				Vector3 pos = this.FloorPosition + new Vector3(0, 0.5f, 0);
				Voxel.Coord wallCoord = this.WallRunMap.Value.GetCoordinate(pos).Move(this.WallDirection, 2);
				wallJump(this.WallRunMap, this.WallDirection.Value.GetReverse(), wallCoord);
			}

			bool go = supported || wallJumping;

			BlockPredictor.Possibility instantiatedBlockPossibility = null;
			Voxel.Coord instantiatedBlockPossibilityCoord = default(Voxel.Coord);

			if (!go)
			{
				// Check block possibilities beneath us
				Vector3 jumpPos = this.FloorPosition + new Vector3(0, -1.0f, 0);
				foreach (BlockPredictor.Possibility possibility in this.Predictor.AllPossibilities)
				{
					Voxel.Coord possibilityCoord = possibility.Map.GetCoordinate(jumpPos);
					if (possibilityCoord.Between(possibility.StartCoord, possibility.EndCoord)
						&& !possibility.Map.GetCoordinate(jumpPos + new Vector3(2.0f)).Between(possibility.StartCoord, possibility.EndCoord))
					{
						this.Predictor.InstantiatePossibility(possibility);
						go = true;
						instantiatedBlockPossibility = possibility;
						instantiatedBlockPossibilityCoord = possibilityCoord;
						break;
					}
				}
			}

			if (!go)
			{
				// Check block possibilities for wall jumping
				Vector3 playerPos = this.Position;
				Vector3[] wallJumpDirections = new[] { rotationMatrix.Left, rotationMatrix.Right, rotationMatrix.Backward, rotationMatrix.Forward };
				foreach (BlockPredictor.Possibility possibility in this.Predictor.AllPossibilities)
				{
					foreach (Vector3 dir in wallJumpDirections)
					{
						foreach (Voxel.Coord coord in possibility.Map.Rasterize(playerPos, playerPos + (dir * wallJumpDistance)))
						{
							if (coord.Between(possibility.StartCoord, possibility.EndCoord))
							{
								this.Predictor.InstantiatePossibility(possibility);
								instantiatedBlockPossibility = possibility;
								instantiatedBlockPossibilityCoord = coord;
								wallJump(possibility.Map, possibility.Map.GetRelativeDirection(dir).GetReverse(), coord);
								wallJumping = true;
								go = true;
								break;
							}
						}
						if (wallJumping)
							break;
					}
					if (wallJumping)
						break;
				}
			}

			if (go)
			{
				float totalMultiplier = 1.0f;

				if (wallJumping)
				{
					if (this.wallJumpCount == 0)
						this.wallJumpChainStart = this.Position;
					else
					{
						Vector3 chainDistance = this.Position - this.wallJumpChainStart;
						chainDistance.Y = 0.0f;
						if (chainDistance.Length() > 6.0f)
						{
							this.wallJumpCount = 0;
							this.wallJumpChainStart = this.Position;
						}
					}

					if (this.wallJumpCount > 3)
						return false;
					totalMultiplier = 1.0f - Math.Min(1.0f, this.wallJumpCount / 8.0f);
					this.wallJumpCount++;
				}
				else
				{
					if (supported)
					{
						// Regular jump
						// Take base velocity into account

						BEPUphysics.Entities.Entity supportEntity = this.SupportEntity;
						if (supportEntity != null)
						{
							Vector3 supportLocation = this.FloorPosition;
							baseVelocity += supportEntity.LinearVelocity + Vector3.Cross(supportEntity.AngularVelocity, supportLocation - supportEntity.Position);
						}
					}
					else
					{
						// We haven't hit the ground, so fall damage will not be handled by the physics system.
						// Need to do it manually here.
						this.FallDamage.Execute(this.LinearVelocity.Value.Y);
						if (instantiatedBlockPossibility != null)
							this.WalkedOn.Execute(instantiatedBlockPossibility.Map, instantiatedBlockPossibilityCoord, instantiatedBlockPossibility.Map.GetRelativeDirection(Direction.NegativeY));

						// Also manually reset our ability to kick and wall-jump
						this.CanKick.Value = true;
						this.wallJumpCount = 0;
					}
				}

				Vector3 velocity = this.LinearVelocity;
				float currentVerticalSpeed = velocity.Y;
				velocity.Y = 0.0f;
				float jumpSpeed = jumpDirection.Length();
				if (jumpSpeed > 0)
					jumpDirection *= (wallJumping ? this.MaxSpeed : velocity.Length()) / jumpSpeed;

				float verticalMultiplier = 1.0f;

				if (main.TotalTime - this.LastRollEnded < 0.3f)
					totalMultiplier *= 1.5f;

				float verticalJumpSpeed = this.JumpSpeed * verticalMultiplier;

				// If we're not instantiating a block possibility beneath us or we're not currently falling, incorporate some of our existing vertical velocity in our jump
				if (instantiatedBlockPossibility == null || wallJumping || currentVerticalSpeed > 0.0f)
					verticalJumpSpeed += currentVerticalSpeed * 0.5f;

				this.LinearVelocity.Value = baseVelocity + new Vector3(jumpDirection.X, verticalJumpSpeed, jumpDirection.Y) * totalMultiplier;

				velocity = this.LinearVelocity;
				velocity.Y = 0.0f;
				this.LastSupportedSpeed.Value = velocity.Length();

				if (supported && this.SupportEntity.Value != null)
				{
					Vector3 impulsePosition = this.FloorPosition;
					Vector3 impulse = this.LinearVelocity.Value * this.Mass * -1.0f;
					this.SupportEntity.Value.ApplyImpulse(ref impulsePosition, ref impulse);
				}

				Session.Recorder.Event(main, "Jump");

				this.IsSupported.Value = false;
				this.SupportEntity.Value = null;
				this.HasTraction.Value = false;

				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_JUMP, this.Entity);

				this.Model.Stop
				(
					"Vault",
					"Mantle",
					"TopOut",
					"Jump",
					"JumpLeft",
					"JumpRight",
					"JumpBackward"
				);

				velocity = -Vector3.TransformNormal(this.LinearVelocity, Matrix.CreateRotationY(-this.Rotation));
				velocity.Y = 0.0f;
				if (wallRunState == WallRun.State.Left || wallRunState == WallRun.State.Right)
					velocity.Z = 0.0f;
				else if (wallJumping)
					velocity.Z *= 0.5f;
				else
					velocity.X = 0.0f;
				Direction direction = DirectionExtensions.GetDirectionFromVector(velocity);
				string animation;
				switch (direction)
				{
					case Direction.NegativeX:
						animation = "JumpLeft";
						break;
					case Direction.PositiveX:
						animation = "JumpRight";
						break;
					case Direction.PositiveZ:
						animation = wallJumping ? "JumpBackward" : "Jump";
						break;
					default:
						animation = "Jump";
						break;
				}
				this.Model.StartClip(animation, 4, false);
				this.Model[animation].CurrentTime = TimeSpan.FromSeconds(0.2);

				// Deactivate any wall-running we're doing
				this.DeactivateWallRun.Execute();

				// Play a footstep sound since we're jumping off the ground
				AkSoundEngine.PostEvent(AK.EVENTS.FOOTSTEP_PLAY, this.Entity);

				this.LastJump.Value = this.main.TotalTime;
				return true;
			}

			return false;
		}
	}
}
