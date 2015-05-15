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
		public Property<bool> IsSwimming = new Property<bool>();
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
		public Command<float> UpdateLockedRotation = new Command<float>();
		public Command Vault = new Command();
		public Command<Voxel, Voxel.Coord, Direction> WalkedOn = new Command<Voxel, Voxel.Coord, Direction>();
		public Property<State> CurrentState = new Property<State>();
		public Property<Voxel> WallRunVoxel = new Property<Voxel>();
		public Property<Direction> WallDirection = new Property<Direction>();
		public Property<Direction> WallRunDirection = new Property<Direction>();
		public Property<float> LastSupportedSpeed = new Property<float>();

		private const float minWallRunSpeed = 4.0f;

		private float lastWallRunEnded = -1.0f;
		private const float wallRunDelay = 0.75f;

		// Since block possibilities are instantiated on another thread,
		// we have to give that thread some time to do it before checking if there is actually a wall to run on.
		// Otherwise, we will immediately stop wall-running since the wall hasn't been instantiated yet.
		private float wallInstantiationTimer = 0.0f;

		public Property<Voxel> LastWallRunMap = new Property<Voxel>();
		public Property<Direction> LastWallDirection = new Property<Direction>();

		private Voxel.Coord lastWallRunCoord;

		private Vector3 lastWallRunStart;

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

		public bool Activate(State state, bool checkPossibilities)
		{
			if (!this.EnableWallRun)
				return false;

			Vector3 playerVelocity = this.LinearVelocity.Value;
			if (playerVelocity.Y < FallDamage.RollingDamageVelocity)
				return false;

			if (this.IsSwimming)
			{
				if (state == State.Left || state == State.Right)
					return false;
			}

			this.wallInstantiationTimer = 0.0f;

			// Prevent the player from repeatedly wall-running and wall-jumping ad infinitum.
			bool wallRunJumpDelayPassed = main.TotalTime - this.LastWallJump > wallRunDelay;

			Matrix matrix = Matrix.CreateRotationY(this.Rotation);

			Vector3 forwardVector = -matrix.Forward;

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
				Vector3 baseVelocity = voxel.LinearVelocity + Vector3.Cross(voxel.AngularVelocity, this.Position - voxel.Transform.Value.Translation);
				Vector3 v = Vector3.Normalize(playerVelocity - baseVelocity);
				v.Y = 0.0f;
				if (Vector3.Dot(forwardVector, v) < -0.3f)
					continue;

				Voxel.Coord coord = voxel.GetCoordinate(pos);
				Direction dir = voxel.GetRelativeDirection(wallVector);
				Direction up = voxel.GetRelativeDirection(Direction.PositiveY);
				Direction forwardDir = voxel.GetRelativeDirection(forwardVector);
				for (int i = 1; i < maxWallDistance; i++)
				{
					Voxel.Coord wallCoord = coord.Move(dir, i);
					if (voxel[coord.Move(dir, i - 1)] != Voxel.States.Empty
						|| voxel[coord.Move(dir, i - 1).Move(up, 1)] != Voxel.States.Empty
						|| voxel[coord.Move(dir, i - 1).Move(up, 2)] != Voxel.States.Empty
						|| ((state == State.Left || state == State.Right)
							&& (voxel[coord.Move(forwardDir).Move(dir, i - 1)] != Voxel.States.Empty
							|| voxel[coord.Move(forwardDir).Move(dir, i - 1).Move(up, 1)] != Voxel.States.Empty
							|| voxel[coord.Move(forwardDir).Move(dir, i - 1).Move(up, 2)] != Voxel.States.Empty)))
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
							Vector3 wallRunStartDiff = this.lastWallRunStart - this.Position;
							wallRunStartDiff.Y = 0.0f;
							addInitialVelocity = this.IsSupported || (differentWall && (wallRunStartDiff.Length() > 2.0f || main.TotalTime - this.lastWallRunEnded > wallRunDelay));
						}
					}
					else if (checkPossibilities)
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
									this.wallInstantiationTimer = 0.25f;
									break;
								}
							}
						}
					}
				}
			}

			if (closestVoxel != null)
			{
				if ((state == State.Left || state == State.Right) && !addInitialVelocity && Vector3.Dot(forwardVector, playerVelocity) < minWallRunSpeed)
					return false;
				this.Position.Value = closestVoxel.GetAbsolutePosition(closestCoord.Move(closestDir, closestDistance - 2)) + new Vector3(0, this.Height * 0.5f, 0);
				this.setup(closestVoxel, closestDir, state, forwardVector, addInitialVelocity);
				return true;
			}
			return false;
		}

		private void setup(Voxel voxel, Direction dir, State state, Vector3 forwardVector, bool addInitialVelocity, bool rotationAlreadyLocked = false)
		{
			this.StopKick.Execute();
			this.AllowUncrouch.Value = true;

			this.WallRunVoxel.Value = this.LastWallRunMap.Value = voxel;
			this.WallDirection.Value = this.LastWallDirection.Value = dir;
			this.lastWallRunCoord = new Voxel.Coord { X = int.MinValue, Y = int.MinValue, Z = int.MinValue };
			this.lastWallRunStart = this.Position;

			Vector3 baseVelocity = voxel.LinearVelocity + Vector3.Cross(voxel.AngularVelocity, this.Position - voxel.Transform.Value.Translation);
			if (state == State.Straight)
			{
				// Determine if we're actually going down
				float threshold = addInitialVelocity ? -0.5f : 0.0f;
				if (!this.IsSupported && this.LinearVelocity.Value.Y - baseVelocity.Y < threshold)
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
					velocity.X = baseVelocity.X;
					velocity.Z = baseVelocity.Z;
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
				float rotation = (float)Math.Atan2(wallVector.X, wallVector.Z);
				if (rotationAlreadyLocked)
					this.UpdateLockedRotation.Execute(rotation);
				else
				{
					this.Rotation.Value = rotation;
					this.LockRotation.Execute();
				}
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

				float rotation = (float)Math.Atan2(velocity.X, velocity.Z);
				if (rotationAlreadyLocked)
					this.UpdateLockedRotation.Execute(rotation);
				else
				{
					this.Rotation.Value = rotation;
					this.LockRotation.Execute();
				}

				if (addInitialVelocity)
				{
					velocity.Y = 0;
					float length = velocity.Length();
					if (length > 0)
					{
						velocity /= length;

						Vector3 currentHorizontalVelocity = this.LinearVelocity - baseVelocity;
						float currentVerticalSpeed = currentHorizontalVelocity.Y;
						currentHorizontalVelocity.Y = 0.0f;
						float horizontalSpeed = currentHorizontalVelocity.Length();
						velocity *= Math.Min(this.MaxSpeed * 1.4f, Math.Max(horizontalSpeed * 1.1f, 6.0f));

						if (Vector3.Dot(velocity - baseVelocity, forwardVector) < minWallRunSpeed + 1.0f)
							velocity += forwardVector * ((minWallRunSpeed + 2.0f) - Vector3.Dot(velocity - baseVelocity, forwardVector));

						velocity += baseVelocity;

						velocity.Y = (currentVerticalSpeed > -10.0f ? Math.Max(currentVerticalSpeed * 0.5f, 0.0f) + velocity.Length() * 0.6f : currentVerticalSpeed * 0.5f + 3.0f);

						this.LinearVelocity.Value = velocity;
						this.LastSupportedSpeed.Value = Vector3.Dot(velocity, forwardVector);
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
			if (this.IsSupported)
			{
				this.lastWallRunEnded = -1000.0f;
				this.LastWallRunMap.Value = null;
			}
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

				Voxel voxel = this.WallRunVoxel.Value;
				if (voxel == null || !voxel.Active)
				{
					this.Deactivate();
					return;
				}

				Vector3 wallRunVector = voxel.GetAbsoluteVector(this.WallRunDirection.Value.GetVector());
				Vector3 baseVelocity = voxel.LinearVelocity + Vector3.Cross(voxel.AngularVelocity, this.Position - voxel.Transform.Value.Translation);
				float wallRunSpeed = Vector3.Dot(this.LinearVelocity.Value - baseVelocity, wallRunVector);
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
						Vector3 wallVector = voxel.GetAbsoluteVector(this.WallDirection.Value.GetVector());
						Voxel.GlobalRaycastResult result = Voxel.GlobalRaycast(pos, wallRunVector + wallVector, 2.0f);
						if (result.Voxel != null && result.Voxel != voxel)
						{
							float dot = Vector3.Dot(result.Voxel.GetAbsoluteVector(result.Normal.GetReverse().GetVector()), wallVector);
							if (dot > 0.7f)
							{
								Matrix matrix = Matrix.CreateRotationY(this.Rotation);
								Vector3 forwardVector = -matrix.Forward;
								this.setup(result.Voxel, result.Normal.GetReverse(), wallRunState, forwardVector, false, true);
								return;
							}
						}
					}
				}

				Voxel.Coord coord = voxel.GetCoordinate(pos);
				Voxel.Coord wallCoord = coord.Move(this.WallDirection, 2);
				Voxel.State wallType = voxel[wallCoord];

				if (!wallCoord.Equivalent(this.lastWallRunCoord))
				{
					this.lastWallRunCoord = wallCoord;
					this.WalkedOn.Execute(voxel, wallCoord, this.WallDirection);
				}

				if (this.EnableEnhancedWallRun
					&& (wallRunState == State.Left || wallRunState == State.Right)
					&& Zone.CanBuild(this.Position)
					&& voxel.Entity.Type != "Bouncer")
				{
					Direction up = voxel.GetRelativeDirection(Direction.PositiveY);
					if (up.IsPerpendicular(this.WallDirection))
					{
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
								if ((float)Math.Sqrt(dx * dx + dy * dy) < radius && voxel[y].ID == 0)
								{
									buildCoords.Add(new EffectBlockFactory.BlockBuildOrder
									{
										Voxel = voxel,
										Coordinate = y,
										State = Voxel.States.Blue,
									});
								}
							}
						}
						Factory.Get<EffectBlockFactory>().Build(main, buildCoords, this.Position);
					}
					else
					{
						this.Deactivate();
						return;
					}
				}
				else if (wallType.ID == 0 && this.wallInstantiationTimer == 0.0f) // We ran out of wall to walk on
				{
					this.Deactivate();
					return;
				}

				this.wallInstantiationTimer = Math.Max(0.0f, this.wallInstantiationTimer - dt);

				Vector3 coordPos = voxel.GetAbsolutePosition(coord);

				Vector3 normal = voxel.GetAbsoluteVector(this.WallDirection.Value.GetVector());
				// Equation of a plane
				// normal (dot) point = d
				float d = Vector3.Dot(normal, coordPos) + (wallRunState == State.Down ? 0.3f : 0.4f);

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