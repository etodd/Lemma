using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComponentBind;
using Lemma.Factories;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class WallRun : Component<Main>, IUpdateableComponent
	{
		public enum State { None, Left, Right, Straight, Down }

		// Input/output properties
		public Property<Vector3> LinearVelocity = new Property<Vector3>();
		public Property<Vector3> Position = new Property<Vector3>();
		public Property<float> Rotation = new Property<float>();
		public Property<bool> IsSupported = new Property<bool>();
		public BlockPredictor Predictor;

		// Input properties
		public Property<float> Height = new Property<float>();
		public Property<float> JumpSpeed = new Property<float>();
		public Property<float> MaxSpeed = new Property<float>();
		public Property<bool> EnableWallRun = new Property<bool>();
		public Property<bool> EnableWallRunHorizontal = new Property<bool>();
		public Property<bool> EnableEnhancedWallRun = new Property<bool>();
		public Property<float> LastWallJump = new Property<float>();

		// Output properties
		public Property<bool> AllowUncrouch = new Property<bool>();
		public Property<bool> HasTraction = new Property<bool>();
		public Command StopKick = new Command();
		public Command LockRotation = new Command();
		public Command Vault = new Command();
		public Command<Voxel, Voxel.Coord, Direction> WalkedOn = new Command<Voxel, Voxel.Coord, Direction>();
		public Property<State> CurrentState = new Property<State>();
		public Property<Voxel> WallRunVoxel = new Property<Voxel>();
		public Property<Direction> WallDirection = new Property<Direction>();
		public Property<Direction> WallRunDirection = new Property<Direction>();

		private const float minWallRunSpeed = 4.0f;

		private float lastWallRunEnded = -1.0f;
		private const float wallRunDelay = 0.5f;

		// Since block possibilities are instantiated on another thread,
		// we have to give that thread some time to do it before checking if there is actually a wall to run on.
		// Otherwise, we will immediately stop wall-running since the wall hasn't been instantiated yet.
		private float wallInstantiationTimer = 0.0f;

		public Property<Voxel> LastWallRunMap = new Property<Voxel>();
		public Property<Direction> LastWallDirection = new Property<Direction>();

		public override void Awake()
		{
			base.Awake();
			this.Serialize = false;
			this.EnabledWhenPaused = false;
		}

		public override void delete()
		{
			base.delete();
			AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_SLIDE_LOOP, this.Entity);
		}

		public bool Activate(State state)
		{
			if (!this.EnableWallRun)
				return false;

			Vector3 playerVelocity = this.LinearVelocity;
			if (playerVelocity.Y < FallDamage.RollingDamageVelocity)
				return false;

			wallInstantiationTimer = 0.0f;

			// Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.
			bool wallRunDelayPassed = main.TotalTime - this.lastWallRunEnded > wallRunDelay;
			bool wallRunJumpDelayPassed = main.TotalTime - this.LastWallJump > wallRunDelay;

			Matrix matrix = Matrix.CreateRotationY(this.Rotation);

			Vector3 forwardVector = -matrix.Forward;

			playerVelocity.Normalize();
			playerVelocity.Y = 0.0f;
			if (Vector3.Dot(forwardVector, playerVelocity) < -0.3f)
				return false;

			Vector3 wallVector;
			switch (state)
			{
				case State.Straight:
					wallVector = forwardVector;
					break;
				case State.Left:
					if (!this.EnableWallRunHorizontal)
						return false;
					wallVector = -matrix.Left;
					break;
				case State.Right:
					if (!this.EnableWallRunHorizontal)
						return false;
					wallVector = -matrix.Right;
					break;
				default:
					wallVector = Vector3.Zero;
					break;
			}

			Vector3 pos = this.Position + new Vector3(0, this.Height * -0.5f, 0);

			// Attempt to wall-run on an existing map
			bool addInitialVelocity = false;
			Voxel closestVoxel = null;
			Voxel.Coord closestCoord = default(Voxel.Coord);
			const int maxWallDistance = 4;
			int closestDistance = maxWallDistance;
			Direction closestDir = Direction.None;
			foreach (Voxel voxel in Voxel.ActivePhysicsVoxels)
			{
				Voxel.Coord coord = voxel.GetCoordinate(pos);
				Direction dir = voxel.GetRelativeDirection(wallVector);
				Direction up = voxel.GetRelativeDirection(Direction.PositiveY);
				for (int i = 1; i < maxWallDistance; i++)
				{
					Voxel.Coord wallCoord = coord.Move(dir, i);
					if (voxel[coord.Move(dir, i - 1)].ID != 0
						|| voxel[coord.Move(dir, i - 1).Move(up, 1)].ID != 0
						|| voxel[coord.Move(dir, i - 1).Move(up, 2)].ID != 0)
					{
						// Blocked
						break;
					}

					// Need at least two blocks to consider it a wall
					if (voxel[wallCoord].ID != 0 && voxel[wallCoord.Move(up)].ID != 0)
					{
						bool differentWall = voxel != this.LastWallRunMap.Value || dir != this.LastWallDirection.Value;
						if ((differentWall || wallRunJumpDelayPassed) && i < closestDistance)
						{
							closestVoxel = voxel;
							closestDistance = i;
							closestCoord = coord;
							closestDir = dir;
							addInitialVelocity = differentWall || wallRunDelayPassed;
						}
					}
					else
					{
						// Check block possibilities
						List<BlockPredictor.Possibility> mapBlockPossibilities = this.Predictor.GetPossibilities(voxel);
						if (mapBlockPossibilities != null)
						{
							foreach (BlockPredictor.Possibility block in mapBlockPossibilities)
							{
								if (wallCoord.Between(block.StartCoord, block.EndCoord))
								{
									this.Predictor.InstantiatePossibility(block);
									this.Predictor.ClearPossibilities();
									closestVoxel = voxel;
									closestDistance = i;
									closestCoord = coord;
									closestDir = dir;
									addInitialVelocity = true;
									wallInstantiationTimer = 0.25f;
									break;
								}
							}
						}
					}
				}
			}

			if (closestVoxel != null)
			{
				this.Position.Value = closestVoxel.GetAbsolutePosition(closestCoord.Move(closestDir, closestDistance - 2)) + new Vector3(0, this.Height * 0.5f, 0);
				this.setup(closestVoxel, closestDir, state, forwardVector, addInitialVelocity);
				return true;
			}
			return false;
		}

		private void setup(Voxel voxel, Direction dir, State state, Vector3 forwardVector, bool addInitialVelocity)
		{
			this.StopKick.Execute();
			this.AllowUncrouch.Value = true;

			this.WallRunVoxel.Value = this.LastWallRunMap.Value = voxel;
			this.WallDirection.Value = this.LastWallDirection.Value = dir;

			if (state == State.Straight)
			{
				// Determine if we're actually going down
				if (!this.IsSupported && this.LinearVelocity.Value.Y < -0.5f)
					state = State.Down;
			}

			this.CurrentState.Value = state;

			Session.Recorder.Event(main, "WallRun", this.CurrentState.Value.ToString());

			this.WallRunDirection.Value = state == State.Straight ? voxel.GetRelativeDirection(Vector3.Up) : (state == State.Down ? voxel.GetRelativeDirection(Vector3.Down) : dir.Cross(voxel.GetRelativeDirection(Vector3.Up)));

			if (state == State.Straight || state == State.Down)
			{
				if (state == State.Straight)
				{
					Vector3 velocity = this.LinearVelocity.Value;
					velocity.X = 0;
					velocity.Z = 0;
					if (addInitialVelocity)
						velocity.Y = Math.Max(this.JumpSpeed * 1.3f, (this.LinearVelocity.Value.Y * 1.4f) + this.JumpSpeed * 0.5f);
					else
						velocity.Y = this.LinearVelocity.Value.Y;

					this.LinearVelocity.Value = velocity;
					this.IsSupported.Value = false;
					this.HasTraction.Value = false;
				}
				Vector3 wallVector = this.WallRunVoxel.Value.GetAbsoluteVector(this.WallDirection.Value.GetVector());

				// Make sure we lock in the correct rotation value
				this.Rotation.Value = (float)Math.Atan2(wallVector.X, wallVector.Z);
				this.LockRotation.Execute();
			}
			else
			{
				this.IsSupported.Value = false;
				this.HasTraction.Value = false;
				Vector3 velocity = voxel.GetAbsoluteVector(this.WallRunDirection.Value.GetVector());
				if (Vector3.Dot(velocity, forwardVector) < 0.0f)
				{
					velocity = -velocity;
					this.WallRunDirection.Value = this.WallRunDirection.Value.GetReverse();
				}
				this.Rotation.Value = (float)Math.Atan2(velocity.X, velocity.Z);
				this.LockRotation.Execute();

				if (addInitialVelocity)
				{
					velocity.Y = 0.0f;
					float length = velocity.Length();
					if (length > 0)
					{
						velocity /= length;

						Vector3 currentHorizontalVelocity = this.LinearVelocity;
						currentHorizontalVelocity.Y = 0.0f;
						float horizontalSpeed = currentHorizontalVelocity.Length();
						velocity *= Math.Min(this.MaxSpeed * 2.0f, Math.Max(horizontalSpeed * 1.25f, 6.0f));

						if (Vector3.Dot(velocity, forwardVector) < minWallRunSpeed + 1.0f)
							velocity += forwardVector * ((minWallRunSpeed + 1.0f) - Vector3.Dot(velocity, forwardVector));

						float currentVerticalSpeed = this.LinearVelocity.Value.Y;
						velocity.Y = (currentVerticalSpeed > -10.0f ? Math.Max(currentVerticalSpeed * 0.5f, 0.0f) + velocity.Length() * 0.6f : currentVerticalSpeed * 0.5f + 3.0f);

						this.LinearVelocity.Value = velocity;
					}
				}
			}
			if (this.CurrentState != State.Straight)
				AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE_LOOP, this.Entity);
		}

		public void Deactivate()
		{
			if (this.CurrentState.Value != State.None)
				this.lastWallRunEnded = main.TotalTime; // Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.

			this.WallRunVoxel.Value = null;
			this.WallDirection.Value = Direction.None;
			this.WallRunDirection.Value = Direction.None;
			this.CurrentState.Value = State.None;
			AkSoundEngine.PostEvent(AK.EVENTS.STOP_PLAYER_SLIDE_LOOP, this.Entity);
		}

		public void Update(float dt)
		{
			State wallRunState = this.CurrentState;
			if (wallRunState != State.None)
			{
				this.Vault.Execute(); // Try to vault up
				if (this.CurrentState.Value == State.None) // We vaulted
					return;

				if (!this.WallRunVoxel.Value.Active || this.IsSupported)
				{
					this.Deactivate();
					return;
				}

				Vector3 wallRunVector = this.WallRunVoxel.Value.GetAbsoluteVector(this.WallRunDirection.Value.GetVector());
				float wallRunSpeed = Vector3.Dot(this.LinearVelocity.Value, wallRunVector);
				Vector3 pos = this.Position + new Vector3(0, this.Height * -0.5f, 0);

				if (wallRunState == State.Straight)
				{
					if (wallRunSpeed < 0.0f)
					{
						// Start sliding down
						this.CurrentState.Value = wallRunState = State.Down;
						AkSoundEngine.PostEvent(AK.EVENTS.PLAY_PLAYER_SLIDE_LOOP, this.Entity);
					}
				}
				else if (wallRunState == State.Left || wallRunState == State.Right)
				{
					if (this.IsSupported || wallRunSpeed < minWallRunSpeed)
					{
						// We landed on the ground or we're going too slow to continue wall-running
						this.Deactivate();
						return;
					}
					else
					{
						// Check if we should switch to another wall
						Vector3 wallVector = this.WallRunVoxel.Value.GetAbsoluteVector(this.WallDirection.Value.GetVector());
						Voxel.GlobalRaycastResult result = Voxel.GlobalRaycast(pos, wallRunVector + wallVector, 2.0f);
						if (result.Voxel != null && result.Voxel != this.WallRunVoxel.Value)
						{
							float dot = Vector3.Dot(result.Voxel.GetAbsoluteVector(result.Normal.GetReverse().GetVector()), wallVector);
							if (dot > 0.7f)
							{
								Matrix matrix = Matrix.CreateRotationY(this.Rotation);
								Vector3 forwardVector = -matrix.Forward;
								this.setup(result.Voxel, result.Normal.GetReverse(), wallRunState, forwardVector, false);
							}
						}
					}
				}

				Voxel.Coord coord = this.WallRunVoxel.Value.GetCoordinate(pos);
				Voxel.Coord wallCoord = coord.Move(this.WallDirection, 2);
				Voxel.State wallType = this.WallRunVoxel.Value[wallCoord];
				this.WalkedOn.Execute(this.WallRunVoxel, wallCoord, this.WallDirection);

				if (this.EnableEnhancedWallRun && (wallRunState == State.Left || wallRunState == State.Right))
				{
					Direction up = this.WallRunVoxel.Value.GetRelativeDirection(Direction.PositiveY);
					Direction right = this.WallDirection.Value.Cross(up);

					List<EffectBlockFactory.BlockBuildOrder> buildCoords = new List<EffectBlockFactory.BlockBuildOrder>();

					const int radius = 5;
					int upwardRadius = wallRunState == State.Down ? 0 : radius;
					for (Voxel.Coord x = wallCoord.Move(right, -radius); x.GetComponent(right) < wallCoord.GetComponent(right) + radius; x = x.Move(right))
					{
						int dx = x.GetComponent(right) - wallCoord.GetComponent(right);
						for (Voxel.Coord y = x.Move(up, -radius); y.GetComponent(up) < wallCoord.GetComponent(up) + upwardRadius; y = y.Move(up))
						{
							int dy = y.GetComponent(up) - wallCoord.GetComponent(up);
							if ((float)Math.Sqrt(dx * dx + dy * dy) < radius && this.WallRunVoxel.Value[y].ID == 0)
							{
								buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
								{
									Voxel = this.WallRunVoxel,
									Coordinate = y,
									State = Voxel.States.Blue,
								});
							}
						}
					}
					Factory.Get<EffectBlockFactory>().Build(main, buildCoords, this.Position);
				}
				else if (wallType.ID == 0 && wallInstantiationTimer == 0.0f) // We ran out of wall to walk on
				{
					this.Deactivate();
					return;
				}

				if (this.WallRunVoxel.Value == null || !this.WallRunVoxel.Value.Active)
					return;

				wallInstantiationTimer = Math.Max(0.0f, wallInstantiationTimer - dt);

				Vector3 coordPos = this.WallRunVoxel.Value.GetAbsolutePosition(coord);

				Vector3 normal = this.WallRunVoxel.Value.GetAbsoluteVector(this.WallDirection.Value.GetVector());
				// Equation of a plane
				// normal (dot) point = d
				float d = Vector3.Dot(normal, coordPos);

				// Distance along the normal to keep the player glued to the wall
				float snapDistance = d - Vector3.Dot(pos, normal);

				this.Position.Value += normal * snapDistance;

				Vector3 velocity = this.LinearVelocity;

				// Also fix the velocity so we don't jitter away from the wall
				velocity -= Vector3.Dot(velocity, normal) * normal;

				// Slow our descent
				velocity += new Vector3(0, (wallRunState == State.Straight ? 3.0f : 10.0f) * dt, 0);

				this.LinearVelocity.Value = velocity;
			}
		}
	}
}
